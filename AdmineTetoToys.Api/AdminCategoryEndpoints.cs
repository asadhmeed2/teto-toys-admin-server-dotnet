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



        // POST /api/admin/categories
        categoriesGroup.MapPost("/", async (CreateCategoryRequest request, HttpContext context) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
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

            string categoryLanguage = string.IsNullOrEmpty(request.Language) ? "en" : request.Language;
            await productRepo.CreateCategoryAsync(category, categoryLanguage);

            return Results.Json(new
            {
                // CreateCategoryAsync backfills the generated id — return it so callers
                // can immediately edit or link to the new category.
                id = category.Id,
                name = category.Name,
                slug = category.Slug,
                language = categoryLanguage
            }, statusCode: 201);
        });

        // GET /api/admin/categories/{categoryId} — single category, name resolved for
        // ?language= (falls back to 'en'). Used to populate the edit form and to reload
        // it when the admin switches language, mirroring the product edit flow.
        categoriesGroup.MapGet("/{categoryId:int}", async (int categoryId, HttpContext context, string? language) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            string languageVal = string.IsNullOrEmpty(language) ? "en" : language;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            var category = await productRepo.GetCategoryByIdAsync(categoryId, languageVal);

            if (category == null)
                return Results.NotFound(new { error = "not_found", error_description = $"Category {categoryId} not found." });

            return Results.Ok(new
            {
                id = category.Id,
                name = category.Name,
                slug = category.Slug,
                number_of_active_products = category.NumberOfActiveProducts,
                language = languageVal
            });
        });

        // PUT /api/admin/categories/{categoryId} — edit the name for one language.
        categoriesGroup.MapPut("/{categoryId:int}", async (int categoryId, UpdateCategoryRequest request, HttpContext context) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return Results.Json(new { error = "invalid_request", error_description = "Category name is required." }, statusCode: 400);

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();

            if (!await productRepo.CategoryExistsAsync(categoryId))
                return Results.NotFound(new { error = "not_found", error_description = $"Category {categoryId} not found." });

            string categoryLanguage = string.IsNullOrEmpty(request.Language) ? "en" : request.Language;

            // Only the translation row changes; the slug stays put so existing links
            // and the 'general' guard keep working after a rename.
            await productRepo.UpdateCategoryAsync(categoryId, request.Name.Trim(), categoryLanguage);

            var updated = await productRepo.GetCategoryByIdAsync(categoryId, categoryLanguage);

            return Results.Ok(new
            {
                id = categoryId,
                name = updated?.Name ?? request.Name.Trim(),
                slug = updated?.Slug,
                number_of_active_products = updated?.NumberOfActiveProducts ?? 0,
                language = categoryLanguage
            });
        });

        // DELETE /api/admin/categories/{categoryId}
        // Moves subcategories and products to the General category, then deletes the category.
        categoriesGroup.MapDelete("/{categoryId}", async (int categoryId, HttpContext context) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context, "Admin");
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();

            if (!await productRepo.CategoryExistsAsync(categoryId))
                return Results.NotFound(new { error = "not_found", error_description = $"Category {categoryId} not found." });

            try
            {
                await productRepo.DeleteCategoryAsync(categoryId);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new { error = "invalid_request", error_description = ex.Message }, statusCode: 400);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = "server_error", error_description = "Failed to delete category. All changes were rolled back.", detail = ex.Message }, statusCode: 500);
            }
        });

        // GET /api/admin/categories
        categoriesGroup.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search, string? language) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;
            string languageVal = string.IsNullOrEmpty(language) ? "en" : language;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            var (items, totalCount) = await productRepo.GetCategoriesPaginatedAsync(pageVal, pageSizeVal, search, languageVal);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSizeVal);

            return Results.Ok(new
            {
                items = items.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    slug = c.Slug,
                    number_of_active_products = c.NumberOfActiveProducts
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
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            if (string.IsNullOrWhiteSpace(request.Name) || request.CategoryId <= 0)
                return Results.Json(new { error = "invalid_request", error_description = "Subcategory Name and a valid Parent Category ID are required." }, statusCode: 400);

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            string subcategoryLanguage = string.IsNullOrEmpty(request.Language) ? "en" : request.Language;

            var categoryExistsTask = productRepo.CategoryExistsAsync(request.CategoryId);
            var subcategoryExistsTask = productRepo.SubcategoryExistsAsync(request.CategoryId, request.Name.Trim(), subcategoryLanguage);

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

            await productRepo.CreateSubcategoryAsync(subcategory, subcategoryLanguage);

            return Results.Json(new
            {
                category_id = subcategory.CategoryId,
                name = subcategory.Name
            }, statusCode: 201);
        });

        // GET /api/admin/subcategories
        subcategoriesGroup.MapGet("/", async (HttpContext context, int? page, int? pageSize, string? search, string? language) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            int pageVal = page ?? 1;
            int pageSizeVal = pageSize ?? 20;
            if (pageVal < 1) pageVal = 1;
            if (pageSizeVal < 1 || pageSizeVal > 100) pageSizeVal = 20;
            string languageVal = string.IsNullOrEmpty(language) ? "en" : language;

            var productRepo = context.RequestServices.GetRequiredService<IProductRepository>();
            var (items, totalCount) = await productRepo.GetSubcategoriesPaginatedAsync(pageVal, pageSizeVal, search, languageVal);

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
