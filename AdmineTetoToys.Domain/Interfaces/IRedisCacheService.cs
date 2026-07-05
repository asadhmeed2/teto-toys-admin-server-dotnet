namespace AdmineTetoToys.Domain.Interfaces;

public interface IRedisCacheService
{
    Task SetRefreshTokenAsync(string token, TimeSpan ttl);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task InvalidateRefreshTokenAsync(string token);

    Task SetResetTokenAsync(string key, string userId, TimeSpan ttl);
    Task<string?> GetResetTokenUserIdAsync(string key);
    Task InvalidateResetTokenAsync(string key);

    Task SetAdminSessionAsync(string email, string role, TimeSpan ttl);
    Task<AdmineTetoToys.Domain.Entities.AdminSession?> GetAdminSessionAsync(string email);
    Task InvalidateAdminSessionAsync(string email);

    Task SetPermissionsAsync(string email, string permissionsJson, TimeSpan ttl);
    Task<string?> GetPermissionsAsync(string email);
    Task InvalidatePermissionsAsync(string email);
}
