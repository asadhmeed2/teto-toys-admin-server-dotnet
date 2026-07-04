using AdmineTetoToys.Domain.Interfaces;

namespace AdmineTetoToys.Application.Services;

public interface IPasswordResetService
{
    Task<(bool Success, string? Error, string? ErrorDescription, int StatusCode)> ForgotPasswordAsync(string email);
    Task<(bool Success, string? Error, string? ErrorDescription, int StatusCode)> ResetPasswordAsync(string token, string newPassword, string confirmPassword);
}
