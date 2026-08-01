namespace AdmineTetoToys.Domain.Entities;

/// <summary>
/// Read-only projection of admin_users for the admin users list.
/// Deliberately has no PasswordHash — this shape is what reaches the API surface.
/// </summary>
public class AdminUserListItem
{
    public string AdminId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Partner";
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

/// <summary>
/// Read-only projection of storefront users for the teto-toys users list.
/// Deliberately has no PasswordHash.
/// </summary>
public class CustomerListItem
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MarketingOptIn { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}
