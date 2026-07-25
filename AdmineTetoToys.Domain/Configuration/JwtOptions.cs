namespace AdmineTetoToys.Domain.Configuration;

/// <summary>
/// Central token lifetime settings. Bound from the "Jwt" section of configuration
/// (appsettings.json / environment). TimeSpan values bind from strings such as
/// "00:15:00" (15 min) and "7.00:00:00" (7 days). Defaults apply when unset.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Lifetime of an access token / admin session in Redis.</summary>
    public TimeSpan AccessTokenTtl { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Lifetime of a refresh token / refresh cookie.</summary>
    public TimeSpan RefreshTokenTtl { get; set; } = TimeSpan.FromDays(7);

    // Convenience accessors for APIs that take minutes/seconds.
    public int AccessTokenMinutes => (int)AccessTokenTtl.TotalMinutes;
    public int RefreshTokenMinutes => (int)RefreshTokenTtl.TotalMinutes;

    /// <summary>Value for the OAuth-style "expires_in" response field (seconds).</summary>
    public int AccessTokenSeconds => (int)AccessTokenTtl.TotalSeconds;
}
