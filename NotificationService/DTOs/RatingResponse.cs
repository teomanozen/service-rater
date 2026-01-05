namespace NotificationService.DTOs;

public class RatingNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ServiceProviderId { get; set; }
    public int CustomerId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "NewRating";
}