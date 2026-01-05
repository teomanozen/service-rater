using Microsoft.AspNetCore.Mvc;
using RatingService.DTOs;
using RatingService.Services;
using System.ComponentModel.DataAnnotations;

namespace RatingService.Controllers;

[ApiController]
[Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly ILogger<RatingsController> _logger;

    public RatingsController(IRatingService ratingService, ILogger<RatingsController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new rating for a service provider
    /// </summary>
    /// <param name="request">Rating details</param>
    /// <returns>The created rating</returns>
    /// <response code="201">Rating created successfully</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(RatingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RatingResponse>> CreateRating([FromBody] CreateRatingRequest request)
    {
        try
        {
            var result = await _ratingService.CreateRatingAsync(request);
            
            // Return simple Created response since we don't have a GetRating endpoint yet
            return Created($"/api/ratings/{result.Id}", result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rating data provided");
            return Problem(
                detail: "Invalid input data provided",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating rating");
            return Problem(
                detail: "An unexpected error occurred while creating the rating",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Gets the average rating for a service provider
    /// </summary>
    /// <param name="serviceProviderId">Service provider ID (must be greater than 0)</param>
    /// <returns>Average rating statistics</returns>
    /// <response code="200">Average rating retrieved successfully</response>
    /// <response code="400">Invalid service provider ID</response>
    /// <response code="404">No ratings found for this provider</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("average")]
    [ProducesResponseType(typeof(AverageRatingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AverageRatingResponse>> GetAverageRating(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Service Provider ID must be greater than 0")] 
        int serviceProviderId)
    {
        try
        {
            var result = await _ratingService.GetAverageRatingAsync(serviceProviderId);

            if (result == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "No Ratings Found",
                    Detail = $"No ratings exist for service provider {serviceProviderId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error retrieving average rating for Service Provider {ServiceProviderId}", 
                serviceProviderId);
            return Problem(
                detail: "An error occurred while retrieving the average rating",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }
}