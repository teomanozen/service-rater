using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using NotificationService.DTOs;
using NotificationService.Storage;

namespace NotificationService.Services;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel; 
    private readonly string _queueName;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQConsumerService(
        IConfiguration configuration,
        ILogger<RabbitMQConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        var userName = configuration["RabbitMQ:UserName"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        _queueName = configuration["RabbitMQ:QueueName"] ?? "rating-notifications";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();  // ← Changed

        _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();

        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false)
            .GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ consumer configured for queue: {QueueName}", _queueName);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(_channel);  // ← Changed to AsyncEventingBasicConsumer

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received message from RabbitMQ: {Message}", json);

                var notification = JsonSerializer.Deserialize<RatingNotification>(json);

                if (notification != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<INotificationStore>();

                    await store.AddNotificationAsync(notification);

                    _logger.LogInformation(
                        "Stored notification for Service Provider {ServiceProviderId}",
                        notification.ServiceProviderId);

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize notification, rejecting message");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from RabbitMQ");
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer)
            .GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ consumer started");

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        base.Dispose();
        _logger.LogInformation("RabbitMQ consumer stopped");
    }
}