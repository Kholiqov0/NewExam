using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
        => ToResponse(await authService.RegisterAsync(dto));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
        => ToResponse(await authService.LoginAsync(dto));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        => ToResponse(await authService.ChangePasswordAsync(CurrentUserId, dto));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        => ToResponse(await authService.ForgotPasswordAsync(dto));

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode(VerifyCodeDto dto)
        => ToResponse(await authService.VerifyCodeAsync(dto));

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        => ToResponse(await authService.ResetPasswordAsync(dto));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
        => ToResponse(await authService.GetMeAsync(CurrentUserId));
}
