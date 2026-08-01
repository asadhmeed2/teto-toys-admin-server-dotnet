using MySql.Data.MySqlClient;
using AdmineTetoToys.Domain.Entities;
using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Infrastructure.Data;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT user_id, email, password_hash, is_active FROM users WHERE email = @email";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new User
        {
            UserId = reader.GetString(reader.GetOrdinal("user_id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
        };
    }

    public async Task CreateUserAsync(
        string userId, string email, string passwordHash,
        string firstName, string lastName, bool isAdult,
        DateTime termsAcceptedAt, string termsVersion,
        bool marketingOptIn, DateTime createdAt)
    {
        const string sql = @"
            INSERT INTO users (user_id, email, password_hash, first_name, last_name,
                               is_adult, terms_accepted_at, terms_version, marketing_opt_in, created_at)
            VALUES (@userId, @email, @passwordHash, @firstName, @lastName,
                    @isAdult, @termsAcceptedAt, @termsVersion, @marketingOptIn, @createdAt)";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@isAdult", isAdult);
        cmd.Parameters.AddWithValue("@termsAcceptedAt", termsAcceptedAt);
        cmd.Parameters.AddWithValue("@termsVersion", termsVersion);
        cmd.Parameters.AddWithValue("@marketingOptIn", marketingOptIn);
        cmd.Parameters.AddWithValue("@createdAt", createdAt);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        const string sql = "UPDATE users SET last_login = @now WHERE user_id = @userId";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@userId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdatePasswordAsync(string userId, string newPasswordHash)
    {
        const string sql = "UPDATE users SET password_hash = @hash WHERE user_id = @userId";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@hash", newPasswordHash);
        cmd.Parameters.AddWithValue("@userId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(List<CustomerListItem> Items, int TotalCount)> GetUsersPaginatedAsync(
        int page, int pageSize, string? search, bool searchEmail = true)
    {
        var items = new List<CustomerListItem>();
        int totalCount;
        int offset = (page - 1) * pageSize;

        // Callers who cannot see emails must not be able to probe for them either.
        var whereClause = string.IsNullOrEmpty(search)
            ? string.Empty
            : searchEmail
                ? " WHERE (first_name LIKE @search OR last_name LIKE @search OR email LIKE @search)"
                : " WHERE (first_name LIKE @search OR last_name LIKE @search)";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using (var countCmd = new MySqlCommand($"SELECT COUNT(1) FROM users{whereClause}", conn))
        {
            if (!string.IsNullOrEmpty(search))
                countCmd.Parameters.AddWithValue("@search", $"%{search}%");
            totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        // password_hash is intentionally not selected.
        var itemsSql = $@"
            SELECT user_id, email, first_name, last_name, is_active, marketing_opt_in, created_at, last_login
            FROM users{whereClause}
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
                var optInOrdinal = reader.GetOrdinal("marketing_opt_in");
                var activeOrdinal = reader.GetOrdinal("is_active");

                items.Add(new CustomerListItem
                {
                    UserId = reader.GetString(reader.GetOrdinal("user_id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    // Both columns are nullable-with-default in the schema.
                    IsActive = !reader.IsDBNull(activeOrdinal) && reader.GetBoolean(activeOrdinal),
                    MarketingOptIn = !reader.IsDBNull(optInOrdinal) && reader.GetBoolean(optInOrdinal),
                    CreatedAt = reader.IsDBNull(createdOrdinal) ? null : reader.GetDateTime(createdOrdinal),
                    LastLogin = reader.IsDBNull(lastLoginOrdinal) ? null : reader.GetDateTime(lastLoginOrdinal),
                });
            }
        }

        return (items, totalCount);
    }
}
