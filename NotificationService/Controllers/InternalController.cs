using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Internal API for service-to-service communication.
/// Should be protected by network policies in production.
/// </summary>
[ApiController]
[Route("api/internal")]
public class InternalController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<InternalController> _logger;

    public InternalController(INotificationService notificationService, ILogger<InternalController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Receives a new rating notification from Rating Service
    /// </summary>
    /// <param name="notification">Notification details</param>
    /// <returns>Success confirmation</returns>
    /// <response code="204">Notification received successfully</response>
    /// <response code="400">Invalid notification data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddNotification([FromBody] RatingNotification notification)
    {
        try
        {
            await _notificationService.AddNotificationAsync(notification);
            
            // Return 204 No Content - fire-and-forget acknowledgment
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid notification data received");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Notification"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error storing notification");
            return Problem(
                detail: "An error occurred while processing the notification",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }
}