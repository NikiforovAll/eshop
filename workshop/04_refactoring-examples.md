# Code Refactoring Examples for eShop

**Context**: These prompts demonstrate code refactoring use cases for the eShop reference application - a .NET 9 e-commerce system with microservices architecture orchestrated by .NET Aspire.

**Category**: Code Refactoring
**Sub Category**: Microservices Refactoring

---

## Prompt 1: Refactor CatalogApi Endpoints to CQRS Handler Classes

Refactor src\Catalog.API\Apis\CatalogApi.cs to use dedicated handler classes following CQRS pattern instead of inline lambda functions.

CURRENT STATE:
CatalogApi.cs contains 20+ endpoint methods (lines 116-404) with business logic implemented directly in static methods like GetAllItems(), UpdateItem(), DeleteItemById(). This makes the file 422 lines long and difficult to test individual operations.

REFACTORING GOAL:
Extract each endpoint's business logic into separate handler classes following the pattern used in Ordering.API:

EXAMPLE FOR GetItemById (lines 171-191):
- Create src\Catalog.API\Application\Queries\GetCatalogItemByIdQuery.cs
- Create src\Catalog.API\Application\Queries\GetCatalogItemByIdQueryHandler.cs
- Implement IRequestHandler<GetCatalogItemByIdQuery, CatalogItem>
- Move validation and database access logic from the static method into the handler
- Update the endpoint to: api.MapGet("/items/{id:int}", (IMediator mediator, int id) => mediator.Send(new GetCatalogItemByIdQuery(id)))

REQUIREMENTS:
1. Start with these high-priority endpoints: GetItemById, UpdateItem, CreateItem, DeleteItemById
2. Keep the existing minimal API endpoint registrations in CatalogApi.cs
3. Use MediatR (already used in Ordering.API) - add package reference if needed
4. Preserve all existing response types: Ok<T>, NotFound, BadRequest<ProblemDetails>
5. Keep AI embedding logic in UpdateItem and CreateItem handlers
6. Maintain integration event publishing in UpdateItem handler (ProductPriceChangedIntegrationEvent)
7. Ensure all tests in tests\Catalog.FunctionalTests\CatalogApiTests.cs continue passing

FOLLOW EXISTING PATTERNS:
- Use the command/query handler structure from src\Ordering.API\Application\Commands\
- Look at src\Ordering.API\Application\Commands\CreateOrderCommandHandler.cs as a template
- Inject dependencies through constructor like CatalogContext, ICatalogAI, ICatalogIntegrationEventService

DON'T REFACTOR YET:
- Leave GetAllItems, GetItemsBySemanticRelevance, and other query methods for a separate refactoring
- Keep the CatalogServices parameter object for now

Why: Breaks down a large monolithic API class into focused, testable units, follows established CQRS patterns in the codebase, improves maintainability and readability.


---

## Prompt 2: Replace Magic Strings with WebhookType Enum

Refactor the webhook event type handling in src\Webhooks.API\Apis\WebhooksApi.cs to use strongly-typed WebhookType enum instead of string-based event parsing.

CURRENT STATE:
Line 50 in WebhooksApi.cs uses: Type = Enum.Parse<WebhookType>(request.Event, ignoreCase: true)
This accepts ANY string in the request.Event field and throws exception at runtime for invalid values.

The WebhookSubscriptionRequest model likely has: public string Event { get; set; }

REFACTORING GOAL:
1. Change WebhookSubscriptionRequest.Event from string to WebhookType enum
2. Remove the Enum.Parse call and directly assign: Type = request.Event
3. Let ASP.NET Core model binding handle invalid enum values automatically (returns 400 BadRequest)
4. Update API documentation/OpenAPI to show valid enum values

REQUIREMENTS:
1. Modify src\Webhooks.API\Model\WebhookSubscriptionRequest.cs (or wherever it's defined)
2. Change Event property from "public string Event" to "public WebhookType Event"
3. Update line 50 in WebhooksApi.cs from "Enum.Parse<WebhookType>(request.Event, ignoreCase: true)" to just "request.Event"
4. Add [JsonConverter(typeof(JsonStringEnumConverter))] to WebhookType enum in src\Webhooks.API\Model\WebhookType.cs to preserve string serialization in JSON
5. Verify WebhookClient still works correctly when subscribing to webhooks
6. Test that sending invalid event names returns proper 400 error with clear message

EXISTING ENUM DEFINITION (WebhookType.cs):
The enum has three values: CatalogItemPriceChange = 1, OrderShipped = 2, OrderPaid = 3

WHY:
Eliminates runtime string parsing errors, provides compile-time type safety, improves API documentation, and leverages ASP.NET Core's built-in enum validation. The API contract becomes self-documenting.

---

## Prompt 3: Reduce Duplication in BasketService Mapping Methods

Refactor the mapping logic in src\Basket.API\Grpc\BasketService.cs to eliminate duplication between MapToCustomerBasketResponse and MapToCustomerBasket methods.

CURRENT STATE:
Lines 77-110 contain two nearly identical mapping methods:
- MapToCustomerBasketResponse (lines 77-91): Maps CustomerBasket domain model to CustomerBasketResponse gRPC message
- MapToCustomerBasket (lines 93-110): Maps UpdateBasketRequest gRPC message to CustomerBasket domain model

Both methods iterate through items collections and manually map ProductId and Quantity fields. This is duplicated logic.

REFACTORING GOAL:
Extract common mapping logic into reusable methods or use a mapping library while preserving gRPC contract:

OPTION 1 - Extract Helper Methods:
1. Create MapBasketItem(BasketItem source) that returns BasketItem (proto)
2. Create MapBasketItem(BasketItem proto) that returns BasketItem (domain model)
3. Simplify the two existing mapping methods to use these helpers

OPTION 2 - Use Mapperly (preferred for .NET 9):
1. Add Mapperly NuGet package (compile-time source generator mapper)
2. Create src\Basket.API\Mappings\BasketMappings.cs with [Mapper] attribute
3. Define mapping methods with partial methods
4. Inject IBasketMapper into BasketService constructor
5. Replace manual mapping calls with mapper.MapToResponse(customerBasket)

REQUIREMENTS:
1. Preserve exact gRPC contract - CustomerBasketResponse and UpdateBasketRequest proto messages must not change
2. Keep the domain model CustomerBasket and BasketItem classes unchanged
3. Do NOT use AutoMapper (too heavy for this simple case)
4. Ensure all tests in tests\Basket.UnitTests\BasketServiceTests.cs pass
5. Verify gRPC service still works correctly from Ordering.API and WebApp clients
6. If using Mapperly, follow AOT-compatible patterns (Basket.API targets AOT compilation)

FOLLOW EXISTING PATTERNS:
- The codebase prefers explicit, simple mapping over heavy reflection-based solutions
- Check if other services use mapping patterns to maintain consistency

WHY:
Eliminates duplicated mapping logic, makes it easier to add new properties to basket items, reduces chances of mapping bugs where one method is updated but not the other. Mapperly provides compile-time safety with zero runtime overhead.

**Why this works**: Shows exact line numbers with duplication, offers two concrete refactoring approaches, considers AOT compilation requirements, and references test verification.

---

## Prompt 5: Extract Order Status Transition Logic into State Machine

Refactor the status transition methods in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs to use a proper state machine pattern instead of scattered if-checks.

CURRENT STATE:
The Order aggregate has 6 status transition methods (lines 99-168):
- SetAwaitingValidationStatus() - checks if Submitted
- SetStockConfirmedStatus() - checks if AwaitingValidation
- SetPaidStatus() - checks if StockConfirmed
- SetShippedStatus() - checks if Paid
- SetCancelledStatus() - checks if Paid or Shipped
- SetCancelledStatusWhenStockIsRejected() - checks if AwaitingValidation

Each method has different validation logic. The valid transitions are implicit and hard to visualize. The StatusChangeException helper (line 180) provides generic error messages.

REFACTORING GOAL:
Create an explicit state machine that:
1. Defines all valid OrderStatus transitions in one place
2. Makes illegal transitions impossible at compile-time where possible
3. Provides clear error messages for invalid transitions
4. Preserves all existing domain events (OrderStatusChangedTo*DomainEvent)

IMPLEMENTATION APPROACH:
1. Create src\Ordering.Domain\AggregatesModel\OrderAggregate\OrderStateMachine.cs
2. Define a dictionary of valid transitions: Dictionary<OrderStatus, HashSet<OrderStatus>>
3. Add method: bool CanTransitionTo(OrderStatus from, OrderStatus to)
4. Add method: void ValidateTransition(OrderStatus from, OrderStatus to) - throws descriptive exception
5. Refactor each SetXStatus() method to call stateMachine.ValidateTransition(this.OrderStatus, OrderStatus.X)
6. Keep all domain event raising logic in the Order aggregate methods

VALID TRANSITIONS TO ENCODE:
- Submitted → AwaitingValidation, Cancelled
- AwaitingValidation → StockConfirmed, Cancelled
- StockConfirmed → Paid
- Paid → Shipped
- Shipped → (terminal state)
- Cancelled → (terminal state)

REQUIREMENTS:
1. All existing unit tests in tests\Ordering.UnitTests\Domain\OrderAggregateTest.cs must pass
2. Add new tests for invalid transitions (e.g., Submitted → Shipped should throw)
3. Improve StatusChangeException messages to show valid transitions: "Cannot change from Submitted to Shipped. Valid transitions: AwaitingValidation, Cancelled"
4. Keep the Order aggregate as the entry point for all status changes (no direct state machine access)
5. Preserve DDD patterns - state machine is an implementation detail of the aggregate

DON'T CHANGE:
- Domain event raising (AddDomainEvent calls)
- Method signatures (public API of Order aggregate)
- OrderStatus enum values

WHY THIS WORKS:
Centralizes transition logic, makes valid state flows explicit and self-documenting, improves error messages, easier to add new statuses or transitions in the future. The state machine encodes business rules that are currently scattered across 6 methods.

**Why this works**: Identifies scattered logic pattern, provides concrete state machine implementation with explicit transition table, preserves DDD patterns, includes comprehensive testing requirements.

---
## Prompt 6: Apply Interface Segregation Principle to IOrderQueries

Refactor src\Ordering.API\Application\Queries\IOrderQueries.cs and its implementation to follow the Interface Segregation Principle by splitting into focused interfaces.

CURRENT STATE:
The IOrderQueries interface and OrderQueries implementation (src\Ordering.API\Application\Queries\OrderQueries.cs) has three unrelated responsibilities:
1. GetOrderAsync(int id) - single order retrieval
2. GetOrdersFromUserAsync(string userId) - list orders for a user
3. GetCardTypesAsync() - lookup card types reference data

Consumers that only need card types must depend on the entire interface. The OrderQueries class mixes domain query logic with reference data lookups.

REFACTORING GOAL:
Split into three focused interfaces following ISP:

1. IOrderQueries - single order retrieval
   ```csharp
   public interface IOrderQueries
   {
       Task<Order> GetOrderAsync(int id);
   }
   ```

2. IUserOrderQueries - user's order history
   ```csharp
   public interface IUserOrderQueries
   {
       Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId);
   }
   ```

3. IOrderReferenceDataQueries - lookup data
   ```csharp
   public interface IOrderReferenceDataQueries
   {
       Task<IEnumerable<CardType>> GetCardTypesAsync();
   }
   ```

IMPLEMENTATION APPROACH:
1. Keep OrderQueries class implementing all three interfaces initially
2. Update DI registration to register all three interfaces pointing to same implementation
3. Update consumers in src\Ordering.API\Apis\ to inject only the interface they need
4. Consider splitting OrderQueries into three separate classes in a future iteration

REQUIREMENTS:
1. Create the three new interface files in src\Ordering.API\Application\Queries\
2. Update OrderQueries.cs to implement all three interfaces:
   `public class OrderQueries : IOrderQueries, IUserOrderQueries, IOrderReferenceDataQueries`
3. Update DI registration in src\Ordering.API\Extensions or Program.cs:
   ```csharp
   builder.Services.AddScoped<OrderQueries>();
   builder.Services.AddScoped<IOrderQueries>(sp => sp.GetRequiredService<OrderQueries>());
   builder.Services.AddScoped<IUserOrderQueries>(sp => sp.GetRequiredService<OrderQueries>());
   builder.Services.AddScoped<IOrderReferenceDataQueries>(sp => sp.GetRequiredService<OrderQueries>());
   ```
4. Find all usages of IOrderQueries in src\Ordering.API\ and update to inject the specific interface needed
5. Keep the original IOrderQueries interface for now (mark as [Obsolete] with message)
6. Ensure all tests pass in tests\Ordering.FunctionalTests\ and tests\Ordering.UnitTests\

BENEFITS ANALYSIS:
- OrderApi endpoints that only need GetCardTypesAsync no longer depend on order retrieval logic
- Easier to mock in tests - only mock the methods actually being tested
- Future: Can implement IOrderReferenceDataQueries with caching without affecting order queries
- Clear separation: transactional queries vs reference data lookups

DON'T OVERENGINEER:
- Keep one implementation class for now - don't create three separate classes unless needed
- Don't change the query method implementations themselves
- Don't refactor the view models (Order, OrderSummary, CardType) yet

WHY THIS WORKS:
Follows SOLID principles (ISP specifically), makes dependencies explicit, improves testability, allows future optimization of different query types independently. Common refactoring for queries that have grown over time.

**Why this works**: Explains ISP violation clearly, provides concrete interface definitions and DI registration code, includes migration strategy (obsolete attribute), defines what NOT to over-engineer.

---

## Effectiveness Tips

- **Reference exact file paths and line numbers** - Makes it easy to locate code to refactor. Use relative paths from repository root (src\, tests\).

- **Show before/after code examples** - Demonstrate the current problem and desired solution. Include enough context (5-10 lines) to understand the change.

- **Specify what to preserve** - Call out tests that must pass, behavior that must remain identical, API contracts that cannot change. This prevents breaking changes.

- **Follow existing patterns** - Point to similar code in the codebase that demonstrates the preferred pattern. For eShop: reference Ordering.API for CQRS, ServiceDefaults for cross-cutting concerns.

- **Consider architectural constraints** - For eShop: DDD patterns in Ordering, AOT compilation in Basket.API, event-driven communication, microservices isolation.

- **Define scope clearly** - Refactoring should be incremental. Specify what to change NOW and what to defer. Use "Don't change yet" sections.

- **Include verification steps** - Specify which tests to run, how to verify behavior is unchanged, performance considerations to check.

- **Explain the "why"** - Connect refactoring to principles (SOLID, DDD), benefits (testability, maintainability), and specific pain points it solves.

- **Address trade-offs** - Acknowledge any performance implications, complexity increases, or temporary duplication during migration.

- **Use concrete examples** - Instead of "improve error handling", say "map OrderingDomainException to 400 BadRequest with RFC 7807 ProblemDetails format".
