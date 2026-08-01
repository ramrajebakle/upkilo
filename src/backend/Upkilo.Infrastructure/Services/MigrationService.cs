using Upkilo.Core.Interfaces;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using System.Text.Json;

namespace Upkilo.Infrastructure.Services;

public class MigrationService : IMigrationService
{
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<MigrationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MigrationService(
        AppDbContext context,
        IBackgroundJobClient jobClient,
        ILogger<MigrationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _jobClient = jobClient;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<MigrationOverview> GetMigrationOverviewAsync(string provider, string apiKey, string? extraCredentials = null)
    {
        _logger.LogInformation("Fetching migration overview for {Provider}", provider);

        var normalizedProvider = provider.ToLower();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key is required for migration");
        }

        // Validate provider is supported
        var supported = new[] { "calendly", "acuity", "square", "mindbody", "vagaro" };
        if (!supported.Contains(normalizedProvider))
        {
            throw new ArgumentException($"Unsupported provider: {provider}. Supported: {string.Join(", ", supported)}");
        }

        // Test API connectivity by making a lightweight request
        try
        {
            var overview = await FetchProviderOverviewAsync(normalizedProvider, apiKey, extraCredentials);
            return overview;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to {Provider} API", provider);
            throw new InvalidOperationException($"Could not connect to {provider}. Please verify your API key and try again.", ex);
        }
    }

    private async Task<MigrationOverview> FetchProviderOverviewAsync(string provider, string apiKey, string? extraCredentials)
    {
        if (provider == "calendly")
        {
            return await FetchCalendlyOverviewAsync(apiKey);
        }
        else if (provider == "square")
        {
            return await FetchSquareOverviewAsync(apiKey);
        }
        else if (provider == "mindbody")
        {
            return await FetchMindbodyOverviewAsync(apiKey, extraCredentials);
        }

        _logger.LogWarning("Provider '{Provider}' migration overview not yet implemented. API key validation only.", provider);

        if (apiKey.Length < 10)
        {
            throw new ArgumentException("API key appears to be invalid (too short)");
        }

        await Task.CompletedTask;

        return new MigrationOverview
        {
            Provider = provider,
            ServiceCount = 0,
            StaffCount = 0,
            BookingCount = 0,
            FoundServices = new List<string>(),
            FoundStaff = new List<string>()
        };
    }

    private async Task<MigrationOverview> FetchCalendlyOverviewAsync(string apiKey)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // 1. Get User/Organization info
        var userResponse = await client.GetAsync("https://api.calendly.com/users/me");
        if (!userResponse.IsSuccessStatusCode) throw new InvalidOperationException("Invalid Calendly API key or permissions.");

        // 2. Get Event Types (Services)
        var typesResponse = await client.GetAsync("https://api.calendly.com/event_types");
        var typesContent = await typesResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(typesContent);
        var types = doc.RootElement.GetProperty("collection");

        var services = new List<string>();
        foreach (var t in types.EnumerateArray())
        {
            services.Add(t.GetProperty("name").GetString() ?? "Unknown Service");
        }

        return new MigrationOverview
        {
            Provider = "calendly",
            ServiceCount = services.Count,
            StaffCount = 1,
            BookingCount = 0,
            FoundServices = services,
            FoundStaff = new List<string> { "Primary Account Holder" }
        };
    }

    private async Task<MigrationOverview> FetchSquareOverviewAsync(string apiKey)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // 1. Get Team Members
        var teamRes = await client.GetAsync("https://connect.squareup.com/v2/team-members/search");
        var teamContent = await teamRes.Content.ReadAsStringAsync();
        using var teamDoc = JsonDocument.Parse(teamContent);
        var teamCount = teamDoc.RootElement.TryGetProperty("team_members", out var tm) ? tm.GetArrayLength() : 0;

        // 2. Get Catalog Items
        var catRes = await client.GetAsync("https://connect.squareup.com/v2/catalog/list?types=ITEM");
        var catContent = await catRes.Content.ReadAsStringAsync();
        using var catDoc = JsonDocument.Parse(catContent);
        var catCount = catDoc.RootElement.TryGetProperty("objects", out var obj) ? obj.GetArrayLength() : 0;

        return new MigrationOverview
        {
            Provider = "square",
            ServiceCount = catCount,
            StaffCount = teamCount,
            BookingCount = 0,
            FoundServices = new List<string> { $"{catCount} Catalog Items" },
            FoundStaff = new List<string> { $"{teamCount} Team Members" }
        };
    }

    private async Task<MigrationOverview> FetchMindbodyOverviewAsync(string apiKey, string? extraCredentials)
    {
        // Mindbody requires SiteID in extraCredentials
        if (!int.TryParse(extraCredentials, out var siteId))
            throw new ArgumentException("Mindbody requires a valid Site ID in extraCredentials.");

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Api-Key", apiKey);
        client.DefaultRequestHeaders.Add("SiteId", extraCredentials);

        // 1. Get Staff
        var staffRes = await client.GetAsync("https://api.mindbodyonline.com/public/v6/staff/staff");
        var staffContent = await staffRes.Content.ReadAsStringAsync();
        using var staffDoc = JsonDocument.Parse(staffContent);
        var staffCount = staffDoc.RootElement.TryGetProperty("Staff", out var s) ? s.GetArrayLength() : 0;

        return new MigrationOverview
        {
            Provider = "mindbody",
            ServiceCount = 0,
            StaffCount = staffCount,
            BookingCount = 0,
            FoundServices = new List<string>(),
            FoundStaff = new List<string> { $"{staffCount} Staff Members" }
        };
    }

    public async Task<ImportJob> StartMigrationAsync(Guid tenantId, Guid userId, MigrationRequest request)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityType = $"{request.Provider.ToLower()}_migration",
            FileName = $"migration_{request.Provider}_{DateTime.UtcNow:yyyyMMdd}",
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ImportJob>().Add(job);
        await _context.SaveChangesAsync();

        _jobClient.Enqueue<MigrationService>(x => x.ProcessMigrationBackgroundAsync(job.Id, request));

        return job;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessMigrationBackgroundAsync(Guid jobId, MigrationRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var job = await _context.Set<ImportJob>().FindAsync(jobId);
        if (job == null) return;

        job.Status = "processing";
        await _context.SaveChangesAsync();

        var errors = new List<object>();

        try
        {
            if (request.Provider.ToLower() == "calendly")
            {
                await ProcessCalendlyMigrationAsync(job, request);
            }
            else if (request.Provider.ToLower() == "square")
            {
                await ProcessSquareMigrationAsync(job, request);
            }
            else if (request.Provider.ToLower() == "mindbody")
            {
                await ProcessMindbodyMigrationAsync(job, request);
            }
            else
            {
                _logger.LogWarning("Migration processing for provider '{Provider}' is not yet implemented.", request.Provider);
                job.Status = "failed";
                errors.Add(new { error = $"Migration from '{request.Provider}' is not yet implemented." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for job {JobId}", jobId);
            job.Status = "failed";
            errors.Add(new { error = ex.Message });
        }
        finally
        {
            stopwatch.Stop();
            job.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            job.ErrorDetails = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task ProcessCalendlyMigrationAsync(ImportJob job, MigrationRequest request)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.ApiKey);

        // 1. Fetch Event Types -> Map to Services
        var typesResponse = await client.GetAsync("https://api.calendly.com/event_types");
        var typesContent = await typesResponse.Content.ReadAsStringAsync();
        using var typesDoc = JsonDocument.Parse(typesContent);
        var types = typesDoc.RootElement.GetProperty("collection");

        foreach (var t in types.EnumerateArray())
        {
            var name = t.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(name)) continue;

            if (!await _context.Services.AnyAsync(s => s.TenantId == job.TenantId && s.Name == name))
            {
                _context.Services.Add(new Service
                {
                    Id = Guid.NewGuid(),
                    TenantId = job.TenantId,
                    Name = name,
                    Description = t.GetProperty("description_plain").GetString() ?? "",
                    DurationMinutes = t.GetProperty("duration").GetInt32(),
                    Price = 0,
                    IsActive = t.GetProperty("active").GetBoolean(),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync();

        // 2. Fetch Scheduled Events -> Map to Clients & Bookings
        var eventsResponse = await client.GetAsync("https://api.calendly.com/scheduled_events?status=active");
        var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
        using var eventsDoc = JsonDocument.Parse(eventsContent);
        var collections = eventsDoc.RootElement.GetProperty("collection");

        job.TotalRows = collections.GetArrayLength();

        foreach (var ev in collections.EnumerateArray())
        {
            try
            {
                var startTime = ev.GetProperty("start_time").GetDateTime();
                var endTime = ev.GetProperty("end_time").GetDateTime();

                // Get Invitee info (Client)
                var inviteeUri = ev.GetProperty("uri").GetString() + "/invitees";
                var inviteeRes = await client.GetAsync(inviteeUri);
                var inviteeContent = await inviteeRes.Content.ReadAsStringAsync();
                using var inviteeDoc = JsonDocument.Parse(inviteeContent);
                var invitee = inviteeDoc.RootElement.GetProperty("collection")[0];

                var email = invitee.GetProperty("email").GetString() ?? "";
                var clientName = invitee.GetProperty("name").GetString() ?? "Unknown";

                var dbClient = await _context.Clients.FirstOrDefaultAsync(c => c.TenantId == job.TenantId && c.Email == email);
                if (dbClient == null)
                {
                    dbClient = new Client
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        FirstName = clientName.Split(' ')[0],
                        LastName = clientName.Contains(' ') ? clientName.Substring(clientName.IndexOf(' ') + 1) : "",
                        Email = email,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Clients.Add(dbClient);
                    await _context.SaveChangesAsync();
                }

                _context.Bookings.Add(new Booking
                {
                    Id = Guid.NewGuid(),
                    TenantId = job.TenantId,
                    ClientId = dbClient.Id,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = BookingStatus.Confirmed,
                    Source = BookingSource.Import,
                    CreatedAt = DateTime.UtcNow
                });

                job.SuccessfulRows++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating Calendly event");
            }

            job.ProcessedRows++;
            await _context.SaveChangesAsync();
        }

        job.Status = "completed";
    }

    private async Task ProcessSquareMigrationAsync(ImportJob job, MigrationRequest request)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.ApiKey);

        // 1. Staff (Team Members)
        var teamRes = await client.GetAsync("https://connect.squareup.com/v2/team-members/search");
        var teamContent = await teamRes.Content.ReadAsStringAsync();
        using var teamDoc = JsonDocument.Parse(teamContent);
        if (teamDoc.RootElement.TryGetProperty("team_members", out var teamArr))
        {
            foreach (var member in teamArr.EnumerateArray())
            {
                var email = member.TryGetProperty("email_address", out var e) ? e.GetString() : null;
                if (string.IsNullOrEmpty(email)) continue;

                if (!await _context.Staff.AnyAsync(s => s.TenantId == job.TenantId && s.Email == email))
                {
                    _context.Staff.Add(new StaffMember
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        FirstName = member.GetProperty("given_name").GetString() ?? "",
                        LastName = member.GetProperty("family_name").GetString() ?? "",
                        Email = email,
                        Role = "Staff",
                        IsActive = member.GetProperty("status").GetString() == "ACTIVE",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await _context.SaveChangesAsync();

        // 2. Services (Catalog Items)
        var catRes = await client.GetAsync("https://connect.squareup.com/v2/catalog/list?types=ITEM");
        var catContent = await catRes.Content.ReadAsStringAsync();
        using var catDoc = JsonDocument.Parse(catContent);
        if (catDoc.RootElement.TryGetProperty("objects", out var catArr))
        {
            foreach (var obj in catArr.EnumerateArray())
            {
                var name = obj.GetProperty("item_data").GetProperty("name").GetString();
                if (string.IsNullOrEmpty(name)) continue;

                if (!await _context.Services.AnyAsync(s => s.TenantId == job.TenantId && s.Name == name))
                {
                    _context.Services.Add(new Service
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        Name = name,
                        Description = obj.GetProperty("item_data").TryGetProperty("description", out var d) ? d.GetString() : "",
                        DurationMinutes = 60, // Default for now
                        Price = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await _context.SaveChangesAsync();

        // 3. Clients (Customers)
        var custRes = await client.GetAsync("https://connect.squareup.com/v2/customers");
        var custContent = await custRes.Content.ReadAsStringAsync();
        using var custDoc = JsonDocument.Parse(custContent);
        if (custDoc.RootElement.TryGetProperty("customers", out var custArr))
        {
            job.TotalRows = custArr.GetArrayLength();
            foreach (var cust in custArr.EnumerateArray())
            {
                var email = cust.TryGetProperty("email_address", out var e) ? e.GetString() : null;
                if (string.IsNullOrEmpty(email)) continue;

                var dbClient = await _context.Clients.FirstOrDefaultAsync(c => c.TenantId == job.TenantId && c.Email == email);
                if (dbClient == null)
                {
                    dbClient = new Client
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        FirstName = cust.TryGetProperty("given_name", out var gn) ? gn.GetString() : "Unknown",
                        LastName = cust.TryGetProperty("family_name", out var fn) ? fn.GetString() : "",
                        Email = email,
                        PhoneNumber = cust.TryGetProperty("phone_number", out var pn) ? pn.GetString() : null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Clients.Add(dbClient);
                    job.SuccessfulRows++;
                }
                job.ProcessedRows++;
            }
        }
        await _context.SaveChangesAsync();

        job.Status = "completed";
    }

    private async Task ProcessMindbodyMigrationAsync(ImportJob job, MigrationRequest request)
    {
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Api-Key", request.ApiKey);
        client.DefaultRequestHeaders.Add("SiteId", request.ExtraCredentials);

        // 1. Staff
        var staffRes = await client.GetAsync("https://api.mindbodyonline.com/public/v6/staff/staff");
        var staffContent = await staffRes.Content.ReadAsStringAsync();
        using var staffDoc = JsonDocument.Parse(staffContent);
        if (staffDoc.RootElement.TryGetProperty("Staff", out var staffArr))
        {
            foreach (var s in staffArr.EnumerateArray())
            {
                var email = s.TryGetProperty("Email", out var e) ? e.GetString() : null;
                if (string.IsNullOrEmpty(email)) continue;

                if (!await _context.Staff.AnyAsync(st => st.TenantId == job.TenantId && st.Email == email))
                {
                    _context.Staff.Add(new StaffMember
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        FirstName = s.GetProperty("FirstName").GetString() ?? "",
                        LastName = s.GetProperty("LastName").GetString() ?? "",
                        Email = email,
                        Role = "Staff",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await _context.SaveChangesAsync();

        // 2. Clients
        var clientRes = await client.GetAsync("https://api.mindbodyonline.com/public/v6/client/clients");
        var clientContent = await clientRes.Content.ReadAsStringAsync();
        using var clientDoc = JsonDocument.Parse(clientContent);
        if (clientDoc.RootElement.TryGetProperty("Clients", out var clientArr))
        {
            job.TotalRows = clientArr.GetArrayLength();
            foreach (var c in clientArr.EnumerateArray())
            {
                var email = c.TryGetProperty("Email", out var e) ? e.GetString() : null;
                if (string.IsNullOrEmpty(email)) continue;

                var dbClient = await _context.Clients.FirstOrDefaultAsync(cl => cl.TenantId == job.TenantId && cl.Email == email);
                if (dbClient == null)
                {
                    dbClient = new Client
                    {
                        Id = Guid.NewGuid(),
                        TenantId = job.TenantId,
                        FirstName = c.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "Unknown",
                        LastName = c.TryGetProperty("LastName", out var ln) ? ln.GetString() : "",
                        Email = email,
                        PhoneNumber = c.TryGetProperty("MobilePhone", out var mp) ? mp.GetString() : null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Clients.Add(dbClient);
                    job.SuccessfulRows++;
                }
                job.ProcessedRows++;
            }
        }
        await _context.SaveChangesAsync();

        job.Status = "completed";
    }
}
