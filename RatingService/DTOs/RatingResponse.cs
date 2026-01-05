namespace RatingService.DTOs;

public class RatingResponse
{
    public int Id { get; set; }
    public int ServiceProviderId { get; set; }
    public int CustomerId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}