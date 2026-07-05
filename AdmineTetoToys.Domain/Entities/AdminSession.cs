namespace AdmineTetoToys.Domain.Entities;

// ponytail: minimal record for Redis session data
public record AdminSession(string Email, string Role);
