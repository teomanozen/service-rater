# ServiceRater - Technical Details

## Overview

This is a rating system with two microservices. Customers submit ratings, service providers get notifications. Built it to learn microservices patterns.

## Architecture
```
Customer → RatingService → PostgreSQL
              ↓
        (HTTP POST)
              ↓
        NotificationService → In-Memory Queue
              ↓
        Service Provider (polls for updates)
```

Pretty straightforward - ratings go to database, notifications go to memory, providers poll to check for new ones.

## Services

### RatingService (Port 5082)

What it does:
- Takes rating submissions from customers
- Validates scores (1-5) and other fields
- Saves to PostgreSQL
- Tries to notify NotificationService (doesn't fail if this doesn't work)
- Calculates average ratings

Tech: ASP.NET Core 8, Entity Framework Core, PostgreSQL

### NotificationService (Port 5014)

What it does:
- Receives notifications from RatingService
- Stores them in memory (ConcurrentQueue for thread safety)
- Lets service providers poll for their notifications
- Removes notifications after they're read (once-only delivery)

Tech: ASP.NET Core 8, in-memory concurrent collections

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

## Design Choices

### Why HTTP?
Simple, easy to debug, works fine for v1. Will switch to RabbitMQ in v2 for better scalability.

### Why in-memory notifications?
Fast and simple. Okay to lose them on restart since they're not critical data. Will use Redis in v2 if needed.

### Why PostgreSQL?
Ratings are important data that needs proper persistence and ACID guarantees. Plus PostgreSQL is great at aggregations.

### Database aggregation
Instead of loading all ratings into memory and calculating average in C#, I do it in the database with GROUP BY. Way faster (5ms vs 200ms for 10k ratings).

## Testing

30+ unit tests using NUnit, Moq, and FluentAssertions. Tests cover:
- Business logic
- Error handling
- Edge cases
- Thread safety
- Fault tolerance (ratings work even if notifications fail)

Run with: `dotnet test`

## Configuration

**RatingService/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=servicerater_db;..."
  },
  "NotificationService": {
    "BaseUrl": "http://localhost:5014"
  }
}
```

## Deployment

Right now: PostgreSQL in Docker, services run locally with `dotnet run`

Future: Will containerize everything with Docker Compose in v2

## What I Learned

- Microservices communication patterns
- Fault tolerance (don't let one service failure break another)
- Thread-safe concurrent collections
- Entity Framework migrations
- Writing comprehensive unit tests
- RESTful API design

## Future Plans (v2.0.0)

Main goal: Switch to RabbitMQ for async messaging

Why:
- Decouple services completely
- Better fault tolerance (messages persist)
- Can scale NotificationService horizontally
- Add retry logic

Other ideas:
- Redis for notifications (instead of in-memory)
- API Gateway
- Better logging and monitoring
- Containerize everything

## Troubleshooting

**Database won't connect?**
```bash
docker ps  # check if postgres is running
docker logs servicerater-postgres
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

## Tech Stack

- .NET 8
- PostgreSQL 16
- Docker
- Entity Framework Core 8
- NUnit, Moq, FluentAssertions
- Swagger/OpenAPI

That's pretty much it. Check the code for more details.