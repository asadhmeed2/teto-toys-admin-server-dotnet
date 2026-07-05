using AdmineTetoToys.Domain.Entities;

namespace AdmineTetoToys.Domain.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email);
    Task UpdateLastLoginAsync(string adminId);
    Task CreateAsync(AdminUser user);
}
