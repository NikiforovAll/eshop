# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

eShop is a reference .NET application implementing an e-commerce website using a **services-based architecture** with **.NET Aspire**. It demonstrates best practices for building cloud-native, microservices-based applications with .NET 9.

## Architecture

### Services Architecture
The application follows a **distributed microservices architecture** orchestrated by .NET Aspire:

**Core Services:**
- **Catalog.API** - Product catalog management (PostgreSQL with pgvector)
- **Basket.API** - Shopping cart (Redis, gRPC service)
- **Ordering.API** - Order processing (PostgreSQL, DDD patterns)
- **Identity.API** - Authentication and authorization (IdentityServer)
- **Webhooks.API** - Webhook subscriptions and notifications

**Background Processors:**
- **OrderProcessor** - Processes order events asynchronously
- **PaymentProcessor** - Handles payment processing events

**Frontend Apps:**
- **WebApp** - Main e-commerce web application (Blazor)
- **WebhookClient** - Webhook testing client

**Infrastructure:**
- **eShop.AppHost** - .NET Aspire orchestration host (entry point)
- **eShop.ServiceDefaults** - Shared service configuration (OpenTelemetry, health checks, authentication)
- **EventBus / EventBusRabbitMQ** - Event-driven communication via RabbitMQ
- **IntegrationEventLogEF** - Outbox pattern for reliable event publishing

### Key Architectural Patterns

**Domain-Driven Design (DDD)** - Ordering service implements:
- Aggregate roots (e.g., `Order` in `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`)
- Value objects (e.g., `Address`)
- Domain events
- Repository pattern
- Encapsulated collections with behavior

**Event-Driven Architecture:**
- Services communicate via `IntegrationEvent` messages published through `IEventBus`
- RabbitMQ for asynchronous messaging
- Outbox pattern for transactional message publishing

**Service Defaults Pattern:**
- All services call `.AddServiceDefaults()` to configure:
  - OpenTelemetry (logging, metrics, tracing)
  - Health checks (`/health`, `/alive` endpoints)
  - Service discovery
  - HTTP resilience (via Polly)
  - Authentication

**gRPC Communication:**
- Basket.API exposes gRPC services (see `src/Basket.API/Proto/basket.proto`)
- Used for high-performance inter-service calls

## Building and Running

### Prerequisites
- Docker Desktop (must be running)
- .NET 9 SDK
- Visual Studio 2022 17.10+ (Windows) OR Visual Studio Code with C# Dev Kit

### Run Locally

**Primary method:**
```bash
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```
Then navigate to the Aspire dashboard URL shown in the console output (e.g., `http://localhost:19888/login?t=...`).

**Visual Studio (Windows):**
- Open `eShop.Web.slnf`
- Set `eShop.AppHost` as startup project
- Press Ctrl+F5

**IMPORTANT:** The Aspire AppHost (`src/eShop.AppHost/Program.cs`) is the orchestration entry point that:
- Spins up infrastructure (Redis, RabbitMQ, PostgreSQL with pgvector)
- Configures service dependencies and environment variables
- Sets up service discovery and health checks
- Manages launch profiles (http/https)

### Testing

**Run all tests:**
```bash
dotnet test
```

**Run specific test project:**
```bash
dotnet test tests/Basket.UnitTests/Basket.UnitTests.csproj
dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
dotnet test tests/Ordering.UnitTests/Ordering.UnitTests.csproj
dotnet test tests/Ordering.FunctionalTests/Ordering.FunctionalTests.csproj
```

**IMPORTANT:** Functional tests leverage the Aspire host to spin up test containers and **require Docker to be running**.

**Run a single test:**
```bash
dotnet test --filter FullyQualifiedName=Namespace.ClassName.MethodName
```

### Build

**Build entire solution:**
```bash
dotnet build eShop.Web.slnf
```

**Build specific project:**
```bash
dotnet build src/Catalog.API/Catalog.API.csproj
```

## Configuration

### AI Integration (Optional)
The application supports Azure OpenAI and Ollama for AI features:

**Azure OpenAI:**
1. Add connection string to `src/eShop.AppHost/appsettings.json`:
```json
"ConnectionStrings": {
  "OpenAi": "Endpoint=xxx;Key=xxx;"
}
```
2. Set `useOpenAI = true` in `src/eShop.AppHost/Program.cs:78`

**Ollama:**
- Set `useOllama = true` in `src/eShop.AppHost/Program.cs:84`

### Service Communication
Services discover each other via .NET Aspire service discovery. The AppHost configures:
- Identity URLs for authentication
- Database connections
- Event bus (RabbitMQ) references
- Callback URLs for OAuth flows

## Project Structure

**Services are self-contained** - each service directory contains:
- API controllers/endpoints
- Application logic
- Extensions for DI configuration
- Proto files (if gRPC)

**Shared code:**
- `src/Shared/` - Cross-cutting utilities (ActivityExtensions, MigrateDbContextExtensions)
- `src/eShop.ServiceDefaults/` - Service configuration standards
- `src/EventBus/` - Event bus abstractions
- `src/EventBusRabbitMQ/` - RabbitMQ implementation

**Domain separation:**
- Ordering service separates Domain (`Ordering.Domain`), API (`Ordering.API`), and Infrastructure (`Ordering.Infrastructure`)
- Other services use simpler structures appropriate to their complexity

## Development Practices

**Service Defaults:** When adding a new service, call `.AddServiceDefaults()` or `.AddBasicServiceDefaults()` to get standard observability and resilience.

**Database Migrations:** Use EF Core migrations for schema changes. The AppHost uses `.WaitFor()` to ensure databases are ready before dependent services start.

**Health Checks:** Services should tag health checks with `"live"` for liveness probes.

**AOT Compatibility:** EventBus and some libraries target AOT compatibility (`<IsAotCompatible>true</IsAotCompatible>`).

**HTTP vs HTTPS:** By default uses HTTPS. Set `ESHOP_USE_HTTP_ENDPOINTS=1` environment variable to force HTTP (used in CI for Playwright tests).

## Azure Deployment

Use Azure Developer CLI (`azd`):
```bash
azd auth login
azd init
azd up
```

The `azd` tool automatically detects the .NET Aspire project and provisions Azure resources.

## Solution Files

- `eShop.Web.slnf` - Solution filter for core web services (recommended for most development)
- `eShop.slnx` - Full solution including all projects

## Sample Data

Catalog data is seeded from `src/Catalog.API/Setup/catalog.json`. Product names, descriptions, and images are AI-generated (GPT-3.5-Turbo and DALL-E 3).
- CLAUDE CODE IS AWESOME