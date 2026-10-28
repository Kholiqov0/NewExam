using Application.ApiResult;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterDto dto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto);
    Task<Result<UserDto>> GetMeAsync(int currentUserId);
    Task<Result<string>> ChangePasswordAsync(int currentUserId, ChangePasswordDto dto);
    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<Result<string>> VerifyCodeAsync(VerifyCodeDto dto);
    Task<Result<string>> ResetPasswordAsync(ResetPasswordDto dto);
}
