using System.Text.Json;
using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class EventService(
    IEventRepository eventRepository,
    ICategoryRepository categoryRepository,
    IDistributedCache cache,
    ILogger<EventService> logger) : IEventService
{
    public async Task<Result<PagedResult<EventDto>>> GetPagedAsync(EventFilter filter, CancellationToken ct)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 10) filter.PageSize = 10;

        var sortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "date" : filter.SortBy.Trim().ToLower();

        if (sortBy != "date" && sortBy != "price" && sortBy != "title")
            return Result<PagedResult<EventDto>>.Fail("SortBy must be date, price or title", ErrorType.Validation);

        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice.Value > filter.MaxPrice.Value)
            return Result<PagedResult<EventDto>>.Fail("MinPrice must not be greater than MaxPrice", ErrorType.Validation);

        filter.SortBy = sortBy;

        var version = await EventCache.GetVersionAsync(cache);
        var key = EventCache.ListKey(version, filter);
        var json = await cache.GetStringAsync(key);

        if (json is not null)
        {
            logger.LogInformation("Cache HIT key={Key}", key);
            var cached = JsonSerializer.Deserialize<PagedResult<EventDto>>(json)!;
            return Result<PagedResult<EventDto>>.Success(cached);
        }

        logger.LogInformation("Cache MISS key={Key}", key);

        var paged = await eventRepository.GetPagedAsync(filter, ct);

        var items = paged.Items.Select(e => new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            StartsAt = e.StartsAt,
            Price = e.Price,
            TotalSeats = e.TotalSeats,
            AvailableSeats = e.AvailableSeats,
            CreatedAt = e.CreatedAt,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            OrganizerId = e.OrganizerId
        }).ToList();

        var result = new PagedResult<EventDto>(items, paged.Page, paged.PageSize, paged.TotalCount);

        await cache.SetStringAsync(key, JsonSerializer.Serialize(result), EventCache.Options());

        return Result<PagedResult<EventDto>>.Success(result);
    }

    public async Task<Result<EventDto>> GetByIdAsync(int id)
    {
        var key = EventCache.ByIdKey(id);
        var json = await cache.GetStringAsync(key);

        if (json is not null)
        {
            logger.LogInformation("Cache HIT key={Key}", key);
            var cached = JsonSerializer.Deserialize<EventDto>(json)!;
            return Result<EventDto>.Success(cached);
        }

        logger.LogInformation("Cache MISS key={Key}", key);

        var ev = await eventRepository.GetByIdAsync(id);

        if (ev == null)
            return Result<EventDto>.Fail("Event not found", ErrorType.NotFound);

        var result = new EventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            Location = ev.Location,
            StartsAt = ev.StartsAt,
            Price = ev.Price,
            TotalSeats = ev.TotalSeats,
            AvailableSeats = ev.AvailableSeats,
            CreatedAt = ev.CreatedAt,
            CategoryId = ev.CategoryId,
            CategoryName = ev.Category.Name,
            OrganizerId = ev.OrganizerId
        };

        await cache.SetStringAsync(key, JsonSerializer.Serialize(result), EventCache.Options());

        return Result<EventDto>.Success(result);
    }

    public async Task<Result<EventDto>> CreateAsync(CreateEventDto dto, int currentUserId)
    {
        var title = dto.Title.Trim();
        var location = dto.Location.Trim();
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (title.Length < 3 || title.Length > 200)
            return Result<EventDto>.Fail("Title must be between 3 and 200 characters", ErrorType.Validation);

        if (location.Length < 2 || location.Length > 200)
            return Result<EventDto>.Fail("Location must be between 2 and 200 characters", ErrorType.Validation);

        if (description != null && description.Length > 2000)
            return Result<EventDto>.Fail("Description must not be longer than 2000 characters", ErrorType.Validation);

        if (dto.Price < 0)
            return Result<EventDto>.Fail("Price must not be negative", ErrorType.Validation);

        if (dto.TotalSeats < 1 || dto.TotalSeats > 100000)
            return Result<EventDto>.Fail("TotalSeats must be between 1 and 100000", ErrorType.Validation);

        if (dto.StartsAt <= DateTime.UtcNow)
            return Result<EventDto>.Fail("StartsAt must be in the future", ErrorType.Validation);

        var category = await categoryRepository.GetByIdAsync(dto.CategoryId);

        if (category == null)
            return Result<EventDto>.Fail("Category not found", ErrorType.NotFound);

        var ev = new Event
        {
            Title = title,
            Description = description,
            Location = location,
            StartsAt = dto.StartsAt,
            Price = dto.Price,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            CreatedAt = DateTime.UtcNow,
            CategoryId = dto.CategoryId,
            OrganizerId = currentUserId
        };

        await eventRepository.CreateAsync(ev);
        await EventCache.InvalidateAsync(cache, ev.Id);

        var result = new EventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            Location = ev.Location,
            StartsAt = ev.StartsAt,
            Price = ev.Price,
            TotalSeats = ev.TotalSeats,
            AvailableSeats = ev.AvailableSeats,
            CreatedAt = ev.CreatedAt,
            CategoryId = ev.CategoryId,
            CategoryName = category.Name,
            OrganizerId = ev.OrganizerId
        };

        return Result<EventDto>.Success(result);
    }

    public async Task<Result<EventDto>> UpdateAsync(int id, UpdateEventDto dto, int currentUserId, bool isAdmin)
    {
        var ev = await eventRepository.GetByIdAsync(id);

        if (ev == null)
            return Result<EventDto>.Fail("Event not found", ErrorType.NotFound);

        if (ev.OrganizerId != currentUserId && !isAdmin)
            return Result<EventDto>.Fail("Access denied", ErrorType.Forbidden);

        var title = dto.Title.Trim();
        var location = dto.Location.Trim();
        var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (title.Length < 3 || title.Length > 200)
            return Result<EventDto>.Fail("Title must be between 3 and 200 characters", ErrorType.Validation);

        if (location.Length < 2 || location.Length > 200)
            return Result<EventDto>.Fail("Location must be between 2 and 200 characters", ErrorType.Validation);

        if (description != null && description.Length > 2000)
            return Result<EventDto>.Fail("Description must not be longer than 2000 characters", ErrorType.Validation);

        if (dto.Price < 0)
            return Result<EventDto>.Fail("Price must not be negative", ErrorType.Validation);

        if (dto.TotalSeats < 1 || dto.TotalSeats > 100000)
            return Result<EventDto>.Fail("TotalSeats must be between 1 and 100000", ErrorType.Validation);

        if (dto.StartsAt <= DateTime.UtcNow)
            return Result<EventDto>.Fail("StartsAt must be in the future", ErrorType.Validation);

        var occupied = ev.TotalSeats - ev.AvailableSeats;

        if (dto.TotalSeats < occupied)
            return Result<EventDto>.Fail("TotalSeats must not be less than already occupied seats", ErrorType.Validation);

        var category = await categoryRepository.GetByIdAsync(dto.CategoryId);

        if (category == null)
            return Result<EventDto>.Fail("Category not found", ErrorType.NotFound);

        ev.Title = title;
        ev.Description = description;
        ev.Location = location;
        ev.StartsAt = dto.StartsAt;
        ev.Price = dto.Price;
        ev.AvailableSeats += dto.TotalSeats - ev.TotalSeats;
        ev.TotalSeats = dto.TotalSeats;
        ev.CategoryId = dto.CategoryId;

        await eventRepository.UpdateAsync(ev);
        await EventCache.InvalidateAsync(cache, ev.Id);

        var result = new EventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            Location = ev.Location,
            StartsAt = ev.StartsAt,
            Price = ev.Price,
            TotalSeats = ev.TotalSeats,
            AvailableSeats = ev.AvailableSeats,
            CreatedAt = ev.CreatedAt,
            CategoryId = ev.CategoryId,
            CategoryName = category.Name,
            OrganizerId = ev.OrganizerId
        };

        return Result<EventDto>.Success(result);
    }
}
