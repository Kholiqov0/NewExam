using Application.ApiResult;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<PagedResult<Booking>> GetPagedByUserAsync(int userId, BookingStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Bookings.AsNoTracking().Include(b => b.Event).Where(b => b.UserId == userId);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        query = query.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id);

        return await query.ToPagedListAsync(page, pageSize, ct);
    }

    public async Task<Booking?> GetByIdAsync(int id)
        => await context.Bookings.Include(b => b.Event).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<Booking>> GetExpiredAsync(DateTime now)
        => await context.Bookings
            .Include(b => b.Event)
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt <= now)
            .ToListAsync();

    public async Task<int> CreateAsync(Booking booking)
    {
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking.Id;
    }

    public async Task<int> UpdateAsync(Booking booking)
        => await context.SaveChangesAsync();
}
