using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RatingService.Models;

[Table("ratings")]
public class Rating
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("service_provider_id")]
    public int ServiceProviderId { get; set; }
    
    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }
    
    [Required]
    [Range(1, 5, ErrorMessage = "Score must be between 1 and 5")]
    [Column("score")]
    public int Score { get; set; }
    
    [Column("comment")]
    public string? Comment { get; set; }
    
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}