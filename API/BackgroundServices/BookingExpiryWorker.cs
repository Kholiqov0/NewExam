using Application.Interfaces;
using Application.Settings;

namespace API.BackgroundServices;

public class BookingExpiryWorker(
    IServiceScopeFactory scopeFactory,
    BookingSettings settings,
    ILogger<BookingExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBookingExpiryService>();

                await service.ExpireAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Booking expiry check failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.ExpiryCheckSeconds), stoppingToken);
        }
    }
}
