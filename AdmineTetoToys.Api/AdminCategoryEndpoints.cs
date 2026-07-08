using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdmineTetoToys.Application.DTOs;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

public static class AdminCategoryEndpoints
{
    public static void MapAdminCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var categoriesGroup = app.MapGroup("/api/admin/categories");
        var subcategoriesGroup = app.MapGroup("/api/admin/subcategories");

        // Helper to validate Admin session in Redis
        async Task<(bool Authorized, object? UserInfo, IResult? ErrorResult)> ValidateAdminSessionAsync(HttpContext context)
        {
            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
            var redisService = context.RequestServices.GetRequiredService<IRedisCacheService>();
            var config = context.RequestServices.GetRequiredService<IConfiguration>();

            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return (false, null, Results.Json(new { error = "unauthorized", error_description = "Missing or invalid Authorization header." }, statusCode: 401));

            var secret = config["JWT:SECRET"] ?? "SuperSecretKeyForTetoToysTokenAuth2026";
            var userInfo = tokenService.ValidateAndGetUserInfo(authHeader[7..], secret);
            if (userInfo == null)
                return (false, null, Results.Json(new { error = "unauthorized", error_description = "Token is invalid or expired." }, statusCode: 401));

            var adminIdProp = userInfo.GetType().GetProperty("adminId");
            var callerAdminId = adminIdProp?.GetValue(userInfo)?.ToString();
            if (string.IsNullOrEmpty(callerAdminId))
                return (false, null, Results.Json(new { error = "unauthorized", error_description = "Could not identify caller." }, statusCode: 401));

            var session = await redisService.GetAdminSessionAsync(callerAdminId);
            if (session == null)
                return (false, null, Results.Json(new { error = "unauthorized", error_description = "Session expired. Please log in again." }, statusCode: 401));

            if (!string.Equals(session.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(session.Role, "Partner", StringComparison.OrdinalIgnoreCase))
                return (false, null, Results.Json(new { error = "forbidden", error_description = "Only Admin or Partner users can perform this action." }, statusCode: 403));

            return (true, userInfo, null);
        }

        // POST /api/admin/categories
        categoriesGroup.MapPost("/", async (CreateCategoryRequest request, HttpContext context) =>
        {
            var authCheck = await ValidateAdminSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.Json(new { error = "invalid_request", error_description = "Category name is required." }, statusCode: 400);

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();

            // ponytail: category slug is name converted to lowercase with hyphens
            var slug = request.Name.Trim().ToLowerInvariant().Replace(" ", "-");
            
            // ponytail: avoid duplicates
            if (await productRepo.CategoryExistsBySlugAsync(slug))
                return Results.Json(new { error = "invalid_request", error_description = $"Category with slug '{slug}' already exists." }, statusCode: 400);

            var category = new Category
            {
                Name = request.Name.Trim(),
                Slug = slug
            };

            await productRepo.CreateCategoryAsync(category);

            return Results.Json(new
            {
                name = category.Name,
                slug = category.Slug
            }, statusCode: 201);
        });

        // GET /api/admin/categories
        categoriesGroup.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search) =>
        {
            var authCheck = await ValidateAdminSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            var (items, totalCount) = await productRepo.GetCategoriesPaginatedAsync(pageVal, pageSizeVal, search);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeVal);

            return Results.Ok(new
            {
                items = items.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    slug = c.Slug
                }),
                total_count = totalCount,
                page = pageVal,
                page_size = pageSizeVal,
                total_pages = totalPages
            });
        });

        // POST /api/admin/subcategories
        subcategoriesGroup.MapPost("/", async (CreateSubcategoryRequest request, HttpContext context) =>
        {
            var authCheck = await ValidateAdminSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            if (string.IsNullOrWhiteSpace(request.Name) || request.CategoryId <= 0)
                return Results.Json(new { error = "invalid_request", error_description = "Subcategory Name and a valid Parent Category ID are required." }, statusCode: 400);

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();

            var categoryExistsTask = productRepo.CategoryExistsAsync(request.CategoryId);
            var subcategoryExistsTask = productRepo.SubcategoryExistsAsync(request.CategoryId, request.Name.Trim());

            await Task.WhenAll(categoryExistsTask, subcategoryExistsTask);

            if (!await categoryExistsTask)
                return Results.Json(new { error = "invalid_request", error_description = $"Parent Category ID '{request.CategoryId}' does not exist." }, statusCode: 400);

            if (await subcategoryExistsTask)
                return Results.Json(new { error = "invalid_request", error_description = $"Subcategory with name '{request.Name}' already exists under Category ID '{request.CategoryId}'." }, statusCode: 400);

            var subcategory = new Subcategory
            {
                CategoryId = request.CategoryId,
                Name = request.Name.Trim()
            };

            await productRepo.CreateSubcategoryAsync(subcategory);

            return Results.Json(new
            {
                category_id = subcategory.CategoryId,
                name = subcategory.Name
            }, statusCode: 201);
        });

        // GET /api/admin/subcategories
        subcategoriesGroup.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search) =>
        {
            var authCheck = await ValidateAdminSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            var (items, totalCount) = await productRepo.GetSubcategoriesPaginatedAsync(pageVal, pageSizeVal, search);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeVal);

            return Results.Ok(new
            {
                items = items.Select(s => new
                {
                    id = s.Id,
                    category_id = s.CategoryId,
                    name = s.Name
                }),
                total_count = totalCount,
                page = pageVal,
                page_size = pageSizeVal,
                total_pages = totalPages
            });
        });
    }
}
