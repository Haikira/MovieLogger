# MovieLogger Development Guidelines

## Technology
- .NET 10
- C#
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- xUnit
- NSubstitute

## Architecture
- MovieLogger.Api contains API controllers and HTTP/API concerns
- MovieLogger.Service contains business logic and application services
- MovieLogger.DAL contains database access and Entity Framework Core concerns
- MovieLogger.Service.Tests contains unit tests for the service layer
- Use dependency injection throughout the application
- Keep controllers thin; business logic belongs in the service layer
- Keep database access within the DAL

## Coding Standards
- Use async/await for I/O-bound operations
- Enable nullable reference types
- Prefer clear, readable C#
 over unnecessary abstraction
- Use DTOs for API contracts
- Validate incoming requests
- Do not expose EF entities directly from API endpoints

## Testing
- New business logic should have unit tests
- API behaviour should have integration tests where appropriate
- Run the test suite before considering a feature complete

## Git
- Work on feature branches
- Use meaningful commit messages
- Do not modify unrelated files

## API
- Use RESTful conventions for endpoints
- Use DTOs for API request and response models
- Do not expose EF Core entities directly from API endpoints
- Use appropriate HTTP status codes
- Validate incoming requests
- Keep controllers focused on HTTP concerns

## Database
- Use Entity Framework Core for database access
- Use SQL Server as the database provider
- Keep EF Core configuration and database access within MovieLogger.DAL
- Use async operations for database I/O
- Avoid unnecessary tracking for read-only queries
- Keep database-specific concerns out of the API layer