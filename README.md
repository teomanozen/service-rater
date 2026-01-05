# ServiceRater

A microservices project where customers rate service providers and providers get notified.

## Version

**v1.0.0** - Basic HTTP communication between services

## Quick Start
```bash
# Clone and setup
git clone <repo-url>
cd ServiceRater

# Start database
docker-compose up -d

# Apply migrations
cd RatingService
dotnet ef database update

# Run services (in separate terminals)
cd RatingService
dotnet run

cd NotificationService  
dotnet run
```

Services run at:
- Rating Service: http://localhost:5082
- Notification Service: http://localhost:5014

## What It Does

Two services talking over HTTP:
- **RatingService** - Customers submit ratings, stored in PostgreSQL
- **NotificationService** - Service providers poll for new rating notifications

The cool part: ratings get saved even if notifications fail (fault tolerance).

## Quick Test
```bash
# Submit a rating
curl -X POST "http://localhost:5082/api/ratings" \
  -H "Content-Type: application/json" \
  -d '{"serviceProviderId": 123, "customerId": 1001, "score": 5, "comment": "Great!"}'

# Check notifications
curl "http://localhost:5014/api/notifications?serviceProviderId=123"

# Get average rating  
curl "http://localhost:5082/api/ratings/average?serviceProviderId=123"
```

## Tech Stack

.NET 8, PostgreSQL 16, Docker, NUnit for tests

## Project Structure
```
ServiceRater/
├── RatingService/           # Handles ratings
├── NotificationService/     # Handles notifications  
├── RatingService.Tests/     # Tests 
├── NotificationService.Tests/  # Tests 
└── docker-compose.yml       # PostgreSQL container
```

## Running Tests
```bash
dotnet test
```

## Key Features

- Ratings stored in PostgreSQL with proper indexes
- Notifications stored in-memory for fast polling
- Once-only delivery (notifications consumed after reading)
- Thread-safe concurrent operations
- RESTful endpoints with Swagger docs

## API Docs

Check Swagger UI when services are running:
- http://localhost:5082/swagger
- http://localhost:5014/swagger

## What's Next (v2.0.0)

Planning to add RabbitMQ for async messaging instead of HTTP. Will make it more scalable and decouple the services better.

## Requirements

- .NET 8 SDK
- Docker

## Notes

Right now services run locally, not containerized. Database is in Docker though. Will containerize everything in v2.

See DOCUMENTATION.md for more details.