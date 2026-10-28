using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<Result<List<CategoryDto>>> GetAllAsync()
    {
        var categories = await categoryRepository.GetAllAsync();

        var result = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        return Result<List<CategoryDto>>.Success(result);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        var name = dto.Name.Trim();

        if (name.Length < 2 || name.Length > 100)
            return Result<CategoryDto>.Fail("Name must be between 2 and 100 characters", ErrorType.Validation);

        if (await categoryRepository.GetByNameAsync(name) != null)
            return Result<CategoryDto>.Fail("Category with this name already exists", ErrorType.Conflict);

        var category = new Category
        {
            Name = name
        };

        await categoryRepository.CreateAsync(category);

        var result = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return Result<CategoryDto>.Success(result);
    }
}
