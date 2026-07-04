namespace AdmineTetoToys.Domain.Interfaces;

public interface IRedisCacheService
{
    Task SetRefreshTokenAsync(string token, TimeSpan ttl);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task InvalidateRefreshTokenAsync(string token);

    Task SetResetTokenAsync(string key, string userId, TimeSpan ttl);
    Task<string?> GetResetTokenUserIdAsync(string key);
    Task InvalidateResetTokenAsync(string key);
}
