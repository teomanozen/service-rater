using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationService.Services;
using NotificationService.Storage;
using NotificationService.DTOs;

namespace NotificationService.Tests.Unit.Services;

[TestFixture]
public class NotificationServiceTests
{
    private NotificationServiceImpl _service;
    private Mock<INotificationStore> _mockStore;
    private Mock<ILogger<NotificationServiceImpl>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockStore = new Mock<INotificationStore>();
        _mockLogger = new Mock<ILogger<NotificationServiceImpl>>();
        _service = new NotificationServiceImpl(_mockStore.Object, _mockLogger.Object);
    }
    
    // ===== GetNotifications Tests =====

    [Test]
    public async Task GetNotifications_ValidRequest_ReturnsNotifications()
    {
        // Arrange
        var notifications = new List<RatingNotification>
        {
            new RatingNotification 
            { 
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = 1, 
                Score = 5,
                Comment = "Excellent!",
                CreatedAt = DateTime.UtcNow,
                Type = "NewRating"
            },
            new RatingNotification 
            { 
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = 2, 
                Score = 4,
                Comment = "Good",
                CreatedAt = DateTime.UtcNow,
                Type = "NewRating"
            }
        };

        _mockStore.Setup(x => x.GetAndConsumeNotificationsAsync(123, 10))
            .ReturnsAsync(notifications);
        
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetNotificationsAsync(123, 10);

        // Assert
        result.Should().NotBeNull();
        result.Notifications.Should().HaveCount(2);
        result.Count.Should().Be(2);
        result.HasMore.Should().BeFalse();
    }

    [Test]
    public async Task GetNotifications_HasMoreNotifications_ReturnsTrueHasMore()
    {
        // Arrange
        var notifications = new List<RatingNotification>
        {
            new RatingNotification 
            { 
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = 1, 
                Score = 5,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockStore.Setup(x => x.GetAndConsumeNotificationsAsync(123, 10))
            .ReturnsAsync(notifications);
        
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetNotificationsAsync(123, 10);

        // Assert
        result.HasMore.Should().BeTrue();
    }

    [Test]
    public async Task GetNotifications_NoNotifications_ReturnsEmptyList()
    {
        // Arrange
        _mockStore.Setup(x => x.GetAndConsumeNotificationsAsync(123, 10))
            .ReturnsAsync(new List<RatingNotification>());
        
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetNotificationsAsync(123, 10);

        // Assert
        result.Notifications.Should().BeEmpty();
        result.Count.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Test]
    public void GetNotifications_InvalidServiceProviderId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.GetNotificationsAsync(0, 10));
    }

    [Test]
    public void GetNotifications_LimitTooLow_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.GetNotificationsAsync(123, 0));
    }

    [Test]
    public void GetNotifications_LimitTooHigh_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.GetNotificationsAsync(123, 51));
    }

    [Test]
    public async Task GetNotifications_DefaultLimit_Uses10()
    {
        // Arrange
        _mockStore.Setup(x => x.GetAndConsumeNotificationsAsync(123, 10))
            .ReturnsAsync(new List<RatingNotification>());
        
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(0);

        // Act
        await _service.GetNotificationsAsync(123);

        // Assert
        _mockStore.Verify(
            x => x.GetAndConsumeNotificationsAsync(123, 10),
            Times.Once);
    }

    [Test]
    public async Task GetNotifications_WithComments_ReturnsCommentsInResponse()
    {
        // Arrange
        var notifications = new List<RatingNotification>
        {
            new RatingNotification 
            { 
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = 1, 
                Score = 5,
                Comment = "Excellent service!",
                CreatedAt = DateTime.UtcNow,
                Type = "NewRating"
            }
        };

        _mockStore.Setup(x => x.GetAndConsumeNotificationsAsync(123, 10))
            .ReturnsAsync(notifications);
        
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetNotificationsAsync(123, 10);

        // Assert
        result.Notifications[0].Comment.Should().Be("Excellent service!");
    }

    // ===== GetNotificationCount Tests =====

    [Test]
    public async Task GetNotificationCount_ValidServiceProviderId_ReturnsCount()
    {
        // Arrange
        _mockStore.Setup(x => x.GetNotificationCountAsync(123))
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetNotificationCountAsync(123);

        // Assert
        result.Should().Be(5);
    }

    [Test]
    public async Task GetNotificationCount_NoNotifications_ReturnsZero()
    {
        // Arrange
        _mockStore.Setup(x => x.GetNotificationCountAsync(999))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetNotificationCountAsync(999);

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public void GetNotificationCount_InvalidServiceProviderId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.GetNotificationCountAsync(0));
    }
}