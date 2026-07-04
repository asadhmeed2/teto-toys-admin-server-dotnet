namespace AdmineTetoToys.Domain.Entities;

public class AdminUser
{
    public string AdminId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Partner"; // Admin | Partner
    public bool IsActive { get; set; }
}
