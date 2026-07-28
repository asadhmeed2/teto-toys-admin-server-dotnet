using AdmineTetoToys.Domain.Entities;

namespace AdmineTetoToys.Domain.Interfaces;

public interface IStoreHoursRepository
{
    /// <summary>Returns all seven weekdays ordered 0 (Sunday) .. 6 (Saturday).</summary>
    Task<List<StoreHours>> GetAllAsync();

    /// <summary>Upserts the supplied days in a single transaction.</summary>
    Task UpdateAsync(IEnumerable<StoreHours> hours);
}
