using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Domain.Filters;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class UserService(IUserRepository userRepository, UserManager<User> userManager) : IUserService
{
    public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserFilter filter, CancellationToken ct)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 10) filter.PageSize = 10;

        var paged = await userRepository.GetPagedAsync(filter, ct);

        var items = paged.Items.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            BirthDate = u.BirthDate,
            Email = u.Email ?? string.Empty
        }).ToList();

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto>(items, paged.Page, paged.PageSize, paged.TotalCount));
    }

    public async Task<Result<UserDto>> UpdateRoleAsync(int id, UpdateUserRoleDto dto)
    {
        var role = dto.Role.Trim().ToLower();

        if (role != "organizer" && role != "user")
            return Result<UserDto>.Fail("Role must be Organizer or User", ErrorType.Validation);

        var user = await userRepository.GetByIdAsync(id);

        if (user == null)
            return Result<UserDto>.Fail("User not found", ErrorType.NotFound);

        var roleName = role == "organizer" ? nameof(UserRole.Organizer) : nameof(UserRole.User);
        var currentRoles = await userManager.GetRolesAsync(user);

        await userManager.RemoveFromRolesAsync(user, currentRoles);

        var roleResult = await userManager.AddToRoleAsync(user, roleName);

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
}
