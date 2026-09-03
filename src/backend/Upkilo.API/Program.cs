using Serilog;
using Fido2NetLib;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Upkilo.API.Middleware;
using Upkilo.API.Hubs;
using Upkilo.API.Services;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Infrastructure.Background;
using Upkilo.Infrastructure.Services;
using Upkilo.Infrastructure.Services.Agents;
using Upkilo.Infrastructure.Services.AI;
using Upkilo.Infrastructure.Security;
using Hangfire;
using Hangfire.PostgreSql;
using Upkilo.API.Jobs;
using System.Threading.RateLimiting;
using Upkilo.Infrastructure.Resilience;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Upkilo.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using FluentValidation;
using FluentValidation.AspNetCore;
using Upkilo.Infrastructure.Validators;
using Upkilo.Infrastructure.Helpers;
using Prometheus;
using StackExchange.Redis;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Upkilo.Infrastructure.HealthChecks;
using Upkilo.Infrastructure.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using Upkilo.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Identity;
using Upkilo.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// appsettings.Production.json declares every secret as a "${VAR}" placeholder, but .NET does
// NOT expand ${...} — a value only becomes real if App Service (or the environment) supplies
// it. Anything still holding a literal placeholder was never configured, yet it is neither
// null nor whitespace, so `string.IsNullOrWhiteSpace(...)` guards on optional features happily
// treat it as a real value.
//
// That crashed production: AzureServiceBus:ConnectionString kept its placeholder, the
// in-memory-transport fallback never triggered, and MassTransit tried to parse
// "${AZURE_SERVICE_BUS_CONNECTION_STRING}" as a Service Bus connection string —
// FormatException, unhandled, container exited 139 before Kestrel ever bound a port.
//
// Blanking them here makes "unset" actually look unset, so every optional-feature guard and
// fail-fast check behaves as written. Applied to all keys rather than the one that bit us,
// because the same trap is armed on Elasticsearch and anything added later.
var unexpandedPlaceholders = builder.Configuration.AsEnumerable()
    .Where(kv => kv.Value is not null &&
                 System.Text.RegularExpressions.Regex.IsMatch(kv.Value, @"^\$\{[A-Za-z_][A-Za-z0-9_]*\}"))
    .Select(kv => kv.Key)
    .ToList();

if (unexpandedPlaceholders.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(
        unexpandedPlaceholders.Select(k => new KeyValuePair<string, string?>(k, null)));
    Log.Warning(
        "Ignoring {Count} configuration key(s) still holding an unexpanded ${{...}} placeholder: {Keys}. " +
        "These are treated as NOT configured. Set them as App Service application settings if the " +
        "corresponding feature is required.",
        unexpandedPlaceholders.Count, string.Join(", ", unexpandedPlaceholders));
}

// Fail fast at startup if required connection strings are absent.
var redisConn = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

// 1. Redis Configuration for Distributed Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConn;
    options.InstanceName = "Upkilo_";
});

// 2. MassTransit & Azure Service Bus Configuration (replaces RabbitMQ — CRITICAL-01)
var asbConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    if (string.IsNullOrWhiteSpace(asbConnectionString))
    {
        // Development: use true in-memory transport — Azure Service Bus transport requires a
        // commercial MassTransit license even without a host, so we must use UsingInMemory here.
        Log.Warning("AzureServiceBus:ConnectionString not configured. Using in-memory transport (development only).");
        x.UsingInMemory((context, cfg) =>
        {
            cfg.UseMessageRetry(r => r.Immediate(3));
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(asbConnectionString);
            // Retry: 5 attempts with exponential back-off capped at 30 s
            cfg.UseMessageRetry(r => r.Exponential(5,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5)));
            // Circuit breaker: trip after 15% failures in a 1-minute window
            cfg.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                cb.TripThreshold = 15;
                cb.ActiveThreshold = 10;
                cb.ResetInterval = TimeSpan.FromMinutes(5);
            });
            cfg.ConfigureEndpoints(context);
        });
    }
});

// 3. Global Rate Limiting — 1 000 req/min per authenticated user or IP (HIGH-02 fix)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1000,  // 1 000 requests per minute
                SegmentsPerWindow = 6,     // refresh every 10 s
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // R5 fix: tight policy for the public unauthenticated AI Receptionist endpoint.
    // Partitioned by IP so a single bot cannot exhaust Azure OpenAI budget.
    options.AddPolicy("receptionist", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,   // 10 messages per minute per IP
                SegmentsPerWindow = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // C3/OWASP A07: strict auth-endpoint limit — 10 attempts per 15 min per IP
    // Blocks brute-force credential stuffing on login, password-reset, and verify-2fa.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                SegmentsPerWindow = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    // VULN-A11: Kiosk endpoints are AllowAnonymous — limit to 20 req/min per IP
    // to prevent unauthenticated client PII enumeration by email/phone.
    options.AddPolicy("kiosk", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                SegmentsPerWindow = 4,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Public booking endpoints: unauthenticated but DB-heavy — 60 req/min per IP
    // prevents scraping and abuse while allowing legitimate multi-step booking flows.
    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                SegmentsPerWindow = 4,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Integration credential-write endpoints (connect/disconnect/test/api-key):
    // tight per-user/IP limit to slow credential brute-forcing and outbound
    // provider-API abuse from the /test live-verification calls.
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,   // 30 writes per minute per user/IP
                SegmentsPerWindow = 6,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = 429;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IDbConnectionSelector, DbConnectionSelector>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppDbContext).Assembly));
builder.Services.AddScoped<DomainEventInterceptor>();

// Graceful shutdown: drain in-flight requests before the process exits on SIGTERM.
// Azure App Service sends SIGTERM then waits 30s before SIGKILL; we use 25s to stay
// inside that window while the ShutdownTimeout (below) enforces the ceiling.
builder.Services.AddHostedService<Upkilo.API.Infrastructure.GracefulShutdownHandler>();
builder.Services.Configure<HostOptions>(opts =>
{
    opts.ShutdownTimeout = TimeSpan.FromSeconds(25);
});

// Catch missing DI registrations at startup in all environments, not just dev.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// WriteTo sinks are driven entirely by appsettings — production uses JSON formatter,
// development uses the human-readable template. The PiiRedactionEnricher is applied
// here so it runs regardless of which sinks are configured via appsettings.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<PiiRedactionEnricher>()
    .CreateLogger();

builder.Host.UseSerilog();

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// CRITICAL: Enable dynamic JSON globally for ALL Npgsql connections.
// This is required for Dictionary<string, object> properties mapped to JSONB columns.
// Without this, Npgsql 8+ throws "Type 'Dictionary`2' required dynamic JSON serialization".
#pragma warning disable CS0618 // GlobalTypeMapper is obsolete but still necessary
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618

var npgsqlBuilder = new NpgsqlDataSourceBuilder(defaultConn);
npgsqlBuilder.EnableDynamicJson();
var npgsqlDataSource = npgsqlBuilder.Build();
builder.Services.AddSingleton(npgsqlDataSource);
builder.Services.AddSingleton<DatabaseHealthMonitor>();
builder.Services.AddScoped<FailoverInterceptor>();
builder.Services.AddScoped<SearchSyncInterceptor>();
builder.Services.AddScoped<AuditLogInterceptor>();
builder.Services.AddScoped<ReadWriteInterceptor>();
builder.Services.AddSingleton<SlowQueryInterceptor>();

builder.Services.AddResiliencePipeline<string>("default", pipeline =>
{
    pipeline.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });
});

builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
{
    // IMPORTANT: Use npgsqlDataSource (built with EnableDynamicJson) so that
    // Dictionary<string, object> JSONB columns serialize correctly in Npgsql 8+.
    options.UseNpgsql(npgsqlDataSource, npgsqlOpts =>
    {
        npgsqlOpts.EnableRetryOnFailure(maxRetryCount: 3);
    });
});

builder.Services.AddSingleton<Upkilo.Core.Interfaces.IRequestCoalescer, Upkilo.Infrastructure.Helpers.RequestCoalescer>();
builder.Services.AddSingleton<ISystemLoadMonitorService, SystemLoadMonitorService>();
builder.Services.AddSingleton<IBusinessMetrics, PrometheusBusinessMetrics>();
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConn, true);
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddSingleton<IDistributedLockProvider, RedisLockProvider>();

// M-NEW-01 FIX: Duplicate AddStackExchangeRedisCache removed — the namespaced registration
// above (line 56, InstanceName="Upkilo_") is the single authoritative registration.


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();

// P5: Response compression — Brotli primary, gzip fallback for JSON payloads > 1KB
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json", "application/problem+json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.SmallestSize);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Upkilo API", Version = "v1" });
    c.DocInclusionPredicate((version, apiDescription) => true);
    c.CustomSchemaIds(type => type.FullName);
});

builder.Services.AddApiVersioning(options =>
{
    // Controllers that use a literal route (e.g. "api/v1/kiosk", "api/schedule-blocks")
    // instead of the "api/v{version:apiVersion}/..." token carry no discoverable version.
    // Without a default, the versioning middleware rejects them with 400 "Unspecified API
    // version". Assume v1.0 so every route resolves to the single live API version.
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer();

var fido2Config = new Fido2Configuration
{
    ServerDomain = builder.Configuration["fido2:serverDomain"] ?? "localhost",
    ServerName = "Upkilo Auth",
    Origins = builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>() ?? new HashSet<string> { "https://localhost:3000", "http://localhost:3000" },
    TimestampDriftTolerance = builder.Configuration.GetValue<int>("fido2:timestampDriftTolerance", 300000)
};
builder.Services.AddSingleton<IFido2>(new Fido2(fido2Config));

// FIX #9: Resolve JWT secret at runtime via IPostConfigureOptions instead of
// building a second ServiceProvider during configuration (which leaks memory).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer();

builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(sp =>
{
    var secretProvider = sp.GetRequiredService<ISecretProvider>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var environment = sp.GetRequiredService<IHostEnvironment>();
    return new PostConfigureJwtBearerOptions(secretProvider, configuration, environment);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };
        policy.WithOrigins(corsOrigins)
        // x-signalr-user-agent is sent by the SignalR JS client on every /negotiate call;
        // omitting it fails preflight and silently breaks real-time notifications.
        .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With",
                     "X-Correlation-Id", "X-Tenant-Id", "X-Timezone", "Stripe-Signature",
                     "x-signalr-user-agent")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .AllowCredentials();
    });
});

builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !context.User.HasClaim(c => c.Type == "portal_access" && c.Value == "true"))
        .Build();
    options.DefaultPolicy = defaultPolicy;

    options.AddPolicy("ClientPortal", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("portal_access", "true");
    });
});

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConn);
builder.Services.AddScoped<INotificationService, NotificationService>();
// ── Service registrations that were missing entirely ──────────────────────────────────────────
// These controllers compiled but returned HTTP 400 "Unable to resolve service for type 'IXxxService'"
// on every request because the service was never registered in DI (there is no assembly-scan/
// convention registration — only MediatR + validators are bulk-registered). Confirmed at runtime
// for ILocationService and IPaymentService; the rest share the identical, verified defect.
builder.Services.AddScoped<Upkilo.Core.Interfaces.ILocationService, Upkilo.Infrastructure.Services.LocationService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IChatbotContextBuilder, Upkilo.Infrastructure.Services.ChatbotContextBuilder>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IChatbotService, Upkilo.Infrastructure.Services.ChatbotService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IAdCampaignService, Upkilo.Infrastructure.Services.AdCampaignService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IAttendanceService, Upkilo.Infrastructure.Services.AttendanceService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IFinancialProjectionService, Upkilo.Infrastructure.Services.FinancialProjectionService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IGiftCertificateService, Upkilo.Infrastructure.Services.GiftCertificateService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IMembershipService, Upkilo.Infrastructure.Services.MembershipService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IPaymentService, Upkilo.Infrastructure.Services.PaymentService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ICartService, Upkilo.Infrastructure.Services.CartService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ISessionService, Upkilo.Infrastructure.Services.SessionService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ISetupWizardService, Upkilo.Infrastructure.Services.SetupWizardService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IPayoutService, Upkilo.Infrastructure.Services.PayoutService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ICommissionService, Upkilo.Infrastructure.Services.CommissionService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ITaxService, Upkilo.Infrastructure.Services.TaxService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ITourService, Upkilo.Infrastructure.Services.TourService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IConsentService, Upkilo.Infrastructure.Services.ConsentService>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.IComplianceEvidenceService, Upkilo.Infrastructure.Services.ComplianceEvidenceService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IExportService, Upkilo.Infrastructure.Services.ExportService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IImportService, Upkilo.Infrastructure.Services.ImportService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IMigrationService, Upkilo.Infrastructure.Services.MigrationService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ILoyaltyService, Upkilo.Infrastructure.Services.LoyaltyService>();
// LoyaltyController injects the CONCRETE LoyaltyService (it calls GetSummaryAsync, which is not
// on ILoyaltyService). Registering only the interface left the concrete type unresolvable, so
// every /api/v1/loyalty/* action failed with 400 "Unable to resolve service for type ...".
builder.Services.AddScoped<Upkilo.Infrastructure.Services.LoyaltyService>();
// Same problem in SearchController — SearchEnhancementService was never registered at all.
builder.Services.AddScoped<Upkilo.Infrastructure.Services.SearchEnhancementService>();
// Consumed by both PricingHealthCheck and the admin validate endpoint.
builder.Services.AddScoped<Upkilo.Infrastructure.Services.PricingIntegrityService>();
// Keeps tenant currency aligned with the connected Stripe account. Used by the account.updated
// webhook, the Connect return path, and the backfill endpoint.
builder.Services.AddScoped<Upkilo.Infrastructure.Services.TenantCurrencySyncService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ICampaignAnalyticsService, Upkilo.Infrastructure.Services.CampaignAnalyticsService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IPushNotificationService, Upkilo.Infrastructure.Services.PushNotificationService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IInvoiceService, Upkilo.Infrastructure.Services.InvoiceService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ICopywritingAgent, Upkilo.Infrastructure.Agents.CopywritingAgent>();
builder.Services.AddScoped<SmtpEmailProvider>();
builder.Services.AddScoped<NotificationFallbackService>();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddScoped<ICalendarService, OutlookCalendarService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<SlackNotificationService>();
builder.Services.AddScoped<ITriggerDispatcher, TriggerDispatcher>();
builder.Services.AddScoped<ITimezoneService, TimezoneService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// SmsUsageTracker wraps SmsService to record SMS usage and report Stripe overages
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<ISmsService>(sp =>
    new Upkilo.Infrastructure.Services.SmsUsageTracker(
        sp.GetRequiredService<SmsService>(),
        sp.GetRequiredService<Upkilo.Infrastructure.Data.AppDbContext>(),
        sp.GetRequiredService<Upkilo.Core.Interfaces.ISubscriptionService>(),
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Upkilo.Infrastructure.Services.SmsUsageTracker>>()));
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.Workflow.IWorkflowStepExecutor, WorkflowStepExecutor>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
// F-02: DNS-pinned outbound client for tenant-supplied webhook URLs (SSRF-safe at connect time).
builder.Services.AddHttpClient(Upkilo.Infrastructure.Services.SsrfGuard.PinnedClientName,
        c => c.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => Upkilo.Infrastructure.Services.SsrfGuard.CreatePinnedHandler());
builder.Services.AddScoped<Upkilo.Core.Interfaces.ITwoFactorService, Upkilo.Infrastructure.Services.TwoFactorService>();
// The single entitlement authority. Registered before ISubscriptionService because
// SubscriptionService now delegates its feature and limit questions here rather than reading
// plan mappings itself — there must be exactly one implementation of "what may this tenant do".
builder.Services.AddScoped<Upkilo.Core.Interfaces.IEntitlementService, Upkilo.Infrastructure.Services.EntitlementService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.ISubscriptionService, Upkilo.Infrastructure.Services.SubscriptionService>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.SubscriptionDowngradeHandler>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.SubscriptionPlanVersioningService>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.UpsellTriggerService>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.SiemLoggingService>();
builder.Services.AddSingleton<Upkilo.Core.Interfaces.IEncryptionService, Upkilo.Infrastructure.Services.AesGcmEncryptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<SsoIntegrationService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IAIService, AzureOpenAIService>();
// SC4: Circuit breaker + retry for Azure OpenAI external calls
builder.Services.AddHttpClient<AzureOpenAIService>()
    .AddStandardResilienceHandler(cfg =>
    {
        cfg.CircuitBreaker.FailureRatio = 0.5;
        cfg.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        cfg.CircuitBreaker.MinimumThroughput = 5;
        cfg.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        cfg.Retry.MaxRetryAttempts = 3;
        cfg.Retry.Delay = TimeSpan.FromSeconds(1);
        cfg.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
    });
builder.Services.AddScoped<Upkilo.Core.Interfaces.IAiModelResolver, Upkilo.Infrastructure.Services.AiModelResolver>();
// ReviewRequestJob is run via Hangfire (see AddOrUpdate below) — no HostedService needed
builder.Services.AddSingleton<Upkilo.Core.Interfaces.IPiiScrubberService, Upkilo.Infrastructure.Services.PiiScrubberService>();
builder.Services.AddScoped<IAIIntentService, AIIntentService>();
builder.Services.AddScoped<Upkilo.Core.Interfaces.IChurnPredictorAgent, Upkilo.Infrastructure.Services.Agents.ChurnPredictorAgent>();
builder.Services.AddScoped<VoiceAgentService>();
builder.Services.AddScoped<ISandboxService, SandboxService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IMarketingAutomationService, MarketingAutomationService>();
builder.Services.AddScoped<IMarketingIntegrationService, MarketingIntegrationService>();
// Warn at startup when key observability/alerting config is missing — prevents silent failure.
// Both spellings, because the two environments supply different ones and only one of them
// was ever read.
//
// Azure App Service sets APPLICATIONINSIGHTS_CONNECTION_STRING — that is the name the portal,
// the ARM templates and `az webapp config appsettings` all use. .NET's environment-variable
// provider maps that to the config key APPLICATIONINSIGHTS_CONNECTION_STRING verbatim; the
// colon form below would need the env var spelled ApplicationInsights__ConnectionString, with
// a DOUBLE UNDERSCORE. Reading only the colon form meant aiConnStr was null in production even
// though the setting was present and correct, so aiConfigured was false, the disabled no-op
// TelemetryClient was registered, and AddApplicationInsightsTelemetry — which reads the Azure
// name natively and would have worked — was never reached.
//
// The result was an API that logged "not configured" at every start and sent no telemetry at
// all, while the portal showed Application Insights wired up. A 500 in production left no
// trace anywhere: not in App Insights, and not on disk either, since App Service application
// logging defaults to Off. This is the same defect the codebase already documents for Stripe
// ("Colon form — see PaymentService.cs for why 'Stripe--SecretKey' never resolved").
//
// SECOND DEFECT, found the hard way: resolving the value here is not enough, because
// AddApplicationInsightsTelemetry(IConfiguration) goes and looks it up AGAIN under
// ApplicationInsights:ConnectionString. Reading the Azure name satisfied the check below while
// the SDK still found nothing, so registration proceeded and its OpenTelemetry Azure Monitor
// metric exporter was constructed with an empty connection string:
//
//   InvalidOperationException: Connection String Error: Required keyword 'InstrumentationKey'
//   is missing in connection string.
//     at Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorMetricExporter..ctor
//
// That throws during Host.StartAsync, before Kestrel binds a port, so the container exits and
// App Service serves its own 503 HTML page. The deploy workflow polls /ready and pipes the
// response to jq, so the HTML surfaced as "jq: parse error: Invalid numeric literal at line 1,
// column 10" rather than as "the app is not starting".
//
// Strictly worse than the original bug: before, this resolved to null, the guard skipped
// registration and the API came up with telemetry disabled. Now the connection string is passed
// EXPLICITLY to the SDK below, so the value that is validated here is the value it uses.
var aiConnStr = builder.Configuration["ApplicationInsights:ConnectionString"]
                ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

// A connection string without InstrumentationKey= is what the exporter rejects, so reject it
// here first and degrade instead. Telemetry is optional; it must never take the API down.
var aiConfigured = !string.IsNullOrWhiteSpace(aiConnStr)
                   && !aiConnStr.StartsWith("${", StringComparison.Ordinal)
                   && aiConnStr.Contains("InstrumentationKey=", StringComparison.OrdinalIgnoreCase);

if (!string.IsNullOrWhiteSpace(aiConnStr) && !aiConfigured)
{
    Log.Warning(
        "Application Insights connection string is present but unusable (no InstrumentationKey=, "
        + "or an unexpanded ${{...}} placeholder) — telemetry is disabled rather than failing startup.");
}

if (!aiConfigured)
{
    Log.Warning("Application Insights connection string is not configured (checked ApplicationInsights:ConnectionString and APPLICATIONINSIGHTS_CONNECTION_STRING) — error telemetry will not be sent to Azure Monitor.");

    // AddApplicationInsightsTelemetry also registers TelemetryClient, which TelemetryService
    // (and transitively GlobalExceptionHandler) require. Skipping registration entirely
    // breaks DI validation at startup, so supply a DISABLED client: telemetry calls become
    // no-ops instead of an unresolvable dependency.
    // DisableTelemetry alone is not enough: TelemetryConfiguration still parses its
    // ConnectionString, and a null one throws NullReferenceException inside
    // ConnectionString.Parse. Supply a syntactically valid all-zero key — nothing is
    // transmitted because DisableTelemetry short-circuits every send.
    builder.Services.AddSingleton(_ => new Microsoft.ApplicationInsights.TelemetryClient(
        new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            DisableTelemetry = true
        }));
}
else
{
    // Registration is GUARDED. Microsoft.ApplicationInsights.AspNetCore 3.x is
    // OpenTelemetry-based, so AddApplicationInsightsTelemetry registers an
    // AzureMonitorMetricExporter that throws at DI-resolution time when no connection
    // string is present:
    //   InvalidOperationException: A connection string was not found.
    // That is an UNHANDLED startup exception — the container exits 139 before Kestrel
    // binds a port, so the app is simply unreachable rather than degraded. Telemetry is
    // optional; it must never be able to take the API down.
    //
    // The connection string is assigned EXPLICITLY rather than passing builder.Configuration
    // and hoping the SDK finds it. The configuration overload re-resolves the value from
    // ApplicationInsights:ConnectionString, which is NOT the key Azure App Service sets —
    // App Service sets APPLICATIONINSIGHTS_CONNECTION_STRING, and only the double-underscore
    // spelling ApplicationInsights__ConnectionString would map to the colon key. So the check
    // above could pass on the Azure name while the SDK independently found nothing and built
    // its exporter with an empty string. Passing the validated value removes the second lookup
    // entirely: what is checked is what is used.
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnStr;
    });
}

var pdKey = builder.Configuration["PagerDuty:IntegrationKey"];
if (string.IsNullOrWhiteSpace(pdKey))
    Log.Warning("PagerDuty:IntegrationKey is not configured — on-call alerting is disabled. Set PagerDuty__IntegrationKey in App Service config.");
// R1 fix: TelemetryService must be singleton — consumed by singleton GlobalExceptionHandler.
// TelemetryClient (AppInsights) and ILogger are both singleton-safe.
builder.Services.AddSingleton<TelemetryService>();
// R2 fix: IAIDashboardService was missing — crashed ChurnPredictorAgent DI validation.
builder.Services.AddScoped<IAIDashboardService, AIDashboardService>();
// R3 fix: ITenantContextAccessor was missing — crashed TenantJobContextFilter DI validation.
// AsyncLocal-based so singleton lifetime is correct.
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();

// Add OpenTelemetry Tracing and Metrics — with Jaeger exporter for distributed tracing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Upkilo.API"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddOtlpExporter(otlp =>
               {
                   // Exports to Jaeger (via OTLP); configure OTEL_EXPORTER_OTLP_ENDPOINT env var
                   otlp.Endpoint = new Uri(
                       builder.Configuration["Jaeger:OtlpEndpoint"] ?? "http://localhost:4317");
               });
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddPrometheusExporter();
    });
// SC4: Circuit breaker + retry for Stripe (billing-critical — must not cascade fail)
builder.Services.AddHttpClient("stripe")
    .AddStandardResilienceHandler(cfg =>
    {
        cfg.CircuitBreaker.FailureRatio = 0.5;
        cfg.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        cfg.CircuitBreaker.MinimumThroughput = 3;
        cfg.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        cfg.Retry.MaxRetryAttempts = 3;
        cfg.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });
// SC4: Circuit breaker for SendGrid (email delivery)
builder.Services.AddHttpClient<SmtpEmailProvider>()
    .AddStandardResilienceHandler();
// SC4: SmsService uses Twilio SDK (not raw HttpClient) — no AddHttpClient registration needed
// SC4: Circuit breaker + retry for Mailchimp/ActiveCampaign/external marketing APIs
builder.Services.AddHttpClient<MarketingIntegrationService>()
    .AddStandardResilienceHandler();
// SC4: Circuit breaker for Discord webhook
builder.Services.AddHttpClient<DiscordNotificationService>()
    .AddStandardResilienceHandler();
// RazorpayService was never registered here — PaymentsController's razorpay/order and
// razorpay/verify actions resolve it via HttpContext.RequestServices.GetRequiredService,
// which throws InvalidOperationException for an unregistered concrete type. Every existing
// call to those actions would have failed before reaching Razorpay at all. Given the same
// resilience treatment as the other external HTTP integrations above — this is a real-money
// payment API, not less critical than SendGrid or Discord.
builder.Services.AddHttpClient<RazorpayService>()
    .AddStandardResilienceHandler();
builder.Services.AddScoped<MarketingAutomationOrchestratorJob>();
builder.Services.AddScoped<BillingAlertJob>();
builder.Services.AddScoped<EscalationFollowupJob>();
builder.Services.AddScoped<AuditLogRetentionJob>();

// Audit System — query/export service, buffered writer, compliance, AI audit
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<BufferedAuditLogService>();
builder.Services.AddSingleton<IAuditLogService>(sp => sp.GetRequiredService<BufferedAuditLogService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BufferedAuditLogService>());
builder.Services.AddScoped<ComplianceReportService>();
builder.Services.AddScoped<IAIAuditService, AIAuditService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<SecurityScannerService>();
builder.Services.AddScoped<OperationalDashboardService>();
builder.Services.AddScoped<PagerDutyService>();
builder.Services.AddScoped<ErrorMonitoringService>();
builder.Services.AddScoped<HealthMonitoringService>();
builder.Services.AddScoped<DeveloperPortalService>();
builder.Services.AddScoped<IntegrationMarketplaceService>();
builder.Services.AddScoped<PredictiveAnalyticsService>();
builder.Services.AddScoped<PluginEcosystemService>();
builder.Services.AddScoped<DomainManagementService>();
builder.Services.AddScoped<LiveChatService>();
builder.Services.AddScoped<WaiverPdfService>();
builder.Services.AddScoped<DataWarehouseSyncService>();
builder.Services.AddScoped<PwaOfflineSyncService>();
builder.Services.AddScoped<XeroIntegrationService>();
builder.Services.AddScoped<KlaviyoService>();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<SmsA2pRegistrationService>();
builder.Services.AddScoped<Upkilo.Infrastructure.Jobs.DataWarehouseSyncJob>();

builder.Services.AddScoped<Upkilo.Infrastructure.Services.ClientRetentionService>();
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.OnboardingDripJob>();
// Nightly retention nudge: clients whose last visit is older than the service's own
// RebookAfterDays interval. Consent-gated — see the notes on the job itself.
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.RebookReminderJob>();

// Phase 2 — Fill My Calendar AI
builder.Services.AddScoped<Upkilo.Infrastructure.Services.CalendarGapAnalyzer>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.ClientMatchingService>();

// Phase 2 — Business Health + Weekly Digest
builder.Services.AddScoped<Upkilo.Infrastructure.Services.BusinessHealthService>();
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.WeeklyDigestJob>();

// Phase 2 — Affiliate Payout Job
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.AffiliatePayoutJob>();

// Phase 2 — AI Receptionist (Claude function calling + Twilio inbound SMS)
builder.Services.AddScoped<Upkilo.Infrastructure.Services.AiReceptionistService>();

// Phase 2 — Waitlist Auto-Fill (real-time slot notification on cancellation)
builder.Services.AddScoped<Upkilo.Infrastructure.Services.WaitlistAutoFillService>();

// Phase 3 — Revenue Forecast + AI Recommendations (Days 74-75)
builder.Services.AddScoped<Upkilo.Infrastructure.Services.RevenueForecastService>();

// Phase 3 — Membership Dunning + Recovery (Days 78-79)
builder.Services.AddScoped<Upkilo.Infrastructure.Services.MembershipDunningService>();
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.MembershipDunningJob>();

// Password hash migration — opt-in via PasswordMigration:Enabled in config
builder.Services.AddHostedService<Upkilo.Infrastructure.Services.PasswordMigrationJob>();

// AI Safety & Automation Safety
builder.Services.AddSingleton<Upkilo.Core.Interfaces.IPromptSanitizer, PromptSanitizer>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddScoped<IPromptVersioningService, PromptVersioningService>();
builder.Services.AddScoped<IAutomationSafetyService, AutomationSafetyService>();
builder.Services.AddSingleton<Upkilo.Infrastructure.Services.JsonSchemaValidator>();

builder.Services.AddSingleton<Upkilo.Infrastructure.Services.FeatureFlagService>();
builder.Services.AddSingleton<Upkilo.Infrastructure.Events.ProjectionRebuilder>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.IProactiveMessagingService, Upkilo.Infrastructure.Services.ProactiveMessagingService>();

// FIX #10: Register missing job types so they can be scheduled
builder.Services.AddScoped<BillingReconciliationJob>();
builder.Services.AddScoped<DlqReconciliationJob>();
builder.Services.AddScoped<BookingReminderJob>();
builder.Services.AddScoped<SessionCleanupJob>();
builder.Services.AddScoped<SlotExpiryJob>();
builder.Services.AddScoped<DunningAutomationJob>();
builder.Services.AddScoped<ReviewRequestJob>();
builder.Services.AddScoped<DataRetentionJob>();
builder.Services.AddScoped<Upkilo.Infrastructure.Jobs.GdprDataDeletionJob>();
builder.Services.AddScoped<Upkilo.Infrastructure.Services.CatalogCacheService>(); // SC7: Redis L2 catalog cache
// Outbox: canonical processor (exponential backoff + DLQ). Scheduled via Hangfire so
// DisableConcurrentExecution prevents duplicate delivery across replicas.
// NOTE: Background.OutboxProcessor and Services.OutboxDispatcher are NOT registered —
// they are superseded by this Hangfire job. Do not AddHostedService them.
builder.Services.AddScoped<Upkilo.Infrastructure.Jobs.OutboxProcessor>();

builder.Services.AddSingleton<TenantJobContextFilter>();
builder.Services.AddHangfire((sp, config) => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseFilter(sp.GetRequiredService<TenantJobContextFilter>())
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

// MED-07: Global retry policy — 3 attempts with escalating delays.
// Jobs that fail all 3 attempts are marked Failed in the dashboard for manual review.
GlobalJobFilters.Filters.Add(new Hangfire.AutomaticRetryAttribute
{
    Attempts = 3,
    DelaysInSeconds = new[] { 60, 300, 900 },
    OnAttemptsExceeded = Hangfire.AttemptsExceededAction.Fail
});

// FIX #8: Register all health checks including Hangfire and Elasticsearch
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql", tags: new[] { "ready", "db" })
    .AddCheck<Upkilo.Infrastructure.HealthChecks.ConnectionPoolHealthCheck>("connection-pool", tags: new[] { "ready", "db" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "cache" })
    .AddCheck<ApplicationHealthCheck>("app", tags: new[] { "live" })
    .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready", "jobs" })
    // Pricing fails silently — a missing price list still returns 200 and renders "Contact us"
    // on every plan, so nothing alerts while the site has quietly stopped selling.
    .AddCheck<Upkilo.Infrastructure.HealthChecks.PricingHealthCheck>("pricing", tags: new[] { "ready", "billing" })
    // Elasticsearch is OPTIONAL — search degrades to plain SQL when it is absent, and it is
    // deliberately not provisioned. It must therefore NOT carry the "ready" tag: readiness
    // decides whether this instance serves traffic at all, and deploy.yml aborts (then rolls
    // back) when /ready is Unhealthy.
    //
    // Tagged "ready" it made an unprovisioned, non-essential component fail every deployment
    // while postgresql, redis, hangfire, pricing and the bus were all Healthy. deploy.yml even
    // has a separate step that inspects Elasticsearch *non-fatally* — the intent was always
    // that it should not block. Reported under "search" so it stays visible on /health.
    .AddElasticsearch(
        builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200",
        name: "elasticsearch",
        tags: new[] { "search" },
        timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

// VULN-006 FIX: Swagger UI restricted to Development only.
// Staging previously exposed the full API schema to any unauthenticated user with network
// access, leaking endpoint structure, parameter schemas, and auth requirements.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Log.Information("Swagger UI enabled for Development");
}

if (app.Environment.IsDevelopment())
{
    // HTTPS not enforced locally. Add Jwt__Secret to appsettings.Development.json.
}
else
{
    // Fail fast if HTTPS is not configured — prevents silent plaintext credential exposure
    // if ASPNETCORE_ENVIRONMENT is accidentally set to a non-Development value on an HTTP host.
    var serverAddresses = app.Urls;
    var hasHttps = serverAddresses.Any(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                   || builder.Configuration.GetValue<bool>("ForceHttps");
    if (!hasHttps)
    {
        Log.Warning("No HTTPS URL detected. Ensure the reverse proxy (nginx/Azure App Gateway) terminates TLS.");
    }
    app.UseHttpsRedirection();
    app.UseHsts();
}

// ========================================================================
// FIX #2: Wire all critical middleware in correct order
// Order: Security → Observability → Routing → Auth → Business → Controllers
// ========================================================================

// 0a. Response compression — must be first to compress all downstream responses
app.UseResponseCompression();

// 0. Global Exception Handler
app.UseExceptionHandler();

// 1. Security headers — must be first to protect all responses
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Correlation ID — early so all logs include it
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Request timing — adds X-Response-Time header
app.UseMiddleware<RequestTimingMiddleware>();

// 4. Request timeout — 30s max per request
app.UseMiddleware<RequestTimeoutMiddleware>();

// 5. Metrics — Prometheus HTTP request tracking
app.UseMiddleware<MetricsMiddleware>();

// 6. Load shedding — reject when system overloaded (before expensive auth work)
app.UseMiddleware<LoadSheddingMiddleware>();

// RFC 8594: Deprecation headers for sunset endpoints
app.UseMiddleware<ApiDeprecationMiddleware>();

app.UseCors("AllowFrontend");
app.UseMiddleware<SandboxMiddleware>();
app.UseRouting();

// File upload and SSRF protection (early pipeline — before auth to fail fast on bad uploads)
app.UseMiddleware<SsrfPreventionMiddleware>();
app.UseMiddleware<FileUploadValidationMiddleware>();

app.UseAuthentication();

// VULN-004 FIX: Rate limiter MUST run after UseAuthentication so User.Identity.Name is
// populated and the global limiter can partition by authenticated user (not just IP).
// Placing it before auth meant every partition key fell back to the IP address.
app.UseRateLimiter();

// API Key and CSRF validations must happen after initial auth
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// 7. Subscription enforcer — check plan limits after auth resolves tenant
app.UseMiddleware<SubscriptionEnforcerMiddleware>();

// 8. Tenant concurrency limiter — per-tenant request cap
app.UseMiddleware<TenantConcurrencyLimiterMiddleware>();

// Day 87: Per-tenant Redis-backed rate limiter (tier-aware: Starter=1k/day, Professional=5k, Business=10k)
app.UseTenantRateLimit();

// 9. Security audit — log auth failures, rate limits, slow requests
app.UseMiddleware<SecurityAuditMiddleware>();

// Health endpoints — /health (liveness) and /ready (readiness with DB/Redis checks)
// Public health endpoints: expose only pass/fail status — no internal topology.
// Detailed check data (memory, queues, version) lives behind authentication on
// GET /api/v1/super-admin/health which requires the SuperAdmin role.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    }
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        // Only expose per-check name + status — no descriptions, data, or durations
        // that would reveal internal service topology to unauthenticated callers.
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString()
            })
        });
    }
});

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// FIX #3: Secure Hangfire dashboard with authentication filter
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthFilter() }
});

// Schedule all recurring background jobs
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Outbox processor — every 30 seconds. DisableConcurrentExecution (set on the job class)
    // ensures only one Hangfire worker processes the outbox at a time across all replicas.
    recurringJobManager.AddOrUpdate<Upkilo.Infrastructure.Jobs.OutboxProcessor>(
        "outbox-processor",
        job => job.ProcessPendingMessagesAsync(),
        "*/30 * * * * *"); // every 30 seconds (second-level cron)

    // Marketing Automation Orchestrator — daily at 2 AM UTC
    recurringJobManager.AddOrUpdate<MarketingAutomationOrchestratorJob>(
        "marketing-automation-orchestrator",
        job => job.ExecuteAsync(),
        Cron.Daily(2));

    // Data Retention — daily at 03:00 UTC, purges audit logs older than 365 days
    recurringJobManager.AddOrUpdate<Upkilo.Infrastructure.Jobs.DataRetentionJob>(
        "data-retention",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily(3));

    // Billing Alerts — every 6 hours
    recurringJobManager.AddOrUpdate<BillingAlertJob>(
        "billing-alerts",
        job => job.ExecuteAsync(),
        Cron.HourInterval(6));

    // Escalation Follow-ups — daily at 4 AM UTC
    recurringJobManager.AddOrUpdate<EscalationFollowupJob>(
        "escalation-followups",
        job => job.ExecuteAsync(),
        Cron.Daily(4));

    // Audit Log Retention — daily at 1 AM UTC, per-tenant retention policy (default 90 days)
    recurringJobManager.AddOrUpdate<AuditLogRetentionJob>(
        "audit-log-retention",
        job => job.ExecuteAsync(),
        Cron.Daily(1));

    // FIX #10: Schedule all previously-unscheduled background jobs

    // Billing Reconciliation — daily at 5 AM UTC, syncs with Stripe
    recurringJobManager.AddOrUpdate<BillingReconciliationJob>(
        "billing-reconciliation",
        job => job.ExecuteAsync(),
        Cron.Daily(5));

    // Booking Reminders — every 15 minutes, sends upcoming appointment reminders
    recurringJobManager.AddOrUpdate<BookingReminderJob>(
        "booking-reminders",
        job => job.ExecuteAsync(),
        "*/15 * * * *");

    // Session Cleanup — every 2 hours, removes expired sessions
    recurringJobManager.AddOrUpdate<SessionCleanupJob>(
        "session-cleanup",
        job => job.ExecuteAsync(),
        Cron.HourInterval(2));

    // Slot Expiry — every 5 minutes, releases expired booking holds
    recurringJobManager.AddOrUpdate<SlotExpiryJob>(
        "slot-expiry",
        job => job.ExecuteAsync(),
        "*/5 * * * *");

    // Dunning Automation — daily at 6 AM UTC, payment retry + suspension
    recurringJobManager.AddOrUpdate<DunningAutomationJob>(
        "dunning-automation",
        job => job.ExecuteAsync(),
        Cron.Daily(6));

    // Review Requests — daily at 10 AM UTC, sends post-booking review requests
    recurringJobManager.AddOrUpdate<ReviewRequestJob>(
        "review-requests",
        job => job.ExecuteAsync(),
        Cron.Daily(10));

    // Churn Retention — daily at 9 AM UTC, AI-driven re-engagement for at-risk clients
    recurringJobManager.AddOrUpdate<ChurnRetentionJob>(
        "churn-retention",
        job => job.ExecuteAsync(),
        Cron.Daily(9));

    // DLQ Auto-Reconciliation — hourly
    recurringJobManager.AddOrUpdate<DlqReconciliationJob>(
        "dlq-reconciliation",
        job => job.ExecuteAsync(),
        Cron.Hourly());
}

// Seed pricing plans on startup — idempotent (PricingSeeder guards with AnyAsync).
// Registration hard-crashes if the Free plan is absent, so this must run before first request.
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<Upkilo.Infrastructure.Data.AppDbContext>();
    await Upkilo.Infrastructure.Data.Seeders.PricingSeeder.SeedAsync(seedDb);
}

// Seed realistic dummy data for local development and testing.
if (app.Environment.IsDevelopment())
{
    using var devSeedScope = app.Services.CreateScope();
    var devDb = devSeedScope.ServiceProvider.GetRequiredService<Upkilo.Infrastructure.Data.AppDbContext>();
    await Upkilo.Infrastructure.Data.Seeders.DevDataSeeder.SeedAsync(devDb);
}

app.Run();

// Expose Program for WebApplicationFactory in contract tests
public partial class Program { }

// =======================================================================
// FIX #9: PostConfigure pattern for JWT — resolves ISecretProvider at runtime
// instead of building a second ServiceProvider during configuration.
// =======================================================================
public class PostConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly ISecretProvider _secretProvider;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PostConfigureJwtBearerOptions(ISecretProvider secretProvider, IConfiguration configuration, IHostEnvironment environment)
    {
        _secretProvider = secretProvider;
        _configuration = configuration;
        _environment = environment;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var jwtSecret = _secretProvider.GetSecret("Jwt:Secret");

        if (string.IsNullOrEmpty(jwtSecret))
        {
            if (_environment.IsProduction() || _environment.IsStaging())
            {
                throw new InvalidOperationException(
                    "FATAL: JWT secret not found in Key Vault. " +
                    "Production/Staging MUST use Azure Key Vault for JWT secrets. " +
                    "Set the 'Jwt:Secret' secret in Key Vault before deploying.");
            }

            // Development-only fallback — load from config file (appsettings.Development.json
            // or environment variable), never generate a random key (sessions would invalidate on restart).
            jwtSecret = _configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
                throw new InvalidOperationException(
                    "Jwt:Secret is not configured. In development, set it in appsettings.Development.json " +
                    "or the Jwt__Secret environment variable (min 32 chars, use: openssl rand -hex 32).");
            Log.Warning("⚠️ Using development JWT key from config. MUST NOT appear in Production logs.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sid = context.Principal?.FindFirstValue("sid");
                if (!string.IsNullOrEmpty(sid))
                {
                    try
                    {
                        var cache = context.HttpContext.RequestServices
                            .GetRequiredService<IDistributedCache>();
                        var revoked = await cache.GetStringAsync($"blacklist:sid:{sid}");
                        if (revoked != null)
                            context.Fail("Token has been revoked");
                    }
                    catch (Exception ex)
                    {
                        // Fail-closed: when Redis is unavailable we cannot confirm the token
                        // has not been revoked, so we reject the request.  This prevents
                        // a Redis outage from disabling the entire revocation system.
                        // Clients must re-authenticate once Redis recovers (typically seconds).
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning(ex, "Redis unavailable during JWT revocation check for sid {Sid}. Rejecting request (fail-closed).", sid);
                        context.Fail("Token revocation check unavailable. Please re-authenticate.");
                    }
                }
            },
            OnMessageReceived = async context =>
            {
                // SignalR: browsers cannot send custom headers on WebSocket/SSE upgrades,
                // so the client passes a short-lived opaque ticket (not the JWT) in the
                // query string.  The ticket is exchanged for the real JWT here and deleted
                // immediately (single-use, 30-second TTL). This prevents JWT tokens from
                // appearing in web-server access logs, browser history, or Referer headers.
                // Client flow: POST /api/v1/signalr/ticket → { ticket } → connect?access_token={ticket}
                //
                // The ticket arrives in the query string only on the WebSocket/SSE upgrade. The
                // preceding /negotiate call is a normal HTTP POST, where the SignalR client sends
                // the same value as an `Authorization: Bearer` header instead — so accept both,
                // otherwise negotiate fails 401 and the connection never reaches the upgrade.
                var ticket = context.Request.Query["access_token"].ToString();
                if (string.IsNullOrEmpty(ticket) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        ticket = authHeader["Bearer ".Length..].Trim();
                }

                if (!string.IsNullOrEmpty(ticket) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    // Reject raw JWTs passed directly (they start with "ey")
                    if (!ticket.StartsWith("ey", StringComparison.Ordinal))
                    {
                        var cache = context.HttpContext.RequestServices
                            .GetRequiredService<IDistributedCache>();
                        var jwt = await cache.GetStringAsync($"signalr:ticket:{ticket}");
                        if (!string.IsNullOrEmpty(jwt))
                        {
                            await cache.RemoveAsync($"signalr:ticket:{ticket}");
                            context.Token = jwt;
                        }
                    }
                    return;
                }

                // VULN-001 FIX: Read the JWT from the HttpOnly cookie for SPA requests.
                // The server issues the token ONLY as an HttpOnly cookie (SetAuthCookie),
                // never in the response body, so JS cannot read it via document.cookie.
                // Without this handler the Bearer middleware ignores the Cookie header and
                // every authenticated REST request would be treated as unauthenticated.
                if (context.Request.Cookies.TryGetValue("token", out var cookieToken) &&
                    !string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }
            }
        };
    }
}
