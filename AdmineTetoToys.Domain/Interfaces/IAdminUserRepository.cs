using AdmineTetoToys.Domain.Entities;

namespace AdmineTetoToys.Domain.Interfaces;

public interface IAdminUserRepository
{
    static TimeSpan AccessTokenTtl { get; } // Access token TTL
    static TimeSpan RefreshTokenTtl { get; } // Refresh token TTL
    Task<AdminUser?> GetByEmailAsync(string email);
    Task<AdminUser?> GetByIdAsync(string adminId);
    Task UpdateLastLoginAsync(string adminId);
    Task CreateAsync(AdminUser user);

}
