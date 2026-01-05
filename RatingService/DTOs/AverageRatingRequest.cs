namespace RatingService.DTOs;

public class AverageRatingResponse
{
    public int ServiceProviderId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public DateTime? LastRatedAt { get; set; }
}