using System.Text.Json.Serialization;

namespace AdmineTetoToys.Application.DTOs;

/// <summary>
/// One weekday's hours. day_of_week: 0 = Sunday .. 6 = Saturday.
/// Times are "HH:mm" strings; ignored when is_closed is true.
/// </summary>
public record StoreHoursDayDto(
    [property: JsonPropertyName("day_of_week")] int DayOfWeek,
    [property: JsonPropertyName("open_time")] string OpenTime,
    [property: JsonPropertyName("close_time")] string CloseTime,
    [property: JsonPropertyName("is_closed")] bool IsClosed
);

public record UpdateStoreHoursRequest(
    [property: JsonPropertyName("days")] List<StoreHoursDayDto> Days
);
