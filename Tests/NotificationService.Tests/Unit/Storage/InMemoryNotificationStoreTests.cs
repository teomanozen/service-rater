using NUnit.Framework;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.DTOs;
using NotificationService.Storage;

namespace NotificationService.Tests.Unit.Storage;

[TestFixture]
public class InMemoryNotificationStoreTests
{
    private Mock<ILogger<InMemoryNotificationStore>> _mockLogger;
    private InMemoryNotificationStore _store;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<InMemoryNotificationStore>>();
        _store = new InMemoryNotificationStore(_mockLogger.Object);
    }

    // ===== AddNotification Tests =====

    [Test]
    public async Task AddNotification_ValidNotification_AddsSuccessfully()
    {
        // Arrange
        var notification = new RatingNotification
        {
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 456,
            Score = 5,
            Comment = "Great!",
            CreatedAt = DateTime.UtcNow,
            Type = "NewRating"
        };

        // Act
        await _store.AddNotificationAsync(notification);

        // Assert
        var count = await _store.GetNotificationCountAsync(123);
        count.Should().Be(1);
    }

    [Test]
    public async Task AddNotification_MultipleNotifications_AllAdded()
    {
        // Arrange
        var notifications = new[]
        {
            new RatingNotification { Id = Guid.NewGuid().ToString(), ServiceProviderId = 123, CustomerId = 1, Score = 5, CreatedAt = DateTime.UtcNow },
            new RatingNotification { Id = Guid.NewGuid().ToString(), ServiceProviderId = 123, CustomerId = 2, Score = 4, CreatedAt = DateTime.UtcNow },
            new RatingNotification { Id = Guid.NewGuid().ToString(), ServiceProviderId = 123, CustomerId = 3, Score = 3, CreatedAt = DateTime.UtcNow }
        };

        // Act
        foreach (var notification in notifications)
        {
            await _store.AddNotificationAsync(notification);
        }

        // Assert
        var count = await _store.GetNotificationCountAsync(123);
        count.Should().Be(3);
    }

    [Test]
    public async Task AddNotification_DifferentProviders_SeparateQueues()
    {
        // Arrange
        var notification1 = new RatingNotification 
        { 
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 100,
            CustomerId = 1, 
            Score = 5, 
            CreatedAt = DateTime.UtcNow 
        };
        
        var notification2 = new RatingNotification 
        { 
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 200,
            CustomerId = 2, 
            Score = 4, 
            CreatedAt = DateTime.UtcNow 
        };

        // Act
        await _store.AddNotificationAsync(notification1);
        await _store.AddNotificationAsync(notification2);

        // Assert
        var count100 = await _store.GetNotificationCountAsync(100);
        var count200 = await _store.GetNotificationCountAsync(200);
        
        count100.Should().Be(1);
        count200.Should().Be(1);
    }

    [Test]
    public async Task AddNotification_WithComment_StoresComment()
    {
        // Arrange
        var notification = new RatingNotification
        {
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 456,
            Score = 5,
            Comment = "Excellent service!",
            CreatedAt = DateTime.UtcNow,
            Type = "NewRating"
        };

        // Act
        await _store.AddNotificationAsync(notification);
        var result = await _store.GetAndConsumeNotificationsAsync(123, 10);

        // Assert
        result[0].Comment.Should().Be("Excellent service!");
    }

    // ===== GetAndConsumeNotifications Tests =====

    [Test]
    public async Task GetAndConsume_ExistingNotifications_ReturnsAndRemoves()
    {
        // Arrange
        var notification1 = new RatingNotification 
        { 
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 1, 
            Score = 5, 
            CreatedAt = DateTime.UtcNow 
        };
        
        var notification2 = new RatingNotification 
        { 
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 2, 
            Score = 4, 
            CreatedAt = DateTime.UtcNow 
        };

        await _store.AddNotificationAsync(notification1);
        await _store.AddNotificationAsync(notification2);

        // Act
        var result = await _store.GetAndConsumeNotificationsAsync(123, limit: 10);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(notification1.Id);
        result[1].Id.Should().Be(notification2.Id);
        
        var countAfter = await _store.GetNotificationCountAsync(123);
        countAfter.Should().Be(0);
    }

    [Test]
    public async Task GetAndConsume_RespectsLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _store.AddNotificationAsync(new RatingNotification
            {
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = i,
                Score = 5,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act
        var result = await _store.GetAndConsumeNotificationsAsync(123, limit: 3);

        // Assert
        result.Should().HaveCount(3);
        
        var remaining = await _store.GetNotificationCountAsync(123);
        remaining.Should().Be(2);
    }

    [Test]
    public async Task GetAndConsume_EmptyQueue_ReturnsEmptyList()
    {
        // Act
        var result = await _store.GetAndConsumeNotificationsAsync(999, limit: 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAndConsume_OnceOnlyConsumption_NotificationsNotReturned()
    {
        // Arrange
        var notification = new RatingNotification
        {
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 1,
            Score = 5,
            CreatedAt = DateTime.UtcNow
        };

        await _store.AddNotificationAsync(notification);

        // Act
        var firstCall = await _store.GetAndConsumeNotificationsAsync(123, 10);
        var secondCall = await _store.GetAndConsumeNotificationsAsync(123, 10);

        // Assert
        firstCall.Should().HaveCount(1);
        secondCall.Should().BeEmpty();
    }

    [Test]
    public async Task GetAndConsume_FIFO_ReturnsInCorrectOrder()
    {
        // Arrange
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var id3 = Guid.NewGuid().ToString();

        await _store.AddNotificationAsync(new RatingNotification { Id = id1, ServiceProviderId = 123, CustomerId = 1, Score = 5, CreatedAt = DateTime.UtcNow });
        await _store.AddNotificationAsync(new RatingNotification { Id = id2, ServiceProviderId = 123, CustomerId = 2, Score = 4, CreatedAt = DateTime.UtcNow });
        await _store.AddNotificationAsync(new RatingNotification { Id = id3, ServiceProviderId = 123, CustomerId = 3, Score = 3, CreatedAt = DateTime.UtcNow });

        // Act
        var result = await _store.GetAndConsumeNotificationsAsync(123, 10);

        // Assert
        result[0].Id.Should().Be(id1);
        result[1].Id.Should().Be(id2);
        result[2].Id.Should().Be(id3);
    }

    [Test]
    public async Task GetAndConsume_DefaultTypeIsNewRating()
    {
        // Arrange
        var notification = new RatingNotification
        {
            Id = Guid.NewGuid().ToString(),
            ServiceProviderId = 123,
            CustomerId = 1,
            Score = 5,
            CreatedAt = DateTime.UtcNow
        };

        await _store.AddNotificationAsync(notification);

        // Act
        var result = await _store.GetAndConsumeNotificationsAsync(123, 10);

        // Assert
        result[0].Type.Should().Be("NewRating");
    }

    // ===== GetNotificationCount Tests =====

    [Test]
    public async Task GetCount_NoNotifications_ReturnsZero()
    {
        // Act
        var count = await _store.GetNotificationCountAsync(999);

        // Assert
        count.Should().Be(0);
    }

    [Test]
    public async Task GetCount_AfterAdding_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 7; i++)
        {
            await _store.AddNotificationAsync(new RatingNotification
            {
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = i,
                Score = 5,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act
        var count = await _store.GetNotificationCountAsync(123);

        // Assert
        count.Should().Be(7);
    }

    [Test]
    public async Task GetCount_AfterConsuming_ReturnsUpdatedCount()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _store.AddNotificationAsync(new RatingNotification
            {
                Id = Guid.NewGuid().ToString(),
                ServiceProviderId = 123,
                CustomerId = i,
                Score = 5,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act
        await _store.GetAndConsumeNotificationsAsync(123, 6);
        var count = await _store.GetNotificationCountAsync(123);

        // Assert
        count.Should().Be(4);
    }
}