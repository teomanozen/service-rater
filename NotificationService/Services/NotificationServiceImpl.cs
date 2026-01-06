using NotificationService.DTOs;
using NotificationService.Storage;

namespace NotificationService.Services;

/// <summary>
/// Handles notification operations with validation and business logic.
/// Enforces limits and provides metadata in responses.
/// </summary>
public class NotificationServiceImpl : INotificationService
{
    private readonly INotificationStore _store;
    private readonly ILogger<NotificationServiceImpl> _logger;

    public NotificationServiceImpl(INotificationStore store, ILogger<NotificationServiceImpl> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<NotificationsResponse> GetNotificationsAsync(int serviceProviderId, int limit = 10)
    {
        if (serviceProviderId <= 0)
            throw new ArgumentException("Service Provider ID must be greater than 0");

        if (limit < 1 || limit > 50)
            throw new ArgumentException("Limit must be between 1 and 50");

        _logger.LogInformation("Getting notifications for Service Provider {ServiceProviderId}, limit: {Limit}", 
            serviceProviderId, limit);

        var notifications = await _store.GetAndConsumeNotificationsAsync(serviceProviderId, limit);
        var remainingCount = await _store.GetNotificationCountAsync(serviceProviderId);

        return new NotificationsResponse
        {
            Notifications = notifications,
            Count = notifications.Count,
            HasMore = remainingCount > 0,
            LastNotificationTime = notifications.LastOrDefault()?.CreatedAt
        };
    }

    public async Task<int> GetNotificationCountAsync(int serviceProviderId)
    {
        if (serviceProviderId <= 0)
            throw new ArgumentException("Service Provider ID must be greater than 0");

        return await _store.GetNotificationCountAsync(serviceProviderId);
    }
}