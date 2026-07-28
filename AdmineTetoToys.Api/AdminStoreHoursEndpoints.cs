using System.Globalization;
using AdmineTetoToys.Application.DTOs;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;

public static class AdminStoreHoursEndpoints
{
    public static void MapAdminStoreHoursEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/store-hours");

        // GET /api/admin/store-hours — full week, used to populate the edit form
        group.MapGet("/", async (HttpContext context) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            var repo = context.RequestServices.GetRequiredService<IStoreHoursRepository>();
            var days = await repo.GetAllAsync();

            return Results.Ok(new
            {
                days = days.Select(d => new
                {
                    day_of_week = d.DayOfWeek,
                    open_time = FormatTime(d.OpenTime),
                    close_time = FormatTime(d.CloseTime),
                    is_closed = d.IsClosed,
                }),
            });
        });

        // PUT /api/admin/store-hours — replace the week in one shot
        group.MapPut("/", async (UpdateStoreHoursRequest request, HttpContext context) =>
        {
            var authCheck = await AdminSessionValidator.ValidateSessionAsync(context);
            if (!authCheck.Authorized) return authCheck.ErrorResult!;

            if (request?.Days == null || request.Days.Count == 0)
                return Results.Json(new { error = "invalid_request", error_description = "At least one day is required." }, statusCode: 400);

            var parsed = new List<StoreHours>();
            var seenDays = new HashSet<int>();

            foreach (var day in request.Days)
            {
                if (day.DayOfWeek < 0 || day.DayOfWeek > 6)
                    return Results.Json(new { error = "invalid_request", error_description = $"day_of_week must be 0-6, got {day.DayOfWeek}." }, statusCode: 400);

                if (!seenDays.Add(day.DayOfWeek))
                    return Results.Json(new { error = "invalid_request", error_description = $"Duplicate entry for day_of_week {day.DayOfWeek}." }, statusCode: 400);

                // A closed day still needs storable times — fall back to a placeholder
                // rather than rejecting whatever the form left in the disabled inputs.
                if (day.IsClosed)
                {
                    parsed.Add(new StoreHours
                    {
                        DayOfWeek = day.DayOfWeek,
                        OpenTime = TryParseTime(day.OpenTime, out var co) ? co : TimeSpan.Zero,
                        CloseTime = TryParseTime(day.CloseTime, out var cc) ? cc : TimeSpan.Zero,
                        IsClosed = true,
                    });
                    continue;
                }

                if (!TryParseTime(day.OpenTime, out var openTime))
                    return Results.Json(new { error = "invalid_request", error_description = $"Invalid open_time '{day.OpenTime}' for day {day.DayOfWeek}. Expected HH:mm." }, statusCode: 400);

                if (!TryParseTime(day.CloseTime, out var closeTime))
                    return Results.Json(new { error = "invalid_request", error_description = $"Invalid close_time '{day.CloseTime}' for day {day.DayOfWeek}. Expected HH:mm." }, statusCode: 400);

                if (closeTime <= openTime)
                    return Results.Json(new { error = "invalid_request", error_description = $"close_time must be after open_time for day {day.DayOfWeek}." }, statusCode: 400);

                parsed.Add(new StoreHours
                {
                    DayOfWeek = day.DayOfWeek,
                    OpenTime = openTime,
                    CloseTime = closeTime,
                    IsClosed = false,
                });
            }

            var repo = context.RequestServices.GetRequiredService<IStoreHoursRepository>();
            await repo.UpdateAsync(parsed);

            // Drop the shared cache so the storefront reflects the change immediately
            // instead of serving stale hours for up to an hour.
            var redisService = context.RequestServices.GetRequiredService<IRedisCacheService>();
            await redisService.InvalidateStoreHoursAsync();

            var updated = await repo.GetAllAsync();
            return Results.Ok(new
            {
                days = updated.Select(d => new
                {
                    day_of_week = d.DayOfWeek,
                    open_time = FormatTime(d.OpenTime),
                    close_time = FormatTime(d.CloseTime),
                    is_closed = d.IsClosed,
                }),
            });
        });
    }

    private static string FormatTime(TimeSpan value) =>
        $"{(int)value.TotalHours:D2}:{value.Minutes:D2}";

    private static bool TryParseTime(string? raw, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Accepts "HH:mm" (what <input type="time"> sends) and "HH:mm:ss".
        return TimeSpan.TryParseExact(raw.Trim(), new[] { @"hh\:mm", @"hh\:mm\:ss" },
            CultureInfo.InvariantCulture, out value);
    }
}
