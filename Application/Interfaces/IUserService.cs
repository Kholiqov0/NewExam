using Application.ApiResult;
using Application.DTOs;
using Domain.Filters;

namespace Application.Interfaces;

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserFilter filter, CancellationToken ct);
    Task<Result<UserDto>> UpdateRoleAsync(int id, UpdateUserRoleDto dto);
}
