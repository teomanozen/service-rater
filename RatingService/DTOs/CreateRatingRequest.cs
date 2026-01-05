using System.ComponentModel.DataAnnotations;

namespace RatingService.DTOs;

public class CreateRatingRequest
{
    [Required(ErrorMessage = "Service Provider ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service Provider ID must be greater than 0")]
    public int ServiceProviderId { get; set; }
    
    [Required(ErrorMessage = "Customer ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be greater than 0")]
    public int CustomerId { get; set; }
    
    [Required(ErrorMessage = "Score is required")]
    [Range(1, 5, ErrorMessage = "Score must be between 1 and 5")]
    public int Score { get; set; }
    
    [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
    public string? Comment { get; set; }
}