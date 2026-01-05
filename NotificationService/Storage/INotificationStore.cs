using NotificationService.DTOs;

namespace NotificationService.Storage;

public interface INotificationStore
{
    Task AddNotificationAsync(RatingNotification notification);
    Task<List<RatingNotification>> GetAndConsumeNotificationsAsync(int serviceProviderId, int limit = 10);
    Task<int> GetNotificationCountAsync(int serviceProviderId);
}