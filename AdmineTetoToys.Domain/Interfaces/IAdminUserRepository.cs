using AdmineTetoToys.Domain.Entities;

namespace AdmineTetoToys.Domain.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByEmailAsync(string email);
    Task<AdminUser?> GetByIdAsync(string adminId);
    Task UpdateLastLoginAsync(string adminId);
    Task CreateAsync(AdminUser user);

    /// <summary>Paginated admin users for the admin-only list page. Never returns password hashes.</summary>
    Task<(List<AdminUserListItem> Items, int TotalCount)> GetAdminUsersPaginatedAsync(
        int page, int pageSize, string? search);

}
