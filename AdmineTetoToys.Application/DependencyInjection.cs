using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AdmineTetoToys.Application.Services;
using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();

        var frontendBaseUrl = configuration["FrontendBaseUrl"] ?? "http://localhost:4201";
        services.AddScoped<IPasswordResetService>(sp => new PasswordResetService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IRedisCacheService>(),
            sp.GetRequiredService<IEmailService>(),
            frontendBaseUrl));

        return services;
    }
}
