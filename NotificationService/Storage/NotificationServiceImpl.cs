using System.Collections.Concurrent;
using NotificationService.DTOs;

namespace NotificationService.Storage;

/// <summary>
/// Thread-safe in-memory storage for notifications with once-only consumption.
/// Uses concurrent collections for lock-free thread-safe operations.
/// </summary>
public class InMemoryNotificationStore : INotificationStore
{
    // Thread-safe storage: ServiceProviderId -> Queue of notifications
    private readonly ConcurrentDictionary<int, ConcurrentQueue<RatingNotification>> _notifications = new();
    private readonly ILogger<InMemoryNotificationStore> _logger;

    public InMemoryNotificationStore(ILogger<InMemoryNotificationStore> logger)
    {
        _logger = logger;
    }

    public Task AddNotificationAsync(RatingNotification notification)
    {
        // GetOrAdd is atomic - creates queue if doesn't exist, returns existing otherwise
        var queue = _notifications.GetOrAdd(
            notification.ServiceProviderId, 
            _ => new ConcurrentQueue<RatingNotification>());
        
        // Enqueue is thread-safe
        queue.Enqueue(notification);
        
        _logger.LogInformation(
            "Added notification {NotificationId} for Service Provider {ServiceProviderId}", 
            notification.Id, 
            notification.ServiceProviderId);
        
        return Task.CompletedTask;
    }

    public Task<List<RatingNotification>> GetAndConsumeNotificationsAsync(int serviceProviderId, int limit = 10)
    {
        var notifications = new List<RatingNotification>();
        
        if (_notifications.TryGetValue(serviceProviderId, out var queue))
        {
            // TryDequeue is thread-safe - once-only consumption
            for (int i = 0; i < limit && queue.TryDequeue(out var notification); i++)
            {
                notifications.Add(notification);
            }
            
            // Clean up empty queues to prevent memory leak
            if (queue.IsEmpty)
            {
                _notifications.TryRemove(serviceProviderId, out _);
                _logger.LogDebug(
                    "Removed empty queue for Service Provider {ServiceProviderId}", 
                    serviceProviderId);
            }
            
            _logger.LogInformation(
                "Retrieved and consumed {Count} notifications for Service Provider {ServiceProviderId}", 
                notifications.Count, 
                serviceProviderId);
        }
        
        return Task.FromResult(notifications);
    }

    public Task<int> GetNotificationCountAsync(int serviceProviderId)
    {
        if (_notifications.TryGetValue(serviceProviderId, out var queue))
        {
            return Task.FromResult(queue.Count);
        }
        
        return Task.FromResult(0);
    }
}