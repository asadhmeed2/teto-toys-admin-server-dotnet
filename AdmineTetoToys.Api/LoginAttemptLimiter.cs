using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Per-account failed-login throttle, counted in Redis.
///
/// The IP rate limiter caps how fast any one host can call the API; this caps how
/// many times a *specific account* may be guessed, no matter how many hosts try.
/// The two are complementary: distributed credential stuffing spreads across IPs
/// but still converges on one email.
///
/// The counter is keyed by a hash of the email, not the email itself, so Redis
/// never holds a plaintext list of who has been attacked.
/// </summary>
public static class LoginAttemptLimiter
{
    private const int DefaultMaxAttempts = 5;
    private const int DefaultLockoutSeconds = 900; // 15 minutes

    private static string KeyFor(string service, string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();

        // Truncated: 96 bits is ample to avoid collisions and keeps the key short.
        return $"login_fail:{service}:{hash[..24]}";
    }

    private static int MaxAttempts(IConfiguration config) =>
        config.GetValue<int?>("LoginLockout:MaxAttempts") ?? DefaultMaxAttempts;

    private static int LockoutSeconds(IConfiguration config) =>
        config.GetValue<int?>("LoginLockout:LockoutSeconds") ?? DefaultLockoutSeconds;

    /// <summary>
    /// Seconds remaining on the lockout, or 0 when the account may attempt a login.
    /// Fails open — a Redis outage must not lock everyone out.
    /// </summary>
    public static async Task<int> GetLockoutRemainingAsync(
        IConnectionMultiplexer? redis, IConfiguration config, string service, string email)
    {
        if (redis == null || string.IsNullOrWhiteSpace(email)) return 0;

        try
        {
            var db = redis.GetDatabase();
            var key = KeyFor(service, email);
            var attempts = (int)await db.StringGetAsync(key);

            if (attempts < MaxAttempts(config)) return 0;

            var ttl = await db.KeyTimeToLiveAsync(key);
            return ttl.HasValue ? Math.Max(1, (int)ttl.Value.TotalSeconds) : LockoutSeconds(config);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Records a failed attempt. The TTL is set on the first failure and not extended
    /// afterwards, so the window is "N failures within LockoutSeconds" rather than a
    /// lockout that a persistent attacker can keep alive indefinitely.
    /// </summary>
    public static async Task RecordFailureAsync(
        IConnectionMultiplexer? redis, IConfiguration config, string service, string email)
    {
        if (redis == null || string.IsNullOrWhiteSpace(email)) return;

        try
        {
            var db = redis.GetDatabase();
            var key = KeyFor(service, email);
            var attempts = await db.StringIncrementAsync(key);

            if (attempts == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(LockoutSeconds(config)));
            }
        }
        catch
        {
            // Best effort — never block a login because the counter couldn't be written.
        }
    }

    /// <summary>Clears the counter after a successful login.</summary>
    public static async Task ResetAsync(
        IConnectionMultiplexer? redis, string service, string email)
    {
        if (redis == null || string.IsNullOrWhiteSpace(email)) return;

        try
        {
            await redis.GetDatabase().KeyDeleteAsync(KeyFor(service, email));
        }
        catch
        {
            // Best effort.
        }
    }
}
