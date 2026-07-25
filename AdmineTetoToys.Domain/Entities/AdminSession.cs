namespace AdmineTetoToys.Domain.Entities;

// ponytail: minimal record for Redis session data
public record AdminSession(string Id, string Role);


public record AdminRefreshTokenData(string Id, string RefreshToken);
