using Application.ApiResult;
using Domain.Entities;
using Domain.Filters;

namespace Application.Interfaces;

public interface IEventRepository
{
    Task<PagedResult<Event>> GetPagedAsync(EventFilter filter, CancellationToken ct);
    Task<Event?> GetByIdAsync(int id);
    Task<int> CreateAsync(Event ev);
    Task<int> UpdateAsync(Event ev);
}
