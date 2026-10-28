using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync()
        => await context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id)
        => await context.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Category?> GetByNameAsync(string name)
    {
        var lower = name.ToLower();
        return await context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
    }

    public async Task<int> CreateAsync(Category category)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }
}
