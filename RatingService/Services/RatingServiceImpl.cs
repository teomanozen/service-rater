using Microsoft.EntityFrameworkCore;
using RatingService.Data;
using RatingService.DTOs;
using RatingService.Models;

namespace RatingService.Services;

/// <summary>
/// Handles creation of ratings and retrieval of rating statistics.
/// Rating creation succeeds even when notification delivery fails.
/// </summary>
public class RatingServiceImpl : IRatingService
{
    private readonly RatingDbContext _context;
    private readonly ILogger<RatingServiceImpl> _logger;
    private readonly INotificationPublisher _notificationPublisher;

    public RatingServiceImpl(RatingDbContext context, INotificationPublisher notificationPublisher,ILogger<RatingServiceImpl> logger)
    {
        _context = context;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<RatingResponse> CreateRatingAsync(CreateRatingRequest request)
    {
        _logger.LogInformation("Creating rating for Service Provider {ServiceProviderId} by Customer {CustomerId}", 
            request.ServiceProviderId, request.CustomerId);

        try
        {
            var rating = new Rating
            {
                ServiceProviderId = request.ServiceProviderId,
                CustomerId = request.CustomerId,
                Score = request.Score,
                Comment = request.Comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // Persist first - this is the critical operation that must succeed
            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Rating {RatingId} created successfully", rating.Id);
            
            var response = new RatingResponse
            {
                Id = rating.Id,
                ServiceProviderId = rating.ServiceProviderId,
                CustomerId = rating.CustomerId,
                Score = rating.Score,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt
            };
            
            // Notification failure must not affect rating persistence
            try
            {
                await _notificationPublisher.PublishRatingNotificationAsync(response);
                _logger.LogInformation("Notification published for rating {RatingId}", rating.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish notification for rating {RatingId} - continuing anyway", rating.Id);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rating for Service Provider {ServiceProviderId}", 
                request.ServiceProviderId);
            throw;
        }
    }
    
    public async Task<AverageRatingResponse?> GetAverageRatingAsync(int serviceProviderId)
    {
        _logger.LogInformation("Getting average rating for Service Provider {ServiceProviderId}", serviceProviderId);
    
        try
        {
            var result = await _context.Ratings
                .Where(r => r.ServiceProviderId == serviceProviderId)
                .GroupBy(r => r.ServiceProviderId)
                .Select(g => new AverageRatingResponse
                {
                    ServiceProviderId = g.Key,
                    AverageRating = Math.Round(g.Average(r => r.Score), 2),
                    TotalRatings = g.Count(),
                    LastRatedAt = g.Max(r => r.CreatedAt)
                })
                .FirstOrDefaultAsync();
        
            if (result == null)
            {
                _logger.LogInformation("No ratings found for Service Provider {ServiceProviderId}", serviceProviderId);
                return null;
            }
        
            _logger.LogInformation(
                "Service Provider {ServiceProviderId} has average rating {AverageRating} from {TotalRatings} ratings",
                result.ServiceProviderId, result.AverageRating, result.TotalRatings);
        
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average rating for Service Provider {ServiceProviderId}",
                serviceProviderId);
            throw;
        }
    }
    
}