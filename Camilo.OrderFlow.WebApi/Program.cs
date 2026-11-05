using Camilo.OrderFlow.BuildingBlocks.Infrastructure.DependencyInjection;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.DependencyInjection;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.DependencyInjection;
using Camilo.OrderFlow.WebApi.Errors;
using Camilo.OrderFlow.WebApi.Extensions;
using Camilo.OrderFlow.WebApi.Infrastructure;
using Camilo.OrderFlow.WebApi.Infrastructure.Http;
using Camilo.OrderFlow.WebApi.Middleware;
using Camilo.OrderFlow.WebApi.Observability.Health;
using Camilo.OrderFlow.WebApi.Observability.OpenTelemetry;
using Camilo.OrderFlow.WebApi.Security;
using Camilo.OrderFlow.WebApi.Swagger;
using Camilo.OrderFlow.WebApi.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel request limits (values from appsettings "Kestrel:Limits") ──────────────────────────
builder.WebHost.ConfigureKestrel((ctx, kestrel) =>
{
    var limits = ctx.Configuration.GetSection("Kestrel:Limits");
    kestrel.Limits.MaxRequestBodySize =
        limits.GetValue("MaxRequestBodySizeBytes", 10L * 1024 * 1024);   // default 10 MB
    kestrel.Limits.RequestHeadersTimeout =
        TimeSpan.FromSeconds(limits.GetValue("RequestHeadersTimeoutSeconds", 30));
    kestrel.Limits.KeepAliveTimeout =
        TimeSpan.FromSeconds(limits.GetValue("KeepAliveTimeoutSeconds", 120));
});

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    .Enrich.WithThreadId()
    .Enrich.WithSpan(new SpanOptions      // adds traceId + spanId from Activity.Current → enables Trace-to-Logs in Grafana
    {
        LogEventPropertiesNames = new SpanLogEventPropertiesNames
        {
            TraceId = "traceId",    // lowercase → stable regex in Loki derivedFields
            SpanId = "spanId",     //             and clean `| json | traceId=` queries
        },
    })
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj} {Properties:j}{NewLine}{Exception}"));

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
// Attach to outgoing HttpClients:
// builder.Services.AddHttpClient("ExternalClient").AddHttpMessageHandler<CorrelationIdHandler>();

builder.Services.AddBuildingBlocks();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Optional Redis L2 for HybridCache — activated when "Cache:Redis" is a non-empty connection string
if (builder.Configuration["Cache:Redis"] is string redis && !string.IsNullOrWhiteSpace(redis))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redis);
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// JWT Bearer authentication — registered here because JwtBearer is a WebApi NuGet dependency.
// JwtOptions are validated by DataAnnotations + ValidateOnStart inside AddIdentityInfrastructure.
// We configure via IOptions<JwtOptions> (not an eager builder.Configuration read) so that
// test fixtures can override the Jwt section via ConfigureAppConfiguration and have those
// values reflected here without a restart.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOpts, jwtOpts) =>
    {
        var j = jwtOpts.Value;
        bearerOpts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = j.Issuer,
            ValidAudience = j.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(j.Key)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddControllers();

// API versioning (header: api-version, default v1.0)
builder.Services.AddAppApiVersioning();

// Swagger + JWT bearer + versioned docs
builder.Services.AddAppSwagger();

builder.Services.AddOpenTelemetryObservability(builder.Configuration);
builder.Services.AddAppHealthChecks(builder.Configuration);

// Resilient HTTP clients (ErpClient) — timeout + retry + circuit breaker
builder.Services.AddResilientHttpClients(builder.Configuration);

// Rate limiting — "default" (all endpoints) + "auth" (authentication endpoints)
builder.Services.AddAppRateLimiting(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty;
        ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.Items["CorrelationId"]?.ToString();
    };
});

// Security headers — configurable via "SecurityHeaders" in appsettings; fail-fast via ValidateOnStart
builder.Services.AddAppSecurityHeaders(builder.Configuration);

// CORS — configurable via "Cors" in appsettings; fails at startup if AllowedOrigins is empty when Enabled
builder.Services.AddAppCors(builder.Configuration);
var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseAppSwagger();
}

// Seed — runs only in Development or when "Seed:Enabled" is explicitly true in config.
// Never enable in Production unless intentional (e.g. first-deploy bootstrap).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:Enabled"))
    await app.Services.SeedIdentityDataAsync();

app.UseExceptionHandler();
app.UseSecurityHeaders();
app.UseCorrelationId();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms | CorrelationId={CorrelationId} | TraceId={TraceId}";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? string.Empty);
        diagnosticContext.Set("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty);
    };
    // Suppress noise from health/swagger/info probes — log at Verbose so it's filtered by minimum level
    options.GetLevel = (ctx, _, ex) =>
    {
        if (ex is not null) return LogEventLevel.Error;
        var path = ctx.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_info", StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    };
});
app.UseHttpsRedirection();

if (corsOptions.Enabled)
    app.UseCors("default");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("default");
app.MapAppHealthChecks();

app.Run();

// Required so WebApplicationFactory<Program> can reference this entry-point type
// from test projects in other assemblies.
public partial class Program { }
