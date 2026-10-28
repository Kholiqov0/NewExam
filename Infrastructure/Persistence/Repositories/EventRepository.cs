using Application.ApiResult;
using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EventRepository(AppDbContext context) : IEventRepository
{
    public async Task<PagedResult<Event>> GetPagedAsync(EventFilter filter, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var query = context.Events.AsNoTracking().Include(e => e.Category).Where(e => e.StartsAt > now);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();

            query = query.Where(e =>
                e.Title.ToLower().Contains(search) ||
                e.Location.ToLower().Contains(search));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == filter.CategoryId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(e => e.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(e => e.Price <= filter.MaxPrice.Value);

        if (filter.SortBy == "price")
        {
            query = filter.Desc
                ? query.OrderByDescending(e => e.Price).ThenBy(e => e.Id)
                : query.OrderBy(e => e.Price).ThenBy(e => e.Id);
        }
        else if (filter.SortBy == "title")
        {
            query = filter.Desc
                ? query.OrderByDescending(e => e.Title).ThenBy(e => e.Id)
                : query.OrderBy(e => e.Title).ThenBy(e => e.Id);
        }
        else
        {
            query = filter.Desc
                ? query.OrderByDescending(e => e.StartsAt).ThenBy(e => e.Id)
                : query.OrderBy(e => e.StartsAt).ThenBy(e => e.Id);
        }

        return await query.ToPagedListAsync(filter.Page, filter.PageSize, ct);
    }

    public async Task<Event?> GetByIdAsync(int id)
        => await context.Events.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<int> CreateAsync(Event ev)
    {
        context.Events.Add(ev);
        await context.SaveChangesAsync();
        return ev.Id;
    }

    public async Task<int> UpdateAsync(Event ev)
        => await context.SaveChangesAsync();
}
