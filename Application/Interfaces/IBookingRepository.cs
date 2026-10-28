using Application.ApiResult;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IBookingRepository
{
    Task<PagedResult<Booking>> GetPagedByUserAsync(int userId, BookingStatus? status, int page, int pageSize, CancellationToken ct);
    Task<Booking?> GetByIdAsync(int id);
    Task<List<Booking>> GetExpiredAsync(DateTime now);
    Task<int> CreateAsync(Booking booking);
    Task<int> UpdateAsync(Booking booking);
}
