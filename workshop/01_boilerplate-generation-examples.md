# Boilerplate Code Generation Examples for eShop

**Context**: These examples demonstrate how to generate complete, boilerplate code for common microservices patterns in the eShop codebase. Each prompt generates all necessary files, configurations, and registrations for a working feature.

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

**Include Configuration Requirements**:
- Show appsettings.json changes, environment variables, connection strings

**Specify Testing Requirements**:
- Mention what should be testable and how (unit tests, functional tests)
- Reference existing test patterns from tests\ directory

**Consider Cross-Cutting Concerns**:
- Logging (ILogger injection and usage)
- Observability (OpenTelemetry integration)
- Error handling (proper exception handling and status codes)
- Security (authentication, authorization attributes)
- Validation (FluentValidation for commands)

**Think in Terms of Complete Modules**:
- Don't just generate a single class - generate everything needed for a working feature
- Integration event = event class + handler + registration + publishing logic
- CQRS command = command class + handler + validator + registration
- API endpoint = endpoint definition + DTOs + validation + OpenAPI docs

---

## Prompt Examples


### Prompt 1: Generate CRUD API Endpoints for New Entity

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

### Prompt 2: Generate Complete Integration Event with Handler and Subscriptions

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

ADDITIONAL REQUIREMENTS:
- Use record type with primary constructor for the event
- Implement IIntegrationEventHandler<ProductRestockedIntegrationEvent> for handler
- Add XML comments explaining when this event is raised
- Include logging in the handler using ILogger

**Why this works**: Specifies complete event properties with types, identifies exact file locations and patterns to follow, includes all registration requirements, and specifies naming conventions ensuring consistency with existing codebase patterns.

---

### Prompt 3: Generate CQRS Command with Handler and Validator
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
