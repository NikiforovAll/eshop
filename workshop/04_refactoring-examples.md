# Code Refactoring Examples for eShop

**Context**: These prompts demonstrate code refactoring use cases for the eShop reference application - a .NET 9 e-commerce system with microservices architecture orchestrated by .NET Aspire.


## Effectiveness Tips

- **Start from principles and goals** - Explain which design principle (SOLID, DDD) or code smell (duplication, large methods) is being addressed and why it matters.
- **Reference exact file paths and line numbers** - Makes it easy to locate code to refactor.
- **Follow existing patterns** - Point to similar code in the codebase that demonstrates the preferred pattern. For eShop: reference Ordering.API for CQRS, ServiceDefaults for cross-cutting concerns.
- **Define scope clearly** - Refactoring should be incremental. Specify what to change NOW and what to defer. Use "Don't change yet" sections.
- **Include verification steps** - Specify which tests to run, how to verify behavior is unchanged, performance considerations to check.
- **Explain the "why"** - Connect refactoring to principles (SOLID, DDD), benefits (testability, maintainability), and specific pain points it solves.
- **Address trade-offs** - Acknowledge any performance implications, complexity increases, or temporary duplication during migration.

---

## Prompt 1: Refactor CatalogApi Endpoints to CQRS Handler Classes

Refactor src\Catalog.API\Apis\CatalogApi.cs to use dedicated handler classes following CQRS pattern instead of inline lambda functions.

CURRENT STATE:
CatalogApi.cs contains 20+ endpoint methods with business logic implemented directly in static methods like GetAllItems(), UpdateItem(), DeleteItemById().

REFACTORING GOAL:
Extract each endpoint's business logic into separate handler classes following the pattern used in Ordering.API:

EXAMPLE FOR GetItemById:
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
---

## Prompt 2: Reduce Duplication in BasketService Mapping Methods

Refactor the mapping logic in src\Basket.API\Grpc\BasketService.cs to eliminate duplication between MapToCustomerBasketResponse and MapToCustomerBasket methods.

CURRENT STATE:
- MapToCustomerBasketResponse: Maps CustomerBasket domain model to CustomerBasketResponse gRPC message
- MapToCustomerBasket: Maps UpdateBasketRequest gRPC message to CustomerBasket domain model

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

---

## Prompt 3: Extract Order Status Transition Logic into State Machine

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
