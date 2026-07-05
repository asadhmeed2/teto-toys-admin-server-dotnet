using MySql.Data.MySqlClient;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Infrastructure.Data;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly string _connectionString;

    public AdminUserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<AdminUser?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT admin_id, email, password_hash, first_name, last_name, role, is_active FROM admin_users WHERE email = @email";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new AdminUser
        {
            AdminId = reader.GetGuid(reader.GetOrdinal("admin_id")).ToString(),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Role = reader.GetString(reader.GetOrdinal("role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
        };
    }

    public async Task UpdateLastLoginAsync(string adminId)
    {
        const string sql = "UPDATE admin_users SET last_login = @now WHERE admin_id = @adminId";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@adminId", adminId);
        await cmd.ExecuteNonQueryAsync();
    }
}
