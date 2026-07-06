namespace AdmineTetoToys.Domain.Interfaces;

public interface IRedisCacheService
{
    Task SetRefreshTokenAsync(string token, TimeSpan ttl);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task InvalidateRefreshTokenAsync(string token);

    Task SetResetTokenAsync(string key, string userId, TimeSpan ttl);
    Task<string?> GetResetTokenUserIdAsync(string key);
    Task InvalidateResetTokenAsync(string key);

    Task SetAdminSessionAsync(string adminId, string role, TimeSpan ttl);
    Task<AdmineTetoToys.Domain.Entities.AdminSession?> GetAdminSessionAsync(string adminId);
    Task InvalidateAdminSessionAsync(string adminId);

    Task SetPermissionsAsync(string adminId, string permissionsJson, TimeSpan ttl);
    Task<string?> GetPermissionsAsync(string adminId);
    Task InvalidatePermissionsAsync(string adminId);
}
