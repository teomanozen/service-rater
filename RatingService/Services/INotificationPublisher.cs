using RatingService.DTOs;

namespace RatingService.Services;

public interface INotificationPublisher
{
    Task PublishRatingNotificationAsync(RatingResponse rating);
}