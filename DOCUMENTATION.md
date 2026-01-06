# ServiceRater - Technical Details

## Overview

This is a rating system with two microservices. Customers submit ratings, service providers get notifications. Built it to learn microservices patterns.

## Version History

### v2.0.0 (2026-01-06) - Current
**Major architectural change: HTTP → RabbitMQ async messaging**

- Replaced synchronous HTTP communication with asynchronous message broker
- Added RabbitMQ for decoupled, fault-tolerant service communication
- Implemented background consumer service (BackgroundService)
- Message persistence with manual acknowledgment
- Improved system scalability and reliability

**Breaking Changes:**
- Requires RabbitMQ server
- Internal HTTP endpoint removed
- Configuration changes required

### v1.0.0 (2026-01-05)
- Initial release with HTTP synchronous communication
- RESTful APIs with proper error handling
- PostgreSQL for persistent rating storage
- In-memory notification storage
- Comprehensive unit tests

---

## Architecture

### System Design (v2.0.0)
```
Customer Application
        ↓ HTTP POST
    Rating Service
        ↓ (saves to DB)
    PostgreSQL
        ↓ Publish to Queue
    RabbitMQ (rating-notifications queue)
        ↓ Background Consumer
    Notification Service
        ↓ (stores in-memory)
    In-Memory Storage
        ↓ HTTP GET (polling)
    Service Provider Application
```

### Communication Flow

1. **Customer submits rating** → Rating Service receives HTTP POST
2. **Rating Service** saves to PostgreSQL (guaranteed, ACID)
3. **Rating Service** publishes notification message to RabbitMQ queue
4. **RabbitMQ** stores message persistently (survives restart)
5. **NotificationService** background consumer picks up message
6. **NotificationService** stores in in-memory queue
7. **Service Provider** polls via HTTP GET
8. **Notification consumed** once and removed from storage

### Key Architectural Principles

- **Microservices** - Independent services, separate concerns
- **Async Messaging** - Decoupled communication via message broker
- **Fault Tolerance** - Rating succeeds even if notification fails
- **Message Durability** - Notifications survive service restarts
- **Once-Only Consumption** - Each notification delivered exactly once
- **Event-Driven** - Background service reacts to queue events
- **Clean Architecture** - Interface-based design, easy to swap implementations

## Services

### RatingService (Port 5082)

What it does:
- Takes rating submissions from customers
- Validates scores (1-5) and other fields
- Saves to PostgreSQL
- Publishes notifications to RabbitMQ (fire-and-forget)
- Calculates average ratings

Tech: ASP.NET Core 8, Entity Framework Core, PostgreSQL, RabbitMQ.Client

### NotificationService (Port 5014)

What it does:
- Background consumer listens to RabbitMQ queue
- Stores notifications in memory (ConcurrentQueue for thread safety)
- Lets service providers poll for their notifications
- Removes notifications after they're read (once-only delivery)

Tech: ASP.NET Core 8, in-memory concurrent collections, RabbitMQ.Client

## Database

PostgreSQL table for ratings:
```sql
CREATE TABLE ratings (
    id SERIAL PRIMARY KEY,
    service_provider_id INTEGER NOT NULL,
    customer_id INTEGER NOT NULL,
    score INTEGER CHECK (score >= 1 AND score <= 5),
    comment TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_ratings_service_provider_id ON ratings(service_provider_id);
```

Simple schema, one index for fast queries.

## API Endpoints

### Rating Service

**Submit a rating:**
```http
POST /api/ratings
{
  "serviceProviderId": 123,
  "customerId": 1001,
  "score": 5,
  "comment": "Excellent!"
}
```

**Get average:**
```http
GET /api/ratings/average?serviceProviderId=123
```

### Notification Service

**Poll notifications:**
```http
GET /api/notifications?serviceProviderId=123&limit=10
```

**Check count:**
```http
GET /api/notifications/count?serviceProviderId=123
```

## Configuration

**RatingService/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=servicerater_db;Username=servicerater;Password=dev_password_123"
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

**NotificationService/appsettings.json:**
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

## RabbitMQ Configuration

### Queue Declaration

Both services declare the queue on startup (idempotent operation):
```csharp
_channel.QueueDeclareAsync(
    queue: "rating-notifications",
    durable: true,              // Queue survives RabbitMQ restart
    exclusive: false,           // Multiple connections can access
    autoDelete: false,          // Don't delete when no consumers
    arguments: null
);
```

### Publisher Configuration (RatingService)
```csharp
var properties = new BasicProperties
{
    Persistent = true  // Messages survive RabbitMQ restart
};

_channel.BasicPublishAsync(
    exchange: "",                    // Default exchange (direct routing)
    routingKey: "rating-notifications",
    mandatory: false,
    basicProperties: properties,
    body: messageBytes
);
```

### Consumer Configuration (NotificationService)
```csharp
// Quality of Service: Process one message at a time
_channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

// Manual acknowledgment (autoAck = false)
_channel.BasicConsumeAsync(
    queue: "rating-notifications",
    autoAck: false,              // We control acknowledgment
    consumer: consumer
);
```

### Message Acknowledgment Strategy

**Success:**
```csharp
await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
// Message deleted from queue
```

**Deserialization failure (poison message):**
```csharp
await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
// Message discarded (sent to DLQ if configured, otherwise deleted)
```

**Processing exception (temporary error):**
```csharp
await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
// Message returned to queue for retry
```

## Design Decisions

### Why Async Messaging with RabbitMQ? (v2.0.0)

**Replaced HTTP synchronous communication for several reasons:**

**Benefits:**
- ✅ **Decoupling** - Services don't need to know each other's locations
- ✅ **Fault Tolerance** - Messages persist if consumer is down
- ✅ **Scalability** - Can add multiple consumer instances
- ✅ **Performance** - Non-blocking, fire-and-forget
- ✅ **Reliability** - Message durability + manual acknowledgment = no data loss

**Trade-offs:**
- ⚠️ Additional infrastructure (RabbitMQ server)
- ⚠️ Eventual consistency (notification not immediate)
- ⚠️ More complexity (message broker management)

**Decision:** Benefits outweigh trade-offs for this use case. Notifications don't need to be instant, and improved reliability/scalability are valuable.

### Why In-Memory Storage for Notifications?

Fast and simple. Okay to lose them on restart since they're not critical data.

**Production consideration:** Would use **Redis** for:
- Persistence across restarts
- Horizontal scaling (shared storage)
- Built-in TTL for auto-cleanup

The interface-based design (`INotificationStore`) allows swapping to Redis without changing business logic.

### Why PostgreSQL for Ratings?

Ratings are important data that needs proper persistence and ACID guarantees. Plus PostgreSQL is great at aggregations.

### Database Aggregation

Instead of loading all ratings into memory and calculating average in C#, I do it in the database with GROUP BY. Way faster (5ms vs 200ms for 10k ratings).

### Message Durability Strategy

**Queue durability:**
```
durable: true → Queue definition survives RabbitMQ restart
```

**Message persistence:**
```
Persistent: true → Message content written to disk
```

**Both required for full durability.** Without both:
- Only durable queue → Messages lost on restart
- Only persistent messages → Queue deleted on restart

### Manual vs Auto Acknowledgment

**Manual (our choice):**
```
Process message → If success: ACK → Message deleted
                → If failure: NACK → Message requeued
```

**Auto (rejected):**
```
Receive message → Immediately deleted from queue
If processing fails → Message lost forever
```

**Decision:** Manual acknowledgment for reliability. Acceptable trade-off: slightly more code for guaranteed delivery.

### prefetchCount = 1

**Why process one message at a time?**

- ✅ Fair distribution across multiple consumers
- ✅ Slower consumer doesn't get overloaded
- ✅ Simple to reason about
- ✅ Easy to debug

**Decision:** Simplicity over throughput. Message rate is low (human-generated ratings), so throughput not critical.

## Testing

75+ unit tests using NUnit, Moq, and FluentAssertions:
- RatingService.Tests: ~45 tests
- NotificationService.Tests: ~30 tests

Tests cover:
- Business logic
- Error handling
- Edge cases
- Thread safety
- Fault tolerance (ratings work even if notifications fail)
- Message acknowledgment patterns

Run with: `dotnet test`

## What I Learned

- Microservices architecture and communication patterns
- Async messaging with RabbitMQ (message broker)
- Fault tolerance and resilience patterns
- Background services (BackgroundService, hosted services)
- Message acknowledgment strategies (ACK/NACK)
- Thread-safe concurrent collections
- Entity Framework Core migrations and query optimization
- Unit testing with mocking and dependency injection
- RESTful API design with proper status codes
- Docker containerization for development
- Git workflow with feature branches and semantic versioning

## Future Improvements (Roadmap)

### v3.0.0 - Production Hardening
- **Redis for notifications** - Persistent storage, horizontal scaling
- **Authentication** - JWT tokens, secure endpoints
- **Rate limiting** - Prevent abuse
- **Dead Letter Queue** - Handle poison messages
- **Health checks** - Service monitoring

### Later Versions
- **API Gateway** - Centralized entry point
- **Monitoring** - OpenTelemetry, Prometheus, Grafana
- **CI/CD Pipeline** - Automated testing and deployment
- **Kubernetes** - Container orchestration

## Troubleshooting

**Database won't connect?**
```bash
docker ps  # check if postgres is running
docker logs servicerater-postgres
```

**RabbitMQ issues?**
```bash
docker ps | grep rabbitmq
docker logs servicerater-rabbitmq
# Check UI: http://localhost:15672
```

**Port already in use?**
```bash
lsof -i :5082  # check what's using the port
lsof -i :5014
```

**Tests failing?**
```bash
dotnet clean
dotnet build
dotnet test --logger "console;verbosity=detailed"
```

## Technology Stack

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Language** | C# | 12 | Application code |
| **Framework** | .NET | 8.0 | Backend services |
| **Web Framework** | ASP.NET Core | 8.0 | REST APIs |
| **Database** | PostgreSQL | 16 | Rating persistence |
| **Message Broker** | RabbitMQ | 3.13 | Async communication |
| **ORM** | Entity Framework Core | 8.0 | Database access |
| **Testing** | NUnit | 4.0 | Unit test framework |
| **Mocking** | Moq | 4.20 | Test doubles |
| **Assertions** | FluentAssertions | 6.12 | Readable test assertions |
| **Messaging Client** | RabbitMQ.Client | 6.8 | RabbitMQ integration |
| **Containerization** | Docker & Docker Compose | Latest | Infrastructure |

That's pretty much it. Check the code for more details.