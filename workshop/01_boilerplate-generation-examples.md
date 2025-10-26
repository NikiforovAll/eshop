# Boilerplate Code Generation Examples for eShop

**Context**: These examples demonstrate how to generate complete, production-ready boilerplate code for common microservices patterns in the eShop codebase. Each prompt generates all necessary files, configurations, and registrations for a working feature.

**Category**: Code Generation and Scaffolding
**Sub Category**: Boilerplate Code Generation

---

## Pattern Overview

The eShop application uses several key patterns that require repetitive boilerplate code:
- **Integration Events** with handlers and event bus subscriptions
- **CQRS Commands/Queries** with MediatR handlers and FluentValidation validators
- **DDD Building Blocks** (aggregates, entities, value objects, repositories)
- **API Endpoints** using ASP.NET Core minimal APIs with versioning
- **gRPC Services** with proto definitions and service implementations
- **EF Core Configuration** with entity type configurations
- **Background Services** extending BackgroundService
- **Service Registration** through extension methods

---

### Prompt 1: Generate Complete Integration Event with Handler and Subscriptions

Generate a complete integration event called ProductRestockedIntegrationEvent for the Catalog.API service with the following requirements:

EVENT PROPERTIES:
- ProductId (int) - ID of the restocked product
- ProductName (string) - Name of the product
- NewStockLevel (int) - Updated stock quantity
- RestockedDate (DateTime) - When the restock occurred
- PreviousStockLevel (int) - Stock level before restock

IMPLEMENTATION REQUIREMENTS:
1. Create the event class following the pattern in src\Catalog.API\IntegrationEvents\Events\ProductPriceChangedIntegrationEvent.cs
2. Create event handler in Webhooks.API at src\Webhooks.API\IntegrationEvents\ProductRestockedIntegrationEventHandler.cs following the pattern in ProductPriceChangedIntegrationEventHandler.cs
3. Add handler registration to src\Webhooks.API\Extensions\Extensions.cs in the AddEventBusSubscriptions method
4. The event should be published from Catalog.API when stock is updated through the catalog API endpoints

NAMING CONVENTIONS:
- Event class: ProductRestockedIntegrationEvent (record type)
- Handler class: ProductRestockedIntegrationEventHandler
- Namespace: eShop.Catalog.API.IntegrationEvents.Events for event
- Namespace: Webhooks.API.IntegrationEvents for handler

ADDITIONAL REQUIREMENTS:
- Use record type with primary constructor for the event
- Implement IIntegrationEventHandler<ProductRestockedIntegrationEvent> for handler
- Add XML comments explaining when this event is raised
- Include logging in the handler using ILogger

**Why this works**: Specifies complete event properties with types, identifies exact file locations and patterns to follow, includes all registration requirements, and specifies naming conventions ensuring consistency with existing codebase patterns.

---

### Prompt 2: Generate CQRS Command with Handler and Validator
Generate a complete CQRS command called UpdateProductStockCommand for Catalog.API with the following specifications:

COMMAND PROPERTIES:
- ProductId (int) - Product to update (required)
- NewStockQuantity (int) - New stock level (must be >= 0)
- Reason (string) - Reason for stock change (required, max 500 chars)
- UpdatedBy (string) - User making the change (required)

IMPLEMENTATION FILES:
1. Command: src\Catalog.API\Application\Commands\UpdateProductStockCommand.cs
   - Follow pattern from src\Ordering.API\Application\Commands\CreateOrderCommand.cs
   - Use [DataContract] attribute
   - Implement IRequest<bool> from MediatR
   - Private setters for immutability
   - Constructor with all parameters

2. Handler: src\Catalog.API\Application\Commands\UpdateProductStockCommandHandler.cs
   - Follow pattern from src\Ordering.API\Application\Commands\CreateOrderCommandHandler.cs
   - Inject CatalogContext, ILogger, and ICatalogIntegrationEventService
   - Implement IRequestHandler<UpdateProductStockCommand, bool>
   - Update product stock in database
   - Publish ProductRestockedIntegrationEvent after successful update
   - Return true on success, false on failure

3. Validator: src\Catalog.API\Application\Validations\UpdateProductStockCommandValidator.cs
   - Follow pattern from src\Ordering.API\Application\Validations\CreateOrderCommandValidator.cs
   - Use FluentValidation AbstractValidator<UpdateProductStockCommand>
   - Rules: ProductId > 0, NewStockQuantity >= 0, Reason NotEmpty and MaxLength(500), UpdatedBy NotEmpty

REGISTRATION REQUIREMENTS:
- Add validator registration to service configuration in Extensions.cs
- Register as: services.AddSingleton<IValidator<UpdateProductStockCommand>, UpdateProductStockCommandValidator>()
- MediatR will auto-discover the handler

ADDITIONAL REQUIREMENTS:
- Add detailed logging statements in handler
- Include proper error handling with try-catch
- Validate product exists before updating stock

**Why this works**: Provides complete property specifications with validation rules, identifies exact file paths and patterns to follow, includes all three required CQRS components (command, handler, validator), and specifies DI registration requirements.

---

### Prompt 3: Generate DDD Value Object with Validation and Equality

Generate a DDD value object called Money for the Ordering.Domain with the following specifications:

VALUE OBJECT PROPERTIES:
- Amount (decimal) - The monetary amount (must be >= 0)
- Currency (string) - Three-letter currency code (must be valid ISO 4217 like "USD", "EUR", "GBP")

IMPLEMENTATION REQUIREMENTS:
1. Create value object at src\Ordering.Domain\AggregatesModel\OrderAggregate\Money.cs
2. Inherit from ValueObject base class in src\Ordering.Domain\SeedWork\ValueObject.cs (like Address does)
3. Follow the pattern from src\Ordering.Domain\AggregatesModel\OrderAggregate\Address.cs

REQUIRED METHODS:
- Private parameterless constructor for EF Core
- Public constructor with validation:
  - Throw ArgumentException if Amount < 0
  - Throw ArgumentException if Currency is null, empty, or not exactly 3 characters
  - Validate Currency is uppercase letters only
- Override GetEqualityComponents() yielding Amount and Currency
- Static operator overloads:
  - operator + (Money, Money) - Add two Money objects (same currency only)
  - operator - (Money, Money) - Subtract two Money objects (same currency only)
  - operator * (Money, decimal) - Multiply amount by scalar
- Static factory method: Money.Zero(string currency) - returns new Money(0, currency)
- Throw InvalidOperationException when trying to add/subtract different currencies

ADDITIONAL FEATURES:
- Public properties with private setters
- ToString() override returning formatted string like "$100.00 USD"
- Add XML documentation comments for public API

FILE STRUCTURE:
```csharp
namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public class Money : ValueObject
{
    // Implementation here following patterns above
}
```

**Why this works**: Specifies complete value object requirements including validation logic, identifies exact inheritance hierarchy and file patterns to follow, includes operator overloads for domain operations, and ensures EF Core compatibility with private constructor.

---

### Prompt 4: Generate CRUD API Endpoints for New Entity

Generate complete CRUD API endpoints for a Supplier entity in Catalog.API with the following specifications:

ENTITY DEFINITION (create if needed):
- SupplierId (int) - Primary key
- Name (string) - Supplier name (required, max 200 chars)
- ContactEmail (string) - Email (required, valid email format)
- ContactPhone (string) - Phone (optional, max 20 chars)
- Country (string) - Country (required, max 100 chars)
- IsActive (bool) - Active status (default true)

IMPLEMENTATION FILES:
1. API Endpoints: src\Catalog.API\Apis\SupplierApi.cs
   - Follow pattern from src\Catalog.API\Apis\CatalogApi.cs
   - Use versioned API routing with HasApiVersion(1, 0)
   - Create MapSupplierApi extension method

REQUIRED ENDPOINTS:
1. GET /api/suppliers - List all suppliers with pagination (pageSize, pageIndex)
   - WithName("ListSuppliers")
   - WithSummary("List all suppliers")
   - Return PaginatedItems<SupplierDTO>

2. GET /api/suppliers/{id} - Get supplier by ID
   - WithName("GetSupplier")
   - WithSummary("Get supplier by ID")
   - Return Results<Ok<SupplierDTO>, NotFound>

3. POST /api/suppliers - Create new supplier
   - WithName("CreateSupplier")
   - WithSummary("Create new supplier")
   - Accept CreateSupplierRequest
   - Validate email format
   - Return Results<Created<SupplierDTO>, ValidationProblem>

4. PUT /api/suppliers/{id} - Update supplier
   - WithName("UpdateSupplier")
   - WithSummary("Update supplier")
   - Accept UpdateSupplierRequest
   - Return Results<Ok, NotFound, ValidationProblem>

5. DELETE /api/suppliers/{id} - Soft delete (set IsActive = false)
   - WithName("DeleteSupplier")
   - WithSummary("Deactivate supplier")
   - Return Results<NoContent, NotFound>

DTO CLASSES (create in same file):
- SupplierDTO - all properties
- CreateSupplierRequest - Name, ContactEmail, ContactPhone, Country
- UpdateSupplierRequest - all properties except SupplierId

REGISTRATION:
- Add MapSupplierApi() call in src\Catalog.API\Program.cs after existing API mappings
- Add suppliers DbSet to CatalogContext
- Tag all endpoints with .WithTags("Suppliers")

ADDITIONAL REQUIREMENTS:
- Include OpenAPI documentation attributes
- Add proper HTTP status code documentation with [ProducesResponseType]
- Include validation for email format using EmailAddressAttribute pattern
- Add logging statements for all operations

**Why this works**: Provides complete CRUD endpoint specifications with request/response types, follows existing API patterns and versioning conventions, includes complete DTO definitions, and specifies all registration requirements.

---

### Prompt 5: Generate gRPC Service Definition

Generate a complete gRPC service for Inventory management in Catalog.API with the following specifications:

SERVICE OPERATIONS:
1. CheckStock - Check if product has sufficient stock
   - Request: ProductId (int32), RequiredQuantity (int32)
   - Response: IsAvailable (bool), CurrentStock (int32), ProductName (string)

2. ReserveStock - Reserve stock for an order
   - Request: ProductId (int32), Quantity (int32), OrderId (int32)
   - Response: Success (bool), ReservationId (string), ErrorMessage (string)

3. ReleaseReservation - Release previously reserved stock
   - Request: ReservationId (string)
   - Response: Success (bool)

IMPLEMENTATION FILES:
1. Proto Definition: src\Catalog.API\Proto\inventory.proto
   - Follow pattern from src\Basket.API\Proto\basket.proto
   - Use package name: InventoryApi
   - Set csharp_namespace = "eShop.Catalog.API.Grpc"
   - Define service named "Inventory"
   - Define all request/response messages

2. Service Implementation: src\Catalog.API\Grpc\InventoryService.cs
   - Follow pattern from src\Basket.API\Grpc\BasketService.cs
   - Inherit from Inventory.InventoryBase
   - Inject CatalogContext, ILogger<InventoryService>, ICatalogIntegrationEventService
   - Implement all three RPC methods
   - Get user identity from ServerCallContext
   - Add proper error handling with RpcException
   - Include logging for each operation

SERVICE LOGIC:
- CheckStock: Query database for product stock level
- ReserveStock:
  - Validate stock availability
  - Create reservation record in database
  - Generate unique ReservationId (Guid)
  - Deduct from available stock
  - Publish StockReservedIntegrationEvent
- ReleaseReservation:
  - Find reservation by ID
  - Return stock to available pool
  - Publish StockReleasedIntegrationEvent

CONFIGURATION:
- Register gRPC service in Program.cs: app.MapGrpcService<InventoryService>()
- Update .csproj to include Proto file:
```xml
<Protobuf Include="Proto\inventory.proto" GrpcServices="Server" />
```

ADDITIONAL REQUIREMENTS:
- Add [AllowAnonymous] to CheckStock only
- Require authentication for ReserveStock and ReleaseReservation
- Include XML documentation comments on proto service
- Add detailed logging at Debug level for diagnostics
- Handle concurrent reservation conflicts with proper error messages

**Why this works**: Provides complete gRPC service specification including proto definition syntax, identifies exact patterns to follow from existing gRPC services, includes service implementation logic, and specifies all configuration and registration requirements.

---

### Prompt 6: Generate Repository Interface and Implementation with EF Core

Generate a complete repository for Supplier entity in Catalog.API with the following specifications:

REPOSITORY PATTERN:
- Interface: src\Catalog.API\Repositories\ISupplierRepository.cs
- Implementation: src\Catalog.API\Repositories\SupplierRepository.cs

REPOSITORY INTERFACE (ISupplierRepository):
- Task<Supplier?> GetByIdAsync(int supplierId, CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default)
- Task<Supplier?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
- Task<Supplier> AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
- Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
- Task<bool> ExistsAsync(int supplierId, CancellationToken cancellationToken = default)

REPOSITORY IMPLEMENTATION (SupplierRepository):
- Constructor injecting CatalogContext and ILogger<SupplierRepository>
- Implement all interface methods using EF Core
- Use AsNoTracking() for read-only queries
- Add logging for all operations
- Include proper null handling
- Use async/await throughout

For complex repository patterns, reference:
- src\Ordering.Domain\AggregatesModel\OrderAggregate\IOrderRepository.cs (interface pattern)
- src\Ordering.Infrastructure\Repositories\OrderRepository.cs (if exists)

ENTITY CONFIGURATION:
Create src\Catalog.API\Infrastructure\EntityConfigurations\SupplierEntityTypeConfiguration.cs
- Implement IEntityTypeConfiguration<Supplier>
- Follow pattern from src\Ordering.Infrastructure\EntityConfigurations\OrderEntityTypeConfiguration.cs
- Configure:
  - Table name: "suppliers"
  - Primary key: SupplierId with UseHiLo("supplierseq")
  - Name: Required, MaxLength(200)
  - ContactEmail: Required, MaxLength(256)
  - ContactPhone: MaxLength(20)
  - Country: Required, MaxLength(100)
  - Index on ContactEmail (unique)
  - Index on Name for search performance

DBCONTEXT UPDATES:
Add to CatalogContext:
```csharp
public DbSet<Supplier> Suppliers => Set<Supplier>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfiguration(new SupplierEntityTypeConfiguration());
}
```

SERVICE REGISTRATION:
Add to Extensions.cs or Program.cs:
```csharp
services.AddScoped<ISupplierRepository, SupplierRepository>();
```

MIGRATION REQUIREMENTS:
After generation, create migration:
```bash
dotnet ef migrations add AddSuppliersTable --project src\Catalog.API\Catalog.API.csproj
```

ADDITIONAL REQUIREMENTS:
- Include XML documentation comments on interface
- Add [Index] attributes on Supplier entity if using EF Core 7+
- Use proper disposal patterns for DbContext
- Include comprehensive error handling in repository implementation

**Why this works**: Provides complete repository pattern with interface and implementation, includes EF Core entity configuration following existing patterns, specifies DbContext integration and migration steps, and covers service registration requirements.

---

### Prompt 7: Generate Background Service for Periodic Task Processing

Generate a background service called StockReplenishmentService for Catalog.API that periodically checks low-stock products and creates replenishment orders:

SERVICE SPECIFICATIONS:
- Service Name: StockReplenishmentService
- Location: src\Catalog.API\Services\StockReplenishmentService.cs
- Inherits: BackgroundService
- Namespace: eShop.Catalog.API.Services

FUNCTIONALITY:
1. Run every 30 minutes (configurable via BackgroundTaskOptions)
2. Query database for products with stock below threshold (AvailableStock < RestockThreshold)
3. Group low-stock products by supplier
4. For each supplier, create a StockReplenishmentRequestedIntegrationEvent
5. Publish events to event bus
6. Log all operations

IMPLEMENTATION PATTERN:
Follow the pattern from src\OrderProcessor\Services\GracePeriodManagerService.cs:
- Constructor with dependency injection
- ExecuteAsync method with infinite loop and cancellation token
- Configurable delay between iterations
- Proper logging at Debug and Information levels
- Graceful shutdown handling

DEPENDENCIES TO INJECT:
- IOptions<BackgroundTaskOptions> options (for configuration)
- IServiceScopeFactory serviceScopeFactory (to create scoped services in background task)
- IEventBus eventBus (to publish integration events)
- ILogger<StockReplenishmentService> logger

CONFIGURATION CLASS:
Create src\Catalog.API\Infrastructure\BackgroundTaskOptions.cs:
```csharp
public class BackgroundTaskOptions
{
    public int CheckIntervalMinutes { get; set; } = 30;
    public int RestockThreshold { get; set; } = 10;
}
```

INTEGRATION EVENT:
Create src\Catalog.API\IntegrationEvents\Events\StockReplenishmentRequestedIntegrationEvent.cs:
```csharp
public record StockReplenishmentRequestedIntegrationEvent(
    int SupplierId,
    string SupplierName,
    List<ReplenishmentItem> Items
) : IntegrationEvent;

public record ReplenishmentItem(
    int ProductId,
    string ProductName,
    int CurrentStock,
    int RequestedQuantity
);

SERVICE IMPLEMENTATION DETAILS:
- Use IServiceScopeFactory to create scope for CatalogContext access
- Query: Products where AvailableStock < RestockThreshold
- Calculate RequestedQuantity as (RestockThreshold * 2) - CurrentStock
- Log product count and supplier information
- Handle exceptions without crashing the background service
- Add cancellation token support throughout async operations

SERVICE REGISTRATION:
Add to src\Catalog.API\Extensions\Extensions.cs or Program.cs:
```csharp
// Configure options
builder.Services.Configure<BackgroundTaskOptions>(
    builder.Configuration.GetSection("BackgroundTasks"));

// Register hosted service
builder.Services.AddHostedService<StockReplenishmentService>();
```

APPSETTINGS CONFIGURATION:
Add to src\Catalog.API\appsettings.json:
```json
"BackgroundTasks": {
  "CheckIntervalMinutes": 30,
  "RestockThreshold": 10
}
```

ADDITIONAL REQUIREMENTS:
- Log when service starts and stops
- Log each iteration with timestamp
- Include try-catch in ExecuteAsync to prevent service crashes
- Use structured logging with event IDs
- Support graceful cancellation via CancellationToken

**Why this works**: Provides complete background service specification following existing patterns, includes all configuration requirements, shows proper dependency injection usage with scoped services, and includes integration event definitions for communication.

---

### Prompt 8: Generate Service Extension Method with Complete DI Registration

Generate a service configuration extension method for a new Payment service being added to the eShop solution:

SERVICE PROJECT:
- Project: src\Payment.API\Payment.API.csproj
- Extension File: src\Payment.API\Extensions\Extensions.cs

EXTENSION METHOD REQUIREMENTS:
Follow pattern from src\Ordering.API\Extensions\Extensions.cs and src\Webhooks.API\Extensions\Extensions.cs

Create AddApplicationServices extension method that configures:

1. AUTHENTICATION:
   - Add default authentication using builder.AddDefaultAuthentication()

2. DATABASE:
   - Add PostgreSQL DbContext named PaymentContext
   - Connection string: "paymentdb"
   - Enable connection pooling
   - Add migrations: builder.Services.AddMigration<PaymentContext, PaymentContextSeed>()
   - Enrich with observability: builder.EnrichNpgsqlDbContext<PaymentContext>()

3. EVENT BUS:
   - Add RabbitMQ event bus
   - Subscribe to these integration events:
     - OrderStatusChangedToStockConfirmedIntegrationEvent -> OrderStatusChangedToStockConfirmedIntegrationEventHandler
     - PaymentTimeoutIntegrationEvent -> PaymentTimeoutIntegrationEventHandler

4. MEDIATR:
   - Configure MediatR to scan current assembly
   - Add behaviors: LoggingBehavior, ValidatorBehavior, TransactionBehavior
   - Register command validators:
     - ProcessPaymentCommand -> ProcessPaymentCommandValidator
     - RefundPaymentCommand -> RefundPaymentCommandValidator

5. APPLICATION SERVICES:
   - services.AddHttpContextAccessor()
   - services.AddScoped<IPaymentService, PaymentService>()
   - services.AddScoped<IPaymentRepository, PaymentRepository>()
   - services.AddTransient<IIdentityService, IdentityService>()

6. INTEGRATION EVENTS:
   - services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<PaymentContext>>()
   - services.AddTransient<IPaymentIntegrationEventService, PaymentIntegrationEventService>()

7. HTTP CLIENTS:
   - Configure HttpClient for external payment gateway:

```csharp
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:Url"]
        ?? throw new InvalidOperationException("PaymentGateway:Url not configured"));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(); // Adds Polly policies
```

COMPLETE FILE STRUCTURE:
```csharp
internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Implementation following patterns above
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        // Event subscriptions here
    }
}
```

PROGRAM.CS USAGE:
Show how to use in src\Payment.API\Program.cs:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices(); // <- This extension method
builder.AddDefaultOpenApi();

var app = builder.Build();
// ... rest of Program.cs
```

ADDITIONAL REQUIREMENTS:
- Use internal static class for extensions (not public)
- Separate event bus subscriptions into private method AddEventBusSubscriptions
- Include null-checking for required configurations
- Add XML documentation comments explaining each registration section
- Follow the exact pattern from existing Extensions.cs files in the solution
- Ensure proper service lifetimes (Scoped vs Transient vs Singleton)

**Why this works**: Provides comprehensive service registration covering all typical microservice needs (auth, database, event bus, MediatR, HTTP clients), follows exact patterns from existing services, includes proper separation of concerns with private helper methods, and shows usage context.

---

## Effectiveness Tips

**Be Specific About Patterns**:
- Always reference exact files to follow as patterns (e.g., "Follow pattern from src\Ordering.API\Application\Commands\CreateOrderCommand.cs")
- This ensures consistency with existing codebase conventions and reduces errors

**Include Complete File Paths**:
- Specify exact file locations with full paths using relative paths from solution root
- This eliminates ambiguity about where generated code should be placed

**Specify All Registration Points**:
- Don't forget DI registration, event bus subscriptions, MediatR configuration, DbContext updates
- Incomplete registrations are a common source of runtime errors

**Reference Existing Patterns**:
- The eShop codebase has established patterns - always reference them
- For Integration Events: follow ProductPriceChangedIntegrationEvent
- For Commands: follow CreateOrderCommand
- For Value Objects: follow Address
- For Repositories: follow IOrderRepository

**Include Configuration Requirements**:
- Show appsettings.json changes, environment variables, connection strings
- Configuration is often overlooked but critical for functionality

**Specify Testing Requirements**:
- Mention what should be testable and how (unit tests, functional tests)
- Reference existing test patterns from tests\ directory

**Consider Cross-Cutting Concerns**:
- Logging (ILogger injection and usage)
- Observability (OpenTelemetry integration)
- Error handling (proper exception handling and status codes)
- Security (authentication, authorization attributes)
- Validation (FluentValidation for commands)

**Use Concrete Examples**:
- Instead of "add some properties", specify "ProductId (int), ProductName (string), Price (decimal)"
- Instead of "create validation", specify "RuleFor(x => x.ProductId).GreaterThan(0)"

**Think in Terms of Complete Modules**:
- Don't just generate a single class - generate everything needed for a working feature
- Integration event = event class + handler + registration + publishing logic
- CQRS command = command class + handler + validator + registration
- API endpoint = endpoint definition + DTOs + validation + OpenAPI docs
