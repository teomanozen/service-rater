using StackExchange.Redis;
using System.Text.Json;
using NotificationService.DTOs;

namespace NotificationService.Storage;

/// <summary>
/// Redis-based notification storage with persistence and horizontal scaling support.
/// Uses Redis Lists for FIFO queue behavior.
/// </summary>
public class RedisNotificationStore : INotificationStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisNotificationStore> _logger;
    private readonly IDatabase _db;

    public RedisNotificationStore(
        IConnectionMultiplexer redis,
        ILogger<RedisNotificationStore> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = _redis.GetDatabase();
    }

    public async Task AddNotificationAsync(RatingNotification notification)
    {
        var key = GetKey(notification.ServiceProviderId);
        var json = JsonSerializer.Serialize(notification);
        
        // LPUSH: Add to left (start) of list
        await _db.ListLeftPushAsync(key, json);
        
        // Optional: Set expiry (notifications auto-delete after 7 days)
        await _db.KeyExpireAsync(key, TimeSpan.FromDays(7));
        
        _logger.LogInformation(
            "Added notification {NotificationId} to Redis for Service Provider {ServiceProviderId}",
            notification.Id,
            notification.ServiceProviderId);
    }

    public async Task<List<RatingNotification>> GetAndConsumeNotificationsAsync(
        int serviceProviderId,
        int limit = 10)
    {
        var key = GetKey(serviceProviderId);
        var notifications = new List<RatingNotification>();

        // RPOP: Remove from right (end) - FIFO behavior
        for (int i = 0; i < limit; i++)
        {
            var json = await _db.ListRightPopAsync(key);

            if (json.IsNullOrEmpty)
                break;

            try
            {
                var notification = JsonSerializer.Deserialize<RatingNotification>(json!);
                if (notification != null)
                {
                    notifications.Add(notification);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize notification from Redis: {Json}", json);
            }
        }

        _logger.LogInformation(
            "Retrieved and consumed {Count} notifications from Redis for Service Provider {ServiceProviderId}",
            notifications.Count,
            serviceProviderId);

        return notifications;
    }

    public async Task<int> GetNotificationCountAsync(int serviceProviderId)
    {
        var key = GetKey(serviceProviderId);
        var count = await _db.ListLengthAsync(key);

        return (int)count;
    }

    /// <summary>
    /// Clear all notifications. Used for testing.
    /// </summary>
    public async Task ClearAll()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await server.FlushDatabaseAsync();

        _logger.LogDebug("Cleared all notifications from Redis");
    }

    private static string GetKey(int serviceProviderId)
    {
        return $"notifications:{serviceProviderId}";
    }
}