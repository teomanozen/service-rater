using NotificationService.DTOs;

namespace NotificationService.Services;

public interface INotificationService
{
    Task AddNotificationAsync(RatingNotification notification);
    Task<NotificationsResponse> GetNotificationsAsync(int serviceProviderId, int limit = 10);
    Task<int> GetNotificationCountAsync(int serviceProviderId);
}