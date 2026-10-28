using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public class RabbitMqPublisher(RabbitMqSettings settings, ILogger<RabbitMqPublisher> logger) : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(BookingMessage message)
    {
        await _lock.WaitAsync();

        try
        {
            if (_connection == null || !_connection.IsOpen || _channel == null || !_channel.IsOpen)
            {
                await CloseAsync();

                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var body = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties { Persistent = true };

            await _channel.BasicPublishAsync(string.Empty, settings.QueueName, false, properties, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish message BookingId={BookingId} Type={Type}", message.BookingId, message.Type);
            await CloseAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task CloseAsync()
    {
        try
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
        }
        catch
        {
        }

        _channel = null;
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _lock.Dispose();
    }
}
