using AdmineTetoToys.Domain.Entities;

namespace AdmineTetoToys.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task CreateUserAsync(
        string userId, string email, string passwordHash,
        string firstName, string lastName, bool isAdult,
        DateTime termsAcceptedAt, string termsVersion,
        bool marketingOptIn, DateTime createdAt);
    Task UpdateLastLoginAsync(string userId);
    Task UpdatePasswordAsync(string userId, string newPasswordHash);

    /// <summary>
    /// Paginated storefront users for the admin list page. Never returns password hashes.
    /// <paramref name="searchEmail"/> is false for Partners: they cannot see emails, so
    /// letting them search by one would leak whether an address is registered.
    /// </summary>
    Task<(List<CustomerListItem> Items, int TotalCount)> GetUsersPaginatedAsync(
        int page, int pageSize, string? search, bool searchEmail = true);
}
