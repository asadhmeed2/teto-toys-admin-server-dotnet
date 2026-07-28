using MySql.Data.MySqlClient;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Infrastructure.Data;

public class StoreHoursRepository : IStoreHoursRepository
{
    private readonly string _connectionString;

    public StoreHoursRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<StoreHours>> GetAllAsync()
    {
        const string sql = @"
            SELECT day_of_week, open_time, close_time, is_closed
            FROM store_hours
            ORDER BY day_of_week ASC";

        var result = new List<StoreHours>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new StoreHours
            {
                DayOfWeek = reader.GetInt32(reader.GetOrdinal("day_of_week")),
                OpenTime = reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("open_time")),
                CloseTime = reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("close_time")),
                IsClosed = reader.GetBoolean(reader.GetOrdinal("is_closed")),
            });
        }

        return result;
    }

    public async Task UpdateAsync(IEnumerable<StoreHours> hours)
    {
        // Upsert rather than UPDATE so a missing weekday row self-heals.
        const string sql = @"
            INSERT INTO store_hours (day_of_week, open_time, close_time, is_closed)
            VALUES (@day, @open, @close, @isClosed)
            ON DUPLICATE KEY UPDATE
                open_time  = VALUES(open_time),
                close_time = VALUES(close_time),
                is_closed  = VALUES(is_closed)";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            foreach (var day in hours)
            {
                await using var cmd = new MySqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@day", day.DayOfWeek);
                cmd.Parameters.AddWithValue("@open", day.OpenTime);
                cmd.Parameters.AddWithValue("@close", day.CloseTime);
                cmd.Parameters.AddWithValue("@isClosed", day.IsClosed);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
