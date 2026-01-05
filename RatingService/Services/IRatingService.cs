using RatingService.DTOs;

namespace RatingService.Services;

public interface IRatingService
{
    Task<RatingResponse> CreateRatingAsync(CreateRatingRequest request);
    Task<AverageRatingResponse?> GetAverageRatingAsync(int serviceProviderId);
}