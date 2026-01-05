using RatingService.DTOs;
using System.Text;
using System.Text.Json;

namespace RatingService.Services;

/// <summary>
/// Publishes notifications via HTTP. Failures are logged but never propagated
/// to ensure notification issues don't affect rating operations.
/// </summary>
public class HttpNotificationPublisher : INotificationPublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpNotificationPublisher> _logger;
    private readonly string _notificationServiceUrl;

    public HttpNotificationPublisher(HttpClient httpClient, IConfiguration configuration, ILogger<HttpNotificationPublisher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationServiceUrl = configuration["NotificationService:BaseUrl"] ?? "https://localhost:7227";
    }

    public async Task PublishRatingNotificationAsync(RatingResponse rating)
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
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Publishing notification for Service Provider {ServiceProviderId} to {Url}", 
                rating.ServiceProviderId, _notificationServiceUrl);

            
            // Send HTTP POST to notification service's internal endpoint
            // Two services communicate through REST - synchronous with fault tolerance
            var response = await _httpClient.PostAsync($"{_notificationServiceUrl}/api/internal/notifications", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully published notification for rating {RatingId}", rating.Id);
            }
            else
            {
                _logger.LogWarning("Failed to publish notification for rating {RatingId}. Status: {StatusCode}", 
                    rating.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing notification for rating {RatingId}", rating.Id);
            // Don't throw - notification failure shouldn't fail rating creation
        }
    }
}