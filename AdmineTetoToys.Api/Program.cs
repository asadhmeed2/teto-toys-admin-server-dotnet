using DotNetEnv;
using AdmineTetoToys.Application;
using AdmineTetoToys.Infrastructure;
using AdmineTetoToys.Domain.Configuration;
using Microsoft.Extensions.Options;

// Load .env file before building the host.
Env.Load(options: new LoadOptions(setEnvVars: true, clobberExistingVars: false));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var allowedOrigin = builder.Configuration["CorsOrigin"] ?? "http://localhost:4201";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminUI", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Central token lifetimes (access/refresh TTLs) — used by admin auth and AuthService.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
// Expose the bound value directly so layers that don't reference Microsoft.Extensions.Options
// (e.g. Application/AuthService) can inject JwtOptions without the IOptions<> wrapper.
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAdminUI");

// Before auth and endpoints: rejected traffic should cost as little as possible.
// Must follow UseCors so 429 responses still carry CORS headers and the browser
// can read them instead of reporting an opaque network error.
app.UseRedisRateLimiting();
app.UseHttpsRedirection();

app.MapAdminAuthEndpoints();
app.MapAdminUserEndpoints();
app.MapAdminProductEndpoints();
app.MapAdminCategoryEndpoints();
app.MapAdminLanguageEndpoints();
app.MapAdminStoreHoursEndpoints();

app.Run();
