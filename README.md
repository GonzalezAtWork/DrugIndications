# Drug Indications Microservice

A microservice-based application that extracts drug indications from DailyMed drug labels, maps them to ICD-10 codes, and provides a queryable API.

## Overview

This application extracts drug indications from DailyMed drug labels, maps them to standardized ICD-10 codes, and processes copay card information. It follows clean architecture principles with a clear separation of concerns between domain, application, infrastructure, and presentation layers.

## Features

- Extract drug indications from DailyMed drug labels
- Map indications to ICD-10 codes
- Parse and structure copay card information
- Provide a REST API for querying drug indications and copay programs
- Authentication and authorization for secure access
- Comprehensive test coverage

## Tech Stack

- .NET 8.0
- C# programming language
- Microsoft SQL Server for data storage
- Docker for containerization
- JWT for authentication
- OpenAI integration for parsing eligibility details

## Project Structure

The project follows clean architecture principles:

- `API/` - **Presentation Layer**: API controllers and endpoints
- `Application/` - **Application Layer**: Use cases and business logic
- `Domain/` - **Domain Layer**: Core business entities and rules
- `Infrastructure/` - **Infrastructure Layer**: External dependencies (database, APIs)
- `Tests/` - **Test Layer**:Unit tests and test utilities

## Setup and Installation

### Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for development only)

1. Clone the repository:
```bash
git clone https://github.com/GonzalezAtWork/DrugIndications.git
cd DrugIndications
```

2. Set up environment variables:
```bash
cp .env.example .env
# Edit .env with your configuration
```

3. Run with Docker Compose:
```bash
docker-compose up -d
```

4. Run locally:
```bash
dotnet restore
dotnet build
dotnet run --project API/DrugIndications.API.csproj
```

## API Documentation

Once the application is running, you can access the Swagger documentation at:
- https://localhost:5001/swagger (when running locally)
- http://localhost:8080/swagger (when running with Docker)

## Testing

Run the tests:
```bash
dotnet test
```

## Scalability Considerations

- Microservice architecture allows for independent scaling of components
- Containerization enables easy deployment and scaling
- Database indexing for optimized queries
- Caching for frequently accessed data
- Rate limiting to prevent API abuse
- Asynchronous processing for long-running tasks

## Potential Improvements

- Implement a more sophisticated NLP model for extracting indications
- Add more comprehensive test coverage
- Implement a message queue for asynchronous processing
- Add monitoring and logging infrastructure
- Implement a circuit breaker pattern for external API calls
- Add support for more drug databases beyond DailyMed

## Production Challenges

- Ensuring high availability and reliability
- Managing API rate limits for external services
- Handling large volumes of data efficiently
- Ensuring data accuracy and validation
- Keeping ICD-10 mappings up-to-date
- Securing sensitive patient and drug information
