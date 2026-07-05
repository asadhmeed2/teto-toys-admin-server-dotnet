using System.Text.Json.Serialization;

namespace AdmineTetoToys.Application.DTOs;

public record CreateAdminUserRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("role")] string Role // "Admin" or "Partner"
);
