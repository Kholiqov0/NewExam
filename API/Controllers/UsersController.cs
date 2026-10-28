using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/users")]
public class UsersController(IUserService userService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserFilter filter, CancellationToken ct)
        => ToResponse(await userService.GetPagedAsync(filter, ct));

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
        => ToResponse(await userService.UpdateRoleAsync(id, dto));
}
