# ServiceRater

Microservices-based rating system with asynchronous communication. Customers submit ratings for service providers, who receive real-time notifications via message queue.

## Version

**v2.0.0** - RabbitMQ async communication

## Quick Start
```bash
git clone <repository-url>
cd ServiceRater

# Start infrastructure (PostgreSQL + RabbitMQ)
docker-compose up -d

# Apply database migrations
cd RatingService
dotnet ef database update

# Start services (in separate terminals)
cd RatingService
dotnet run

cd NotificationService
dotnet run
```

**Services available at:**
- **Rating Service:** http://localhost:5082
- **Notification Service:** http://localhost:5014
- **Rating Service Swagger:** http://localhost:5082/swagger
- **Notification Service Swagger:** http://localhost:5014/swagger
- **RabbitMQ Management UI:** http://localhost:15672 (servicerater / dev_password_123)

## What's Built

**Two microservices communicating via RabbitMQ message broker:**
- **RatingService** - Handles rating submissions, stores in PostgreSQL, publishes notifications
- **NotificationService** - Consumes notifications from queue, stores in-memory, provides polling API

**Tech Stack:** .NET 8, PostgreSQL 16, RabbitMQ 3, Docker, NUnit + Moq + FluentAssertions

## Architecture Highlights

### v2.0.0 - Async Communication with RabbitMQ
```
Customer → RatingService → PostgreSQL (ratings stored)
                ↓
         RabbitMQ Queue (async notification)
                ↓
         NotificationService → In-Memory Store
                ↓
         Service Provider (polls for notifications)
```

**Key improvements over v1.0.0:**
- ✅ **Async messaging** - Rating creation doesn't wait for notification delivery
- ✅ **Decoupled services** - Services can scale independently
- ✅ **Fault tolerant** - Messages persist in queue if consumer is down
- ✅ **Message durability** - Notifications survive RabbitMQ restart
- ✅ **Manual acknowledgment** - No message loss on processing failures

## Quick Test
```bash
# 1. Submit a rating
curl -X POST "http://localhost:5082/api/ratings" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceProviderId": 123,
    "customerId": 1001,
    "score": 5,
    "comment": "Excellent service!"
  }'

# 2. Check RabbitMQ UI
# Open http://localhost:15672
# See message published and consumed in "rating-notifications" queue

# 3. Poll notifications (service provider)
curl "http://localhost:5014/api/notifications?serviceProviderId=123&limit=10"

# 4. Get average rating
curl "http://localhost:5082/api/ratings/average?serviceProviderId=123"
```

## Project Structure
```
ServiceRater/
├── RatingService/                 # Rating management service
│   ├── Controllers/               # API endpoints
│   ├── Services/                  # Business logic
│   │   ├── RatingServiceImpl.cs
│   │   └── RabbitMQNotificationPublisher.cs  # v2.0.0
│   ├── Data/                      # EF Core DbContext
│   └── Models/                    # Database entities
├── RatingService.Tests/           # Unit tests (45+ tests)
├── NotificationService/           # Notification delivery service
│   ├── Controllers/               # API endpoints
│   ├── Services/                  # Business logic
│   │   ├── NotificationServiceImpl.cs
│   │   └── RabbitMQConsumerService.cs  # v2.0.0 Background service
│   └── Storage/                   # In-memory notification storage
├── NotificationService.Tests/     # Unit tests (30+ tests)
└── docker-compose.yml             # PostgreSQL + RabbitMQ
```

## Running Tests
```bash
# Run all tests
dotnet test

# Run specific service tests
dotnet test RatingService.Tests
dotnet test NotificationService.Tests

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

**Test Coverage:** 75+ unit tests covering business logic, error handling, edge cases, and fault tolerance.

## API Documentation

Both services include Swagger UI:
- Rating Service: http://localhost:5082/swagger
- Notification Service: http://localhost:5014/swagger

### Key Endpoints

**Rating Service:**
- `POST /api/ratings` - Submit new rating
- `GET /api/ratings/average?serviceProviderId={id}` - Get average rating

**Notification Service:**
- `GET /api/notifications?serviceProviderId={id}&limit={n}` - Poll notifications (consumed once)
- `GET /api/notifications/count?serviceProviderId={id}` - Get pending count

See `DOCUMENTATION.md` for detailed API specifications.

## Configuration

### RatingService/appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=servicerater_db;..."
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "servicerater",
    "Password": "dev_password_123",
    "QueueName": "rating-notifications"
  }
}
```

### NotificationService/appsettings.json
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "servicerater",
    "Password": "dev_password_123",
    "QueueName": "rating-notifications"
  }
}
```

## Design Decisions

### Why RabbitMQ for Notifications?

**Benefits over HTTP (v1.0.0):**
- **Async** - RatingService doesn't wait for notification delivery (faster response)
- **Decoupled** - Services can restart/scale independently
- **Reliable** - Messages persist if consumer is down
- **Scalable** - Can add multiple NotificationService instances

### Why In-Memory Storage for Notifications?

**For this portfolio project**, notifications use in-memory storage because:
- Fast performance for temporary data
- Simple implementation
- Demonstrates clean architecture (easy to swap via `INotificationStore`)

**Production consideration:** Would use **Redis** for:
- Persistence across restarts
- Horizontal scaling (shared storage)
- Built-in TTL for auto-cleanup

The interface-based design allows swapping to Redis without changing business logic.

### Why PostgreSQL for Ratings?

- ACID guarantees for critical data
- Complex aggregations (averages, counts)
- Permanent storage required
- Excellent query performance with proper indexes

## Fault Tolerance

**Rating creation succeeds even if:**
- ✅ RabbitMQ is down (exception caught, rating still saved)
- ✅ NotificationService is down (message waits in queue)
- ✅ Network fails (automatic requeue on NACK)

**Message delivery guarantees:**
- Durable queues (survive RabbitMQ restart)
- Persistent messages (written to disk)
- Manual acknowledgment (only deleted after successful processing)
- Automatic requeue on failure

## Version History

### v2.0.0 (Current) - Async Communication
- Replace HTTP with RabbitMQ async messaging
- Background consumer service
- Message persistence and acknowledgment
- Improved fault tolerance and scalability

### v1.0.0 - HTTP Sync Communication
- Initial release with HTTP-based communication
- RESTful APIs
- PostgreSQL for ratings
- In-memory notification storage

## Development

### Database Management
```bash
# Connect to PostgreSQL
docker exec -it servicerater-postgres psql -U servicerater -d servicerater_db

# Clear data
DELETE FROM ratings;

# Exit
\q
```

### RabbitMQ Management
```bash
# Access UI
open http://localhost:15672

# View queues, exchanges, connections
# Monitor message rates
# Debug message flow
```

## Roadmap

**Future improvements (v3.0.0+):**
- Redis for persistent notification storage
- Authentication & authorization (JWT)
- API Gateway (Ocelot/YARP)
- Monitoring & observability (OpenTelemetry)
- Dead Letter Queue for failed messages
- Rate limiting
- Health checks

## Requirements

- .NET 8 SDK
- Docker & Docker Compose
- 4GB RAM minimum
