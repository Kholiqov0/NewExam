using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IMessagePublisher publisher,
    IDistributedCache cache,
    BookingSettings settings) : IBookingService
{
    public async Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, int currentUserId)
    {
        if (dto.Seats < 1 || dto.Seats > 10)
            return Result<BookingDto>.Fail("Seats must be between 1 and 10", ErrorType.Validation);

        var ev = await eventRepository.GetByIdAsync(dto.EventId);

        if (ev == null)
            return Result<BookingDto>.Fail("Event not found", ErrorType.NotFound);

        if (ev.StartsAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail("Event has already started", ErrorType.Conflict);

        if (ev.OrganizerId == currentUserId)
            return Result<BookingDto>.Fail("Organizer cannot book own event", ErrorType.Forbidden);

        if (ev.AvailableSeats < dto.Seats)
            return Result<BookingDto>.Fail("Not enough available seats", ErrorType.Conflict);

        ev.AvailableSeats -= dto.Seats;

        var now = DateTime.UtcNow;

        var booking = new Booking
        {
            EventId = ev.Id,
            Event = ev,
            UserId = currentUserId,
            Seats = dto.Seats,
            TotalPrice = ev.Price * dto.Seats,
            Status = BookingStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(settings.HoldMinutes)
        };

        await bookingRepository.CreateAsync(booking);
        await EventCache.InvalidateAsync(cache, ev.Id);

        await publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Created" });

        var result = new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = ev.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        };

        return Result<BookingDto>.Success(result);
    }

    public async Task<Result<PagedResult<BookingDto>>> GetMyAsync(string? status, int page, int pageSize, int currentUserId, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;
        if (pageSize < 10) pageSize = 10;

        BookingStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BookingStatus>(status.Trim(), true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<PagedResult<BookingDto>>.Fail("Status must be Pending, Confirmed, Cancelled or Expired", ErrorType.Validation);

            statusFilter = parsed;
        }

        var paged = await bookingRepository.GetPagedByUserAsync(currentUserId, statusFilter, page, pageSize, ct);

        var items = paged.Items.Select(b => new BookingDto
        {
            Id = b.Id,
            EventId = b.EventId,
            EventTitle = b.Event.Title,
            Seats = b.Seats,
            TotalPrice = b.TotalPrice,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            ExpiresAt = b.ExpiresAt
        }).ToList();

        return Result<PagedResult<BookingDto>>.Success(
            new PagedResult<BookingDto>(items, paged.Page, paged.PageSize, paged.TotalCount));
    }

    public async Task<Result<BookingDto>> PayAsync(int id, int currentUserId)
    {
        var booking = await bookingRepository.GetByIdAsync(id);

        if (booking == null)
            return Result<BookingDto>.Fail("Booking not found", ErrorType.NotFound);

        if (booking.UserId != currentUserId)
            return Result<BookingDto>.Fail("Access denied", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending || booking.ExpiresAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail("Only pending and not expired booking can be paid", ErrorType.Conflict);

        booking.Status = BookingStatus.Confirmed;

        await bookingRepository.UpdateAsync(booking);

        await publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Confirmed" });

        var result = new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = booking.Event.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        };

        return Result<BookingDto>.Success(result);
    }

    public async Task<Result<BookingDto>> CancelAsync(int id, int currentUserId, bool isAdmin)
    {
        var booking = await bookingRepository.GetByIdAsync(id);

        if (booking == null)
            return Result<BookingDto>.Fail("Booking not found", ErrorType.NotFound);

        if (booking.UserId != currentUserId && !isAdmin)
            return Result<BookingDto>.Fail("Access denied", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            return Result<BookingDto>.Fail("Only pending or confirmed booking can be cancelled", ErrorType.Conflict);

        booking.Status = BookingStatus.Cancelled;
        booking.Event.AvailableSeats += booking.Seats;

        await bookingRepository.UpdateAsync(booking);
        await EventCache.InvalidateAsync(cache, booking.EventId);

        await publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Cancelled" });

        var result = new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = booking.Event.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        };

        return Result<BookingDto>.Success(result);
    }
}
