using StackExchange.Redis;
using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Infrastructure.Caching;

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisCacheService(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task SetRefreshTokenAsync(string token, TimeSpan ttl)
    {
        var db = _multiplexer.GetDatabase();
        await db.StringSetAsync($"refresh:{token}", "1", ttl);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var db = _multiplexer.GetDatabase();
        return await db.KeyExistsAsync($"refresh:{token}");
    }

    public async Task InvalidateRefreshTokenAsync(string token)
    {
        var db = _multiplexer.GetDatabase();
        await db.KeyDeleteAsync($"refresh:{token}");
    }

    public async Task SetResetTokenAsync(string key, string userId, TimeSpan ttl)
    {
        var db = _multiplexer.GetDatabase();
        await db.StringSetAsync(key, userId, ttl);
    }

    public async Task<string?> GetResetTokenUserIdAsync(string key)
    {
        var db = _multiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task InvalidateResetTokenAsync(string key)
    {
        var db = _multiplexer.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
    public async Task SetAdminSessionAsync(string email, string role, TimeSpan ttl)
    {
        var db = _multiplexer.GetDatabase();
        // ponytail: store role directly, keyed by email
        await db.StringSetAsync($"admin_session:{email}", role, ttl);
    }

    public async Task<AdmineTetoToys.Domain.Entities.AdminSession?> GetAdminSessionAsync(string email)
    {
        var db = _multiplexer.GetDatabase();
        var role = await db.StringGetAsync($"admin_session:{email}");
        return role.HasValue ? new AdmineTetoToys.Domain.Entities.AdminSession(email, role.ToString()) : null;
    }

    public async Task InvalidateAdminSessionAsync(string email)
    {
        var db = _multiplexer.GetDatabase();
        await db.KeyDeleteAsync($"admin_session:{email}");
    }
}
