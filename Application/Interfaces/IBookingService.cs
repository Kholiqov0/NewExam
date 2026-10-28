using Application.ApiResult;
using Application.DTOs;

namespace Application.Interfaces;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, int currentUserId);
    Task<Result<PagedResult<BookingDto>>> GetMyAsync(string? status, int page, int pageSize, int currentUserId, CancellationToken ct);
    Task<Result<BookingDto>> PayAsync(int id, int currentUserId);
    Task<Result<BookingDto>> CancelAsync(int id, int currentUserId, bool isAdmin);
}
