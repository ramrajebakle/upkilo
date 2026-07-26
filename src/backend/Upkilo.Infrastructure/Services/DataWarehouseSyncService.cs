using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implements Task 1752: Data warehouse export
/// Implements Task 1754: BigQuery sync
/// Implements Task 1756: Snowflake sync
/// Implements Task 1760: Incremental sync
/// Implements Task 1762: Scheduled exports
/// </summary>
public class DataWarehouseSyncService
{
    private readonly ILogger<DataWarehouseSyncService> _logger;
    private readonly AppDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private readonly string _target;
    private readonly string _bqProjectId;
    private readonly string _bqDatasetId;
    private readonly string _bqServiceAccountJson;
    private readonly string _sfAccount;
    private readonly string _sfUsername;
    private readonly string _sfPassword;
    private readonly string _sfDatabase;
    private readonly string _sfSchema;

    public DataWarehouseSyncService(
        ILogger<DataWarehouseSyncService> logger,
        AppDbContext context,
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        _target = configuration["DataWarehouse:Target"] ?? "none";
        _bqProjectId = configuration["DataWarehouse:BigQuery:ProjectId"] ?? string.Empty;
        _bqDatasetId = configuration["DataWarehouse:BigQuery:DatasetId"] ?? string.Empty;
        _bqServiceAccountJson = configuration["DataWarehouse:BigQuery:ServiceAccountJson"] ?? string.Empty;
        _sfAccount = configuration["DataWarehouse:Snowflake:AccountIdentifier"] ?? string.Empty;
        _sfUsername = configuration["DataWarehouse:Snowflake:Username"] ?? string.Empty;
        _sfPassword = configuration["DataWarehouse:Snowflake:Password"] ?? string.Empty;
        _sfDatabase = configuration["DataWarehouse:Snowflake:Database"] ?? string.Empty;
        _sfSchema = configuration["DataWarehouse:Snowflake:Schema"] ?? string.Empty;
    }

    public async Task RunIncrementalSyncAsync(Guid tenantId, string tableName)
    {
        if (_target == "none")
        {
            _logger.LogDebug("DataWarehouse:Target is 'none', skipping sync for {Table}", tableName);
            return;
        }

        _logger.LogInformation("Starting incremental sync for {TenantId} table={Table} target={Target}", tenantId, tableName, _target);

        try
        {
            var watermark = await GetWatermarkAsync(tenantId, tableName);
            var rows = await FetchRowsAsync(tenantId, tableName, watermark);

            if (rows.Count == 0)
            {
                _logger.LogInformation("No new rows for {TenantId}/{Table} since {Watermark}", tenantId, tableName, watermark);
                return;
            }

            var ndjson = BuildNdjson(rows);
            var newWatermark = DateTime.UtcNow;

            if (_target == "bigquery")
                await SyncToBigQueryAsync(tableName, ndjson);
            else if (_target == "snowflake")
                await SyncToSnowflakeAsync(tableName, rows);

            await SetWatermarkAsync(tenantId, tableName, newWatermark);
            _logger.LogInformation("Successfully synced {Count} rows for {TenantId}/{Table}", rows.Count, tenantId, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync {TenantId}/{Table} to {Target}", tenantId, tableName, _target);
        }
    }

    // ---- Watermark helpers ----

    private async Task<DateTime?> GetWatermarkAsync(Guid tenantId, string tableName)
    {
        var db = _redis.GetDatabase();
        var key = $"dw:watermark:{tenantId}:{tableName}";
        var val = await db.StringGetAsync(key);
        if (val.HasValue && DateTime.TryParse(val.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;
        return null;
    }

    private async Task SetWatermarkAsync(Guid tenantId, string tableName, DateTime ts)
    {
        var db = _redis.GetDatabase();
        var key = $"dw:watermark:{tenantId}:{tableName}";
        await db.StringSetAsync(key, ts.ToString("O"));
    }

    // ---- Data fetching ----

    private async Task<List<Dictionary<string, object?>>> FetchRowsAsync(Guid tenantId, string tableName, DateTime? since)
    {
        var cutoff = since ?? DateTime.MinValue;

        return tableName switch
        {
            "bookings" => await FetchBookingsAsync(tenantId, cutoff),
            "clients" => await FetchClientsAsync(tenantId, cutoff),
            "invoices" => await FetchInvoicesAsync(tenantId, cutoff),
            _ => new List<Dictionary<string, object?>>()
        };
    }

    private async Task<List<Dictionary<string, object?>>> FetchBookingsAsync(Guid tenantId, DateTime since)
    {
        var rows = await _context.Bookings
            .Where(b => b.TenantId == tenantId && b.UpdatedAt > since && !b.IsDeleted)
            .Select(b => new { b.Id, b.TenantId, b.StartTime, b.Status, b.Price, b.UpdatedAt })
            .ToListAsync();

        return rows.Select(b => new Dictionary<string, object?>
        {
            ["id"] = b.Id.ToString(),
            ["tenant_id"] = b.TenantId.ToString(),
            ["start_time"] = b.StartTime.ToString("O"),
            ["status"] = b.Status.ToString(),
            ["price"] = b.Price,
            ["updated_at"] = b.UpdatedAt.ToString("O")
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> FetchClientsAsync(Guid tenantId, DateTime since)
    {
        var rows = await _context.Clients
            .Where(c => c.TenantId == tenantId && c.UpdatedAt > since && !c.IsDeleted)
            .Select(c => new { c.Id, c.TenantId, c.Email, c.FirstName, c.LastName, c.CreatedAt, c.UpdatedAt })
            .ToListAsync();

        return rows.Select(c => new Dictionary<string, object?>
        {
            ["id"] = c.Id.ToString(),
            ["tenant_id"] = c.TenantId.ToString(),
            ["email"] = c.Email,
            ["first_name"] = c.FirstName,
            ["last_name"] = c.LastName,
            ["created_at"] = c.CreatedAt.ToString("O"),
            ["updated_at"] = c.UpdatedAt.ToString("O")
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> FetchInvoicesAsync(Guid tenantId, DateTime since)
    {
        var rows = await _context.Invoices
            .Where(i => i.TenantId == tenantId && i.UpdatedAt > since && !i.IsDeleted)
            .Select(i => new { i.Id, i.TenantId, i.CreatedAt, i.TotalAmount, i.Status, i.UpdatedAt })
            .ToListAsync();

        return rows.Select(i => new Dictionary<string, object?>
        {
            ["id"] = i.Id.ToString(),
            ["tenant_id"] = i.TenantId.ToString(),
            ["created_at"] = i.CreatedAt.ToString("O"),
            ["total_amount"] = i.TotalAmount,
            ["status"] = i.Status.ToString(),
            ["updated_at"] = i.UpdatedAt.ToString("O")
        }).ToList();
    }

    // ---- Serialization ----

    private static string BuildNdjson(List<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
            sb.AppendLine(JsonSerializer.Serialize(row));
        return sb.ToString();
    }

    // ---- BigQuery ----

    private async Task SyncToBigQueryAsync(string tableName, string ndjson)
    {
        var token = await GetBigQueryTokenAsync();
        var url = $"https://bigquery.googleapis.com/bigquery/v2/projects/{_bqProjectId}/datasets/{_bqDatasetId}/tables/{tableName}/insertAll";

        // Parse NDJSON back to build BigQuery insertAll payload
        var rows = ndjson.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => new { insertId = Guid.NewGuid().ToString("N"), json = JsonSerializer.Deserialize<JsonElement>(line) })
            .ToList();

        var payload = JsonSerializer.Serialize(new { rows });

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"BigQuery insertAll failed: {response.StatusCode} {body}");
        }
    }

    private async Task<string> GetBigQueryTokenAsync()
    {
        // Parse service account JSON to get client_email and private_key
        using var doc = JsonDocument.Parse(_bqServiceAccountJson);
        var clientEmail = doc.RootElement.GetProperty("client_email").GetString()!;
        var privateKeyPem = doc.RootElement.GetProperty("private_key").GetString()!;
        var tokenUri = doc.RootElement.GetProperty("token_uri").GetString()
            ?? "https://oauth2.googleapis.com/token";

        // Build JWT for Google OAuth2
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = clientEmail,
            scope = "https://www.googleapis.com/auth/bigquery",
            aud = tokenUri,
            exp = now + 3600,
            iat = now
        }));
        var signingInput = $"{header}.{claims}";

        var privateKey = ImportRsaPrivateKey(privateKeyPem);
        var signature = Base64UrlEncode(privateKey.SignData(
            Encoding.ASCII.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));

        var jwt = $"{signingInput}.{signature}";

        var client = _httpClientFactory.CreateClient();
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            new KeyValuePair<string, string>("assertion", jwt)
        });

        var resp = await client.PostAsync(tokenUri, form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var tokenDoc = JsonDocument.Parse(json);
        return tokenDoc.RootElement.GetProperty("access_token").GetString()!;
    }

    private static RSA ImportRsaPrivateKey(string pem)
    {
        var pemContent = pem
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();
        var keyBytes = Convert.FromBase64String(pemContent);
        var rsa = RSA.Create();
        try { rsa.ImportPkcs8PrivateKey(keyBytes, out _); }
        catch { rsa.ImportRSAPrivateKey(keyBytes, out _); }
        return rsa;
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // ---- Snowflake ----

    private async Task SyncToSnowflakeAsync(string tableName, List<Dictionary<string, object?>> rows)
    {
        var fullTable = $"{_sfDatabase}.{_sfSchema}.{tableName.ToUpperInvariant()}";

        if (rows.Count == 0) return;

        var columns = string.Join(", ", rows[0].Keys.Select(k => k.ToUpperInvariant()));
        var valueRows = rows.Select(row =>
            "(" + string.Join(", ", row.Values.Select(v => v == null ? "NULL" : $"'{v?.ToString()?.Replace("'", "''")}'")) + ")"
        );
        var sql = $"INSERT INTO {fullTable} ({columns}) VALUES {string.Join(",", valueRows)}";

        var url = $"https://{_sfAccount}.snowflakecomputing.com/api/v2/statements";
        var payload = JsonSerializer.Serialize(new
        {
            statement = sql,
            timeout = 60,
            database = _sfDatabase,
            schema = _sfSchema
        });

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_sfUsername}:{_sfPassword}"));
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Add("X-Snowflake-Authorization-Token-Type", "BASIC");

        var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Snowflake SQL API failed: {response.StatusCode} {body}");
        }
    }

    // Legacy overload kept for backward compatibility
    public Task RunIncrementalSyncAsync(Guid tenantId, string target, string tableName)
        => RunIncrementalSyncAsync(tenantId, tableName);
}
