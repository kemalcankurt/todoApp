using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using api_gateway.Middleware;
using api_gateway.Services;
using api_gateway.Config;

using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot.Configuration;

using Serilog;

using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

// 1. Configuration
ConfigureConfiguration(builder);

// 2. Logging & Telemetry
ConfigureLogging(builder);
ConfigureTelemetry(builder);

// 3. Services
ConfigureServices(builder);

// 4. Authentication & Authorization
ConfigureAuth(builder);

var app = builder.Build();

// 5. Middleware Pipeline
ConfigureMiddleware(app, environment);

app.Run();

// Helper Methods
void ConfigureConfiguration(WebApplicationBuilder builder)
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    if (environment == "Development")
    {
        builder.Configuration.AddUserSecrets<Program>();
    }
}

void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog((ctx, lc) => lc
        .MinimumLevel.Debug()
        .WriteTo.Console(
            Serilog.Events.LogEventLevel.Debug,
            outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}"
        ));

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        });
    });
}

void ConfigureTelemetry(WebApplicationBuilder builder)
{
    const string SERVICE_NAME = "api-gateway";
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
        });

    var meter = new Meter("Ocelot.Gateway");
    var routeCounter = meter.CreateCounter<long>(
        "ocelot_route_requests_total",
        description: "Number of requests per route",
        unit: "{requests}");

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317")))
        .WithMetrics(metricsProviderBuilder => metricsProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter(meter.Name)
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317")));

    builder.Services.AddSingleton(routeCounter);
}

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.AddSingleton<IJwtService, JwtService>();

    builder.Services.AddOcelot();

    builder.Services.AddSwaggerForOcelot(builder.Configuration);

    builder.Services.AddHealthChecks();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", builder =>
        {
            builder
                .SetIsOriginAllowed(_ => true) // For development - make more restrictive in production
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    if (environment != "Production")
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter 'Bearer' [space] and your valid token."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}

void ConfigureAuth(WebApplicationBuilder builder)
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret is missing!"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
}

void ConfigureMiddleware(WebApplication app, string environment)
{
    // Move CORS before other middleware
    app.UseCors("AllowFrontend");

    // Exception Handling
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = context.Features.Get<IExceptionHandlerFeature>();
            if (error != null)
            {
                var ex = error.Error;
                Log.Error(ex, "An unhandled exception occurred");
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 500,
                    Message = "An error occurred while processing your request."
                });
            }
        });
    });

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    if (environment != "Production")
    {
        // MMLib.SwaggerForOcelot
        var swaggerConfig = builder.Configuration.GetSection("SwaggerEndPoints").Get<List<SwaggerEndPointOptions>>();

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };

        Console.WriteLine($"swaggerConfig: {JsonSerializer.Serialize(swaggerConfig, options)}");

        if (swaggerConfig == null)
        {
            throw new InvalidOperationException(" SwaggerEndPoints section is missing or empty. Check ocelot.json.");
        }
        
        app.UseSwaggerForOcelotUI(opt =>
        {
            opt.PathToSwaggerGenerator = "/swagger/docs";
        });
    }

    app.UseMiddleware<RoleToClientIdMiddleware>();

    // Correlation ID Middleware
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        Log.Information("Handling request with Correlation ID: {CorrelationId}", correlationId);
        await next();
    });

    // Health Check Endpoint
    app.MapWhen(context => context.Request.Path.StartsWithSegments("/healthz"), appBuilder =>
    {
        appBuilder.Run(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Healthy!");
        });
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseOcelot().Wait();

    // Get the counters
    var routeCounter = app.Services.GetRequiredService<Counter<long>>();

    // Add metrics middleware AFTER Ocelot
    app.Use(async (context, next) =>
    {
        var route = context.Request.Path.Value ?? "/";

        // Record all requests
        routeCounter.Add(1, new KeyValuePair<string, object?>("route", route));

        await next();
    });
}