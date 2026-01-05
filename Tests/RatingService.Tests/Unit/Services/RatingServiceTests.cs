using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RatingService.Services;
using RatingService.Data;
using RatingService.DTOs;
using RatingService.Models;

namespace RatingService.Tests.Unit.Services;

[TestFixture]
public class RatingServiceTests
{
    private RatingServiceImpl _service;
    private RatingDbContext _context;
    private Mock<INotificationPublisher> _mockPublisher;
    private Mock<ILogger<RatingServiceImpl>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<RatingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new RatingDbContext(options);
        _mockPublisher = new Mock<INotificationPublisher>();
        _mockLogger = new Mock<ILogger<RatingServiceImpl>>();
        
        _service = new RatingServiceImpl(_context, _mockPublisher.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    // ===== CreateRating Tests =====

    [Test]
    public async Task CreateRating_ValidInput_ReturnsCreatedRating()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 123,
            CustomerId = 1001,
            Score = 5,
            Comment = "Excellent service!"
        };

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ServiceProviderId.Should().Be(123);
        result.CustomerId.Should().Be(1001);
        result.Score.Should().Be(5);
        result.Comment.Should().Be("Excellent service!");
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateRating_ValidInput_SavesRatingToDatabase()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 456,
            CustomerId = 2002,
            Score = 4,
            Comment = "Good work"
        };

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        var savedRating = await _context.Ratings.FindAsync(result.Id);
        savedRating.Should().NotBeNull();
        savedRating!.ServiceProviderId.Should().Be(456);
        savedRating.CustomerId.Should().Be(2002);
        savedRating.Score.Should().Be(4);
        savedRating.Comment.Should().Be("Good work");
    }

    [Test]
    public async Task CreateRating_ValidInput_CallsNotificationPublisher()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 789,
            CustomerId = 3003,
            Score = 3,
            Comment = "Average service"
        };

        // Act
        await _service.CreateRatingAsync(request);

        // Assert
        _mockPublisher.Verify(
            x => x.PublishRatingNotificationAsync(It.Is<RatingResponse>(r => 
                r.ServiceProviderId == 789 && 
                r.CustomerId == 3003 && 
                r.Score == 3)),
            Times.Once);
    }
    
    [Test]
    public async Task CreateRating_NullComment_CreatesRatingSuccessfully()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 123,
            CustomerId = 1001,
            Score = 4,
            Comment = null
        };

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Comment.Should().BeNull();
        result.Score.Should().Be(4);
    }

    [Test]
    public async Task CreateRating_EmptyComment_CreatesRatingSuccessfully()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 123,
            CustomerId = 1001,
            Score = 3,
            Comment = ""
        };

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Comment.Should().BeEmpty();
        result.Score.Should().Be(3);
    }

    [Test]
    public async Task CreateRating_CommentWithWhitespace_TrimsWhitespace()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 123,
            CustomerId = 1001,
            Score = 5,
            Comment = "  Great service!  "
        };

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        result.Comment.Should().Be("Great service!");
    }

    [Test]
    public async Task CreateRating_NotificationPublisherFails_RatingStillSucceeds()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = 123,
            CustomerId = 1001,
            Score = 5,
            Comment = "Should work even if notification fails"
        };

        _mockPublisher.Setup(x => x.PublishRatingNotificationAsync(It.IsAny<RatingResponse>()))
            .ThrowsAsync(new HttpRequestException("Notification service is down"));

        // Act
        var result = await _service.CreateRatingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().Be(5);
        
        var savedRating = await _context.Ratings.FindAsync(result.Id);
        savedRating.Should().NotBeNull();
    }

    [Test]
    public async Task CreateRating_MultipleRatings_AllSavedSuccessfully()
    {
        // Arrange
        var requests = new[]
        {
            new CreateRatingRequest { ServiceProviderId = 100, CustomerId = 1, Score = 5 },
            new CreateRatingRequest { ServiceProviderId = 100, CustomerId = 2, Score = 4 },
            new CreateRatingRequest { ServiceProviderId = 100, CustomerId = 3, Score = 3 }
        };

        // Act
        foreach (var request in requests)
        {
            await _service.CreateRatingAsync(request);
        }

        // Assert
        var savedRatings = await _context.Ratings.Where(r => r.ServiceProviderId == 100).ToListAsync();
        savedRatings.Should().HaveCount(3);
    }

    // ===== GetAverageRating Tests =====

    [Test]
    public async Task GetAverageRating_ExistingRatings_ReturnsCorrectAverage()
    {
        // Arrange
        var serviceProviderId = 100;
        
        _context.Ratings.AddRange(
            new Rating { ServiceProviderId = serviceProviderId, Score = 5, CustomerId = 1, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = serviceProviderId, Score = 4, CustomerId = 2, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = serviceProviderId, Score = 3, CustomerId = 3, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().NotBeNull();
        result!.ServiceProviderId.Should().Be(serviceProviderId);
        result.AverageRating.Should().Be(4.0);
        result.TotalRatings.Should().Be(3);
    }

    [Test]
    public async Task GetAverageRating_NoRatingsExist_ReturnsNull()
    {
        // Arrange
        var serviceProviderId = 999;

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAverageRating_RoundsToTwoDecimals()
    {
        // Arrange
        var serviceProviderId = 200;
        
        _context.Ratings.AddRange(
            new Rating { ServiceProviderId = serviceProviderId, Score = 5, CustomerId = 1, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = serviceProviderId, Score = 4, CustomerId = 2, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = serviceProviderId, Score = 4, CustomerId = 3, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().NotBeNull();
        result!.AverageRating.Should().Be(4.33);
    }

    [Test]
    public async Task GetAverageRating_SingleRating_ReturnsCorrectAverage()
    {
        // Arrange
        var serviceProviderId = 300;
        
        _context.Ratings.Add(
            new Rating { ServiceProviderId = serviceProviderId, Score = 5, CustomerId = 1, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().NotBeNull();
        result!.AverageRating.Should().Be(5.0);
        result.TotalRatings.Should().Be(1);
    }

    [Test]
    public async Task GetAverageRating_ReturnsLastRatedAt()
    {
        // Arrange
        var serviceProviderId = 400;
        var oldDate = DateTime.UtcNow.AddDays(-5);
        var recentDate = DateTime.UtcNow;
        
        _context.Ratings.AddRange(
            new Rating { ServiceProviderId = serviceProviderId, Score = 5, CustomerId = 1, CreatedAt = oldDate },
            new Rating { ServiceProviderId = serviceProviderId, Score = 4, CustomerId = 2, CreatedAt = recentDate }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().NotBeNull();
        result!.LastRatedAt.Should().BeCloseTo(recentDate, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task GetAverageRating_MultipleProviders_ReturnsOnlyRequestedProvider()
    {
        // Arrange
        _context.Ratings.AddRange(
            new Rating { ServiceProviderId = 100, Score = 5, CustomerId = 1, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = 200, Score = 3, CustomerId = 2, CreatedAt = DateTime.UtcNow },
            new Rating { ServiceProviderId = 100, Score = 4, CustomerId = 3, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(100);

        // Assert
        result.Should().NotBeNull();
        result!.ServiceProviderId.Should().Be(100);
        result.TotalRatings.Should().Be(2);
        result.AverageRating.Should().Be(4.5);
    }

    [Test]
    public async Task GetAverageRating_ManyRatings_CalculatesCorrectly()
    {
        // Arrange
        var serviceProviderId = 500;
        var ratings = new List<Rating>();
        
        for (int i = 1; i <= 100; i++)
        {
            ratings.Add(new Rating 
            { 
                ServiceProviderId = serviceProviderId, 
                Score = (i % 5) + 1,
                CustomerId = i, 
                CreatedAt = DateTime.UtcNow 
            });
        }
        
        _context.Ratings.AddRange(ratings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAverageRatingAsync(serviceProviderId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalRatings.Should().Be(100);
        result.AverageRating.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(5);
    }
}