using Application.ApiResult;
using Application.Identity;
using Application.Interfaces;
using Domain.Filters;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<PagedResult<User>> GetPagedAsync(UserFilter filter, CancellationToken ct)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();

            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email!.ToLower().Contains(search));
        }

        query = query.OrderBy(u => u.Id);

        return await query.ToPagedListAsync(filter.Page, filter.PageSize, ct);
    }

    public async Task<User?> GetByIdAsync(int id)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id);
}
