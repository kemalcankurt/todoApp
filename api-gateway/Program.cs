using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using Ocelot.DependencyInjection;
using Ocelot.Middleware;

using api_gateway.Middleware;
using api_gateway.services;
using api_gateway.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<IJwtService, JwtService>();

// JWT Authentication Setup
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing!"));


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
builder.Services.AddOcelot();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<RoleToClientIdMiddleware>();


// Separate branch for `/healthz` that bypasses Ocelot
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

app.Run();
