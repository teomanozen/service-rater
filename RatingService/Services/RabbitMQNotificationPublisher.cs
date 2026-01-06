using RabbitMQ.Client;
using RatingService.DTOs;
using System.Text;
using System.Text.Json;

namespace RatingService.Services;

public class RabbitMQNotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;  // ← Changed from IModel
    private readonly string _queueName;
    private readonly ILogger<RabbitMQNotificationPublisher> _logger;

    public RabbitMQNotificationPublisher(IConfiguration configuration, ILogger<RabbitMQNotificationPublisher> logger)
    {
        _logger = logger;

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
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();  

        _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ publisher configured for queue: {QueueName}", _queueName);
    }

    public Task PublishRatingNotificationAsync(RatingResponse rating)
    {
        try
        {
            var notification = new
            {
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = rating.ServiceProviderId,
                CustomerId = rating.CustomerId,
                Score = rating.Score,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt,
                Type = "NewRating"
            };

            var json = JsonSerializer.Serialize(notification);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            _channel.BasicPublishAsync(
                exchange: "",
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: body).GetAwaiter().GetResult();

            _logger.LogInformation(
                "Published notification to RabbitMQ for Service Provider {ServiceProviderId}, Rating {RatingId}",
                rating.ServiceProviderId,
                rating.Id);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification to RabbitMQ for Rating {RatingId}", rating.Id);
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _logger.LogInformation("RabbitMQ connection closed");
    }
}