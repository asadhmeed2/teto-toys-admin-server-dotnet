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

    public async Task<AdminUser?> GetByIdAsync(string adminId)
    {
        const string sql = "SELECT admin_id, email, password_hash, first_name, last_name, role, is_active FROM admin_users WHERE admin_id = @adminId";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.Add("@adminId", MySqlDbType.VarChar).Value = adminId;

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
    public async Task CreateAsync(AdminUser user)
    {
        const string sql = @"INSERT INTO admin_users (admin_id, email, password_hash, first_name, last_name, role, is_active)
                             VALUES (@adminId, @email, @passwordHash, @firstName, @lastName, @role, @isActive)";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@adminId", user.AdminId);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@firstName", user.FirstName);
        cmd.Parameters.AddWithValue("@lastName", user.LastName);
        cmd.Parameters.AddWithValue("@role", user.Role);
        cmd.Parameters.AddWithValue("@isActive", user.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(List<AdminUserListItem> Items, int TotalCount)> GetAdminUsersPaginatedAsync(
        int page, int pageSize, string? search)
    {
        var items = new List<AdminUserListItem>();
        int totalCount;
        int offset = (page - 1) * pageSize;

        // Search across name and email; role is a small enum so it is not searched.
        var whereClause = string.IsNullOrEmpty(search)
            ? string.Empty
            : " WHERE (first_name LIKE @search OR last_name LIKE @search OR email LIKE @search)";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using (var countCmd = new MySqlCommand($"SELECT COUNT(1) FROM admin_users{whereClause}", conn))
        {
            if (!string.IsNullOrEmpty(search))
                countCmd.Parameters.AddWithValue("@search", $"%{search}%");
            totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        // password_hash is intentionally not selected.
        var itemsSql = $@"
            SELECT admin_id, email, first_name, last_name, role, is_active, created_at, last_login
            FROM admin_users{whereClause}
            ORDER BY created_at DESC
            LIMIT @limit OFFSET @offset";

        await using (var itemsCmd = new MySqlCommand(itemsSql, conn))
        {
            if (!string.IsNullOrEmpty(search))
                itemsCmd.Parameters.AddWithValue("@search", $"%{search}%");
            itemsCmd.Parameters.AddWithValue("@limit", pageSize);
            itemsCmd.Parameters.AddWithValue("@offset", offset);

            await using var reader = await itemsCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var createdOrdinal = reader.GetOrdinal("created_at");
                var lastLoginOrdinal = reader.GetOrdinal("last_login");

                items.Add(new AdminUserListItem
                {
                    // CHAR(36) comes back as Guid from this provider, not string.
                    AdminId = reader.GetIdString("admin_id"),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Role = reader.GetString(reader.GetOrdinal("role")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.IsDBNull(createdOrdinal) ? null : reader.GetDateTime(createdOrdinal),
                    LastLogin = reader.IsDBNull(lastLoginOrdinal) ? null : reader.GetDateTime(lastLoginOrdinal),
                });
            }
        }

        return (items, totalCount);
    }
}
