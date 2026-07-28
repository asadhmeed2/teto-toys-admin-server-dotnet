namespace AdmineTetoToys.Domain.Entities;

/// <summary>
/// Opening hours for a single weekday. DayOfWeek: 0 = Sunday .. 6 = Saturday.
/// When IsClosed is true, OpenTime/CloseTime are ignored.
/// </summary>
public class StoreHours
{
    public int DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
