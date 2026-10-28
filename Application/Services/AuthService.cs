using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class AuthService(UserManager<User> userManager, IJwtTokenGenerator jwt, IEmailService emailService) : IAuthService
{
    private const string ResetPurpose = "ResetPassword";

    public async Task<Result<UserDto>> RegisterAsync(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName)) return Result<UserDto>.Fail("FirstName must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.LastName)) return Result<UserDto>.Fail("LastName must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.Email)) return Result<UserDto>.Fail("Email must not be empty", ErrorType.Validation);
        if (!dto.Email.Contains('@')) return Result<UserDto>.Fail("Email must contain @", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.Password)) return Result<UserDto>.Fail("Password must not be empty", ErrorType.Validation);
        if (dto.FirstName.Trim().Length > 250) return Result<UserDto>.Fail("FirstName must not be longer than 250 characters", ErrorType.Validation);
        if (dto.LastName.Trim().Length > 250) return Result<UserDto>.Fail("LastName must not be longer than 250 characters", ErrorType.Validation);
        if (dto.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow)) return Result<UserDto>.Fail("BirthDate must not be in the future", ErrorType.Validation);

        var email = dto.Email.Trim().ToLower();

        if (await userManager.FindByEmailAsync(email) != null)
            return Result<UserDto>.Fail("User with this email already exists", ErrorType.Conflict);

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            BirthDate = dto.BirthDate,
            Email = email,
            UserName = email
        };

        var created = await userManager.CreateAsync(user, dto.Password);

        if (!created.Succeeded)
            return Result<UserDto>.Fail(
                string.Join("; ", created.Errors.Select(e => e.Description)),
                ErrorType.Validation);

        var roleResult = await userManager.AddToRoleAsync(user, nameof(UserRole.User));

        if (!roleResult.Succeeded)
            return Result<UserDto>.Fail(
                string.Join("; ", roleResult.Errors.Select(e => e.Description)),
                ErrorType.Validation);

        var result = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Email = user.Email ?? string.Empty
        };

        return Result<UserDto>.Success(result);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) return Result<AuthResponseDto>.Fail("Email must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.Password)) return Result<AuthResponseDto>.Fail("Password must not be empty", ErrorType.Validation);

        var email = dto.Email.Trim().ToLower();
        var user = await userManager.FindByEmailAsync(email);

        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Result<AuthResponseDto>.Fail("Invalid email or password", ErrorType.Unauthorized);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwt.Generate(user, roles);

        var result = new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate,
                Email = user.Email ?? string.Empty
            }
        };

        return Result<AuthResponseDto>.Success(result);
    }

    public async Task<Result<UserDto>> GetMeAsync(int currentUserId)
    {
        var user = await userManager.FindByIdAsync(currentUserId.ToString());

        if (user == null)
            return Result<UserDto>.Fail("User not found", ErrorType.Unauthorized);

        var result = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Email = user.Email ?? string.Empty
        };

        return Result<UserDto>.Success(result);
    }

    public async Task<Result<string>> ChangePasswordAsync(int currentUserId, ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword)) return Result<string>.Fail("CurrentPassword must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.NewPassword)) return Result<string>.Fail("NewPassword must not be empty", ErrorType.Validation);

        var user = await userManager.FindByIdAsync(currentUserId.ToString());

        if (user == null)
            return Result<string>.Fail("User not found", ErrorType.Unauthorized);

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
            return Result<string>.Fail(
                string.Join("; ", result.Errors.Select(e => e.Description)),
                ErrorType.Validation);

        return Result<string>.Success("Password has been changed");
    }

    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) return Result<string>.Fail("Email must not be empty", ErrorType.Validation);

        var message = "If this email is registered, a code has been sent";

        var user = await userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

        if (user == null) return Result<string>.Success(message);

        var code = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, ResetPurpose);

        await emailService.SendAsync(
            user.Email!,
            "Password reset code",
            $"Your password reset code: {code}\nIf it was not you, just ignore this email.");

        return Result<string>.Success(message);
    }

    public async Task<Result<string>> VerifyCodeAsync(VerifyCodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) return Result<string>.Fail("Email must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.Code)) return Result<string>.Fail("Code must not be empty", ErrorType.Validation);

        var user = await userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

        if (user == null) return Result<string>.Fail("Invalid or expired code", ErrorType.Validation);

        if (await userManager.IsLockedOutAsync(user))
            return Result<string>.Fail("Too many attempts. Try again later", ErrorType.Unauthorized);

        var isValid = await userManager.VerifyUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, ResetPurpose, dto.Code.Trim());

        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result<string>.Fail("Invalid or expired code", ErrorType.Validation);
        }

        return Result<string>.Success("Code is correct");
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) return Result<string>.Fail("Email must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.Code)) return Result<string>.Fail("Code must not be empty", ErrorType.Validation);
        if (string.IsNullOrWhiteSpace(dto.NewPassword)) return Result<string>.Fail("NewPassword must not be empty", ErrorType.Validation);

        var user = await userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

        if (user == null) return Result<string>.Fail("Invalid or expired code", ErrorType.Validation);

        if (await userManager.IsLockedOutAsync(user))
            return Result<string>.Fail("Too many attempts. Try again later", ErrorType.Unauthorized);

        var isValid = await userManager.VerifyUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, ResetPurpose, dto.Code.Trim());

        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return Result<string>.Fail("Invalid or expired code", ErrorType.Validation);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
            return Result<string>.Fail(
                string.Join("; ", result.Errors.Select(e => e.Description)),
                ErrorType.Validation);

        await userManager.ResetAccessFailedCountAsync(user);

        return Result<string>.Success("Password has been changed");
    }
}
