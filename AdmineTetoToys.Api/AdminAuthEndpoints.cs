using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdmineTetoToys.Application.DTOs;
using AdmineTetoToys.Domain.Interfaces;

public static class AdminAuthEndpoints
{
    public static void MapAdminAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // POST /api/auth/login
        group.MapPost("/login", async (LoginRequest request, HttpContext context) =>
        {
            var adminRepo = context.RequestServices.GetRequiredService<IAdminUserRepository>();
            var hasher = context.RequestServices.GetRequiredService<IPasswordHasher>();
            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
            var redisService = context.RequestServices.GetRequiredService<IRedisCacheService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.Json(new { error = "invalid_request", error_description = "Email and password are required." }, statusCode: 400);

            var admin = await adminRepo.GetByEmailAsync(request.Email);
            if (admin == null || !admin.IsActive)
                return Results.Json(new { error = "invalid_grant", error_description = "Invalid email or password." }, statusCode: 401);

            if (!hasher.VerifyPassword(request.Password, admin.PasswordHash))
                return Results.Json(new { error = "invalid_grant", error_description = "Invalid email or password." }, statusCode: 401);

            await adminRepo.UpdateLastLoginAsync(admin.AdminId);

            var secret = config["JWT:SECRET"] ?? "SuperSecretKeyForTetoToysTokenAuth2026";
            string accessToken = tokenService.GenerateAccessToken(admin.Email, admin.Role, secret, 15);
            string refreshToken = tokenService.GenerateRefreshToken(admin.Email, admin.Role, secret, 7 * 24 * 60);
            await redisService.SetRefreshTokenAsync(refreshToken, TimeSpan.FromDays(7));

            // ponytail: persist admin session in Redis for auth checks on protected endpoints
            await redisService.SetAdminSessionAsync(admin.Email, admin.Role, TimeSpan.FromMinutes(15));

            context.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = false, // set true in production
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            return Results.Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 900,
                role = admin.Role,
            });
        });

        // POST /api/auth/logout
        group.MapPost("/logout", async (HttpContext context) =>
        {
            var redisService = context.RequestServices.GetRequiredService<IRedisCacheService>();
            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();

            var refreshToken = context.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await redisService.InvalidateRefreshTokenAsync(refreshToken);

                // ponytail: clear admin session from Redis
                var email = tokenService.GetEmailFromToken(refreshToken);
                if (!string.IsNullOrEmpty(email))
                    await redisService.InvalidateAdminSessionAsync(email);
            }
            context.Response.Cookies.Delete("refresh_token");
            return Results.Ok(new { message = "Logged out successfully." });
        });

        // GET /api/auth/me
        group.MapGet("/me", (HttpContext context) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "unauthorized", error_description = "Missing or invalid Authorization header." }, statusCode: 401);

            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var secret = config["JWT:SECRET"] ?? "SuperSecretKeyForTetoToysTokenAuth2026";

            var userInfo = tokenService.ValidateAndGetUserInfo(authHeader[7..], secret);
            if (userInfo == null)
                return Results.Json(new { error = "unauthorized", error_description = "Token is invalid or expired." }, statusCode: 401);

            return Results.Ok(userInfo);
        });
    }
}
