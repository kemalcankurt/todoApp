using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using user_service.Config;
using user_service.Data;
using user_service.Repositories;
using user_service.Services;
using user_service.Middlewares;
using user_service.Authorization;
using user_service.Authorization.Requirements;
using user_service.Authorization.Handlers;
using user_service.Mappings;

using Serilog;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

// 1. Configuration
ConfigureConfiguration(builder);

// 2. Logging & Telemetry
ConfigureLogging(builder);
ConfigureTelemetry(builder);

// 3. Core Services
ConfigureServices(builder);

// 4. Security
ConfigureSecurity(builder);

var app = builder.Build();

// 5. Database
await ConfigureDatabase(app);

// 6. Middleware Pipeline
ConfigureMiddleware(app);

await app.RunAsync();

// Helper Methods
void ConfigureConfiguration(WebApplicationBuilder builder)
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    if (environment == "Development")
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
    if (string.IsNullOrEmpty(jwtConfig?.Secret))
    {
        throw new InvalidOperationException("JWT configuration is invalid or missing");
    }
}

void ConfigureLogging(WebApplicationBuilder builder)
{
    var loggerConfig = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

    if (environment == "Production")
    {
        loggerConfig.MinimumLevel.Warning();
    }

    Log.Logger = loggerConfig.CreateLogger();
    builder.Host.UseSerilog();
}

void ConfigureTelemetry(WebApplicationBuilder builder)
{
    const string SERVICE_NAME = "user-service";
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(SERVICE_NAME)
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.name"] = SERVICE_NAME,
            ["service.namespace"] = "todoapp",
            ["service.version"] = "1.0.0",
            ["service.instance.id"] = Guid.NewGuid().ToString(),
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName,
            ["container.name"] = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown"
        })
        .AddTelemetrySdk()
        .AddEnvironmentVariableDetector();

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProvider =>
        {
            tracerProvider
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.SetDbStatementForText = true;
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317"));
        })
        .WithMetrics(metricsProvider =>
        {
            metricsProvider
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddSqlClientInstrumentation()
                .AddMeter("Microsoft.EntityFrameworkCore")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://otel-collector:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        });
}

void ConfigureServices(WebApplicationBuilder builder)
{
    // API Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<UserDbContext>("Database")
        .AddCheck("Self", () => HealthCheckResult.Healthy());

    // Database
    if (environment != "Testing")
    {
        builder.Services.AddDbContext<UserDbContext>(options =>
        {
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(3)
            );
        });
    }

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // HTTP Context
    builder.Services.AddHttpContextAccessor();

    // Application Services
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordService, PasswordService>();
    builder.Services.AddSingleton<IJwtService, JwtService>();

    // Configuration Options
    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection("Jwt"));
}

void ConfigureSecurity(WebApplicationBuilder builder)
{
    // JWT Configuration
    var jwtOptions = builder.Configuration
        .GetSection("Jwt")
        .Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ClockSkew = TimeSpan.Zero
            };
        });

    // Authorization
    builder.Services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
    builder.Services.AddSingleton<IAuthorizationHandler, AdminAuthorizationHandler>();

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.CanViewUser, policy =>
            policy.Requirements.Add(new UserAuthorizationRequirement()))
        .AddPolicy(Policies.AdminOnly, policy =>
            policy.Requirements.Add(new AdminAuthorizationRequirement()));
}

async Task ConfigureDatabase(WebApplication app)
{
    if (environment != "Testing")
    {
        using var scope = app.Services.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}

void ConfigureMiddleware(WebApplication app)
{
    // Exception Handling
    app.UseMiddleware<ExceptionMiddleware>();

    // Health Check Endpoint
    app.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Status = report.Status.ToString(),
                HealthChecks = report.Entries.Select(e => new
                {
                    Component = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description
                })
            });
        }
    }).AllowAnonymous();

    // Core Middleware
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}

// Required for integration testing
public partial class Program { }