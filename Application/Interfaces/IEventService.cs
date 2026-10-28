using Application.ApiResult;
using Application.DTOs;
using Domain.Filters;

namespace Application.Interfaces;

public interface IEventService
{
    Task<Result<PagedResult<EventDto>>> GetPagedAsync(EventFilter filter, CancellationToken ct);
    Task<Result<EventDto>> GetByIdAsync(int id);
    Task<Result<EventDto>> CreateAsync(CreateEventDto dto, int currentUserId);
    Task<Result<EventDto>> UpdateAsync(int id, UpdateEventDto dto, int currentUserId, bool isAdmin);
}
