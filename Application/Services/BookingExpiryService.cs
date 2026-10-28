using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Services;

public class BookingExpiryService(
    IBookingRepository bookingRepository,
    IMessagePublisher publisher,
    IDistributedCache cache) : IBookingExpiryService
{
    public async Task ExpireAsync()
    {
        var expired = await bookingRepository.GetExpiredAsync(DateTime.UtcNow);

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Expired;
            booking.Event.AvailableSeats += booking.Seats;

            await bookingRepository.UpdateAsync(booking);
            await EventCache.InvalidateAsync(cache, booking.EventId);

            await publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Expired" });
        }
    }
}
