using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => ToResponse(await categoryService.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
        => ToResponse(await categoryService.CreateAsync(dto));
}
