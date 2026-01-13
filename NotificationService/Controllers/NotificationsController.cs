using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using NotificationService.DTOs;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService, 
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets notifications for a service provider. Once-only consumption - returned notifications are removed.
    /// </summary>
    /// <param name="serviceProviderId">Service provider ID (must be greater than 0)</param>
    /// <param name="limit">Maximum notifications to return (1-50, default: 10)</param>
    /// <returns>List of notifications with metadata</returns>
    /// <response code="200">Notifications retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationsResponse>> GetNotifications(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Service Provider ID must be greater than 0")] 
        int serviceProviderId,
        [FromQuery, Range(1, 50, ErrorMessage = "Limit must be between 1 and 50")] 
        int limit = 10)
    {
        try
        {
            var result = await _notificationService.GetNotificationsAsync(serviceProviderId, limit);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Request"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Unexpected error getting notifications for Service Provider {ServiceProviderId}", 
                serviceProviderId);
            return Problem(
                detail: "An error occurred while retrieving notifications",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Gets notification count for a service provider without consuming them
    /// </summary>
    /// <param name="serviceProviderId">Service provider ID (must be greater than 0)</param>
    /// <returns>Number of pending notifications</returns>
    /// <response code="200">Count retrieved successfully</response>
    /// <response code="400">Invalid service provider ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetNotificationCount(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Service Provider ID must be greater than 0")] 
        int serviceProviderId)
    {
        try
        {
            var count = await _notificationService.GetNotificationCountAsync(serviceProviderId);
            
            return Ok(new 
            { 
                serviceProviderId = serviceProviderId, 
                pendingNotifications = count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error getting notification count for Service Provider {ServiceProviderId}", 
                serviceProviderId);
            return Problem(
                detail: "An error occurred while retrieving notification count",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }
    
}