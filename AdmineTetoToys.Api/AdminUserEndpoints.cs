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
            var adminRepo = context.RequestServices.GetRequiredService<IAdminUserRepository>();
            var hasher = context.RequestServices.GetRequiredService<IPasswordHasher>();

            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context, "Admin");
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

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

        // GET /api/admin/users — admin users list. Admin-only: Partners must not be
        // able to enumerate other admin accounts.
        group.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context, "Admin");
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;

            var adminRepo = context.RequestServices.GetRequiredService<IAdminUserRepository>();
            var (items, totalCount) = await adminRepo.GetAdminUsersPaginatedAsync(pageVal, pageSizeVal, search);

            return Results.Ok(new
            {
                items = items.Select(u => new
                {
                    admin_id = u.AdminId,
                    email = u.Email,
                    first_name = u.FirstName,
                    last_name = u.LastName,
                    role = u.Role,
                    is_active = u.IsActive,
                    created_at = u.CreatedAt,
                    last_login = u.LastLogin,
                }),
                total_count = totalCount,
                page = pageVal,
                page_size = pageSizeVal,
                total_pages = (int)Math.Ceiling((double)totalCount / pageSizeVal),
            });
        });

        // GET /api/admin/customers — storefront (teto-toys) users list.
        // Admin and Partner may both view, but Partners get a reduced projection:
        // no email, last login or marketing opt-in. Enforced here, not in the UI.
        var customersGroup = app.MapGroup("/api/admin/customers");

        customersGroup.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;

            // Role drives which columns are serialized. Read it off the validated
            // session rather than trusting anything from the client.
            var roleProp = authCheck.UserInfo?.GetType().GetProperty("role");
            var callerRole = roleProp?.GetValue(authCheck.UserInfo)?.ToString() ?? "Partner";
            var isAdmin = string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase);

            var userRepo = context.RequestServices.GetRequiredService<IUserRepository>();
            var (items, totalCount) = await userRepo.GetUsersPaginatedAsync(
                pageVal, pageSizeVal, search, searchEmail: isAdmin);

            // Partners never receive the omitted fields — they are absent from the
            // payload entirely, not blanked out client-side.
            var payload = isAdmin
                ? items.Select(u => (object)new
                {
                    user_id = u.UserId,
                    first_name = u.FirstName,
                    last_name = u.LastName,
                    email = u.Email,
                    is_active = u.IsActive,
                    marketing_opt_in = u.MarketingOptIn,
                    created_at = u.CreatedAt,
                    last_login = u.LastLogin,
                })
                : items.Select(u => (object)new
                {
                    user_id = u.UserId,
                    first_name = u.FirstName,
                    last_name = u.LastName,
                    created_at = u.CreatedAt,
                });

            return Results.Ok(new
            {
                items = payload,
                // Lets the UI render the right columns without guessing.
                viewer_role = isAdmin ? "Admin" : "Partner",
                total_count = totalCount,
                page = pageVal,
                page_size = pageSizeVal,
                total_pages = (int)Math.Ceiling((double)totalCount / pageSizeVal),
            });
        });
    }
}
