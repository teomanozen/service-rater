# ServiceRater

A microservices-based rating and notification system built with .NET 8, demonstrating asynchronous communication, persistent storage, and containerized deployment.

## Overview

ServiceRater allows customers to rate service providers and delivers real-time notifications through a distributed architecture using message queues and persistent storage.

## Architecture

### Services

- **RatingService** - Handles rating submissions and calculates averages
- **NotificationService** - Consumes rating events and manages notifications

### Infrastructure

- **PostgreSQL** - Persistent storage for ratings
- **RabbitMQ** - Asynchronous message broker
- **Redis** - Persistent notification queue with automatic expiration

### Communication Flow
```
Customer → RatingService → PostgreSQL (rating stored)
                        → RabbitMQ (async message)
                        → NotificationService → Redis (notification stored)
                        → Service Provider (polling)
```

## Technology Stack

**Backend:** .NET 8, C# 12, ASP.NET Core Web API  
**Data Layer:** PostgreSQL 16, Redis 7, Entity Framework Core  
**Messaging:** RabbitMQ 3.13  
**Containerization:** Docker, Docker Compose  
**Testing:** NUnit, Moq, FluentAssertions (75+ unit tests)

## Key Features

- **Asynchronous Communication** - RabbitMQ with manual acknowledgment and message persistence
- **Persistent Storage** - Redis with AOF persistence and automatic TTL (7 days)
- **Horizontal Scaling** - Stateless services with shared Redis storage
- **Fault Tolerance** - Automatic database migrations, message retry, and graceful error handling
- **Clean Architecture** - Interface-based design, dependency injection, SOLID principles
- **API Versioning** - Semantic versioning with backward compatibility

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (for local development)

### Running with Docker (Recommended)
```bash
# Clone repository
git clone <repository-url>
cd ServiceRater

# Create environment file
cp .env.example .env

# Start all services
docker-compose up -d

# Database migrations run automatically on startup

# Access services
# RatingService: http://localhost:5082/swagger
# NotificationService: http://localhost:5014/swagger
# RabbitMQ Management: http://localhost:15672 (servicerater / dev_password_123)
```

### Example Usage
```bash
# Submit a rating
curl -X POST "http://localhost:5082/api/ratings" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceProviderId": 1,
    "customerId": 1,
    "score": 5,
    "comment": "Excellent service!"
  }'

# Poll notifications (service provider)
curl "http://localhost:5014/api/notifications?serviceProviderId=1&limit=10"

# Get average rating
curl "http://localhost:5082/api/ratings/average?serviceProviderId=1"
```

## Development

### Local Development (without Docker)
```bash
# Start infrastructure only
docker-compose up -d postgres rabbitmq redis

# Run services locally
cd RatingService
dotnet run

# In another terminal
cd NotificationService
dotnet run
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## API Documentation

Interactive API documentation available via Swagger UI when services are running:
- **RatingService:** http://localhost:5082/swagger
- **NotificationService:** http://localhost:5014/swagger

## Monitoring Tools

- **RabbitMQ Management UI:** http://localhost:15672
- **RedisInsight:** Connect to `localhost:6379`
- **PostgreSQL:** Any PostgreSQL client on `localhost:5432`

## Configuration

Configuration follows 12-Factor App principles:
- **Development:** Uses `appsettings.json` with localhost connections
- **Docker:** Environment variables override configuration
- **Production:** Set environment variables for all sensitive values

## Project Structure
```
ServiceRater/
├── RatingService/          # Rating submission and retrieval
├── NotificationService/    # Notification management
├── RatingService.Tests/    # Unit tests
├── NotificationService.Tests/
├── docker-compose.yml      # Multi-container orchestration
└── .env.example           # Configuration template
```

## Version History

- **v1.2.0** - Redis persistent storage, full containerization
- **v1.1.0** - RabbitMQ async messaging
- **v1.0.0** - Initial release with HTTP sync communication

## Design Decisions

**Why RabbitMQ?** Decouples services, enables fault tolerance, supports high throughput  
**Why Redis?** Sub-millisecond performance, built-in TTL, horizontal scaling support  
**Why PostgreSQL?** ACID guarantees, complex queries, production-ready

## Production Considerations

Current implementation includes:
- ✅ Automatic database migrations
- ✅ Message persistence and acknowledgment
- ✅ Data persistence across restarts
- ✅ Health checks for infrastructure
- ✅ Structured logging

For production deployment, consider adding:
- Authentication and authorization (JWT)
- Rate limiting
- Distributed tracing (OpenTelemetry)
- Monitoring and alerting
- CI/CD pipeline

## License

This is a portfolio/demonstration project.

## Author

Built as a demonstration of microservices architecture, asynchronous communication patterns, and distributed systems design.