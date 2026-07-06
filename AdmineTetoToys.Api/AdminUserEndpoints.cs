using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdmineTetoToys.Application.DTOs;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;

public static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users");

        // POST /api/admin/users — create a new Admin or Partner user (Admin-only)
        group.MapPost("/", async (CreateAdminUserRequest request, HttpContext context) =>
        {
            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
            var redisService = context.RequestServices.GetRequiredService<IRedisCacheService>();
            var adminRepo = context.RequestServices.GetRequiredService<IAdminUserRepository>();
            var hasher = context.RequestServices.GetRequiredService<IPasswordHasher>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();

            // 1. Validate JWT from Authorization header
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "unauthorized", error_description = "Missing or invalid Authorization header." }, statusCode: 401);

            var secret = config["JWT:SECRET"] ?? "SuperSecretKeyForTetoToysTokenAuth2026";
            var userInfo = tokenService.ValidateAndGetUserInfo(authHeader[7..], secret);
            if (userInfo == null)
                return Results.Json(new { error = "unauthorized", error_description = "Token is invalid or expired." }, statusCode: 401);

            // ponytail: extract adminId from validated token via reflection on anonymous object
            var adminIdProp = userInfo.GetType().GetProperty("adminId");
            var callerAdminId = adminIdProp?.GetValue(userInfo)?.ToString();
            if (string.IsNullOrEmpty(callerAdminId))
                return Results.Json(new { error = "unauthorized", error_description = "Could not identify caller." }, statusCode: 401);

            // 2. Verify caller's session exists in Redis and role is Admin
            var session = await redisService.GetAdminSessionAsync(callerAdminId);
            if (session == null)
                return Results.Json(new { error = "unauthorized", error_description = "Session expired. Please log in again." }, statusCode: 401);

            if (!string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "forbidden", error_description = "Only Admin users can create new users." }, statusCode: 403);

            // 3. Validate request
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return Results.Json(new { error = "invalid_request", error_description = "All fields are required." }, statusCode: 400);

            if (request.Role != "Admin" && request.Role != "Partner")
                return Results.Json(new { error = "invalid_request", error_description = "Role must be 'Admin' or 'Partner'." }, statusCode: 400);

            // 4. Check if email already exists
            var existing = await adminRepo.GetByEmailAsync(request.Email);
            if (existing != null)
                return Results.Json(new { error = "conflict", error_description = "A user with this email already exists." }, statusCode: 409);

            // 5. Create the user
            var newUser = new AdminUser
            {
                AdminId = Guid.NewGuid().ToString(),
                Email = request.Email.Trim(),
                PasswordHash = hasher.HashPassword(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Role = request.Role,
                IsActive = true,
            };

            await adminRepo.CreateAsync(newUser);

            return Results.Json(new
            {
                admin_id = newUser.AdminId,
                email = newUser.Email,
                first_name = newUser.FirstName,
                last_name = newUser.LastName,
                role = newUser.Role,
            }, statusCode: 201);
        });
    }
}
