namespace NotificationService.DTOs;

public class NotificationsResponse
{
    public List<RatingNotification> Notifications { get; set; } = new();
    public bool HasMore { get; set; }
    public int Count { get; set; }
    public DateTime? LastNotificationTime { get; set; }
}