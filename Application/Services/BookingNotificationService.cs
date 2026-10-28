using Application.DTOs;
using Application.Interfaces;

namespace Application.Services;

public class BookingNotificationService(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailService emailService) : IBookingNotificationService
{
    public async Task SendAsync(BookingMessage message)
    {
        var booking = await bookingRepository.GetByIdAsync(message.BookingId);

        if (booking == null)
            return;

        var user = await userRepository.GetByIdAsync(booking.UserId);

        if (user == null || string.IsNullOrEmpty(user.Email))
            return;

        var details = $"Event: {booking.Event.Title}\nSeats: {booking.Seats}\nTotal: {booking.TotalPrice}";

        string subject;
        string body;

        if (message.Type == "Created")
        {
            subject = $"Booking #{booking.Id} created";
            body = $"Your booking is created. Please pay before {booking.ExpiresAt:u}.\n{details}";
        }
        else if (message.Type == "Confirmed")
        {
            subject = $"Booking #{booking.Id} paid";
            body = $"Your booking is paid.\n{details}";
        }
        else if (message.Type == "Cancelled")
        {
            subject = $"Booking #{booking.Id} cancelled";
            body = $"Your booking is cancelled.\n{details}";
        }
        else if (message.Type == "Expired")
        {
            subject = $"Booking #{booking.Id} expired";
            body = $"The hold time of your booking is over.\n{details}";
        }
        else
        {
            return;
        }

        await emailService.SendAsync(user.Email, subject, body);
    }
}
