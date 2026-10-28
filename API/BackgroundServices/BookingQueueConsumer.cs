using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace API.BackgroundServices;

public class BookingQueueConsumer(
    RabbitMqSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingQueueConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
                };

                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                await channel.BasicQosAsync(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonSerializer.Deserialize<BookingMessage>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

                        using var scope = scopeFactory.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IBookingNotificationService>();

                        await service.SendAsync(message);

                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process booking message");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                };

                await channel.BasicConsumeAsync(settings.QueueName, false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ consumer failed, retry in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
