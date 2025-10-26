# Tactical Code Documentation Examples for eShop

**Context**: These prompts show how to add effective documentation to the eShop codebase. Focus on documenting complex logic, domain patterns, and integration points where documentation adds real value.

**Category**: Code Documentation
**Documentation Types**: XML docs (`///`), inline comments (`//`), separate docs (README.md, architecture docs)

---

## Documentation Types Overview

**XML Documentation (`/// <summary>`):**
- Public APIs, interfaces, methods that external code consumes
- Complex domain logic, business rules, state machines
- Exception conditions, preconditions, return values
- Usage examples for non-obvious patterns

**Inline Comments (`//`):**
- Non-obvious implementation decisions
- Complex algorithms or business logic
- "Why" not "what" - explain reasoning, not mechanics
- Workarounds, edge cases, performance considerations

**Separate Documentation (README.md, docs/):**
- Architecture overviews, system design
- Integration guides, API contracts
- Deployment instructions, configuration
- Cross-cutting concerns (authentication, observability)

---

## Prompt 1: Document DDD Aggregate Root (XML Docs)

```
Add XML documentation to the Order aggregate root in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs. Focus on:

**Class-level:**
- Explain it's an aggregate root that maintains order consistency boundaries
- Document the state machine: Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped
- Explain invariants: status transitions, encapsulated collection

**Key methods:**
- Status transition methods (SetAwaitingValidationStatus, SetPaidStatus, etc.): preconditions, domain events raised, business meaning
- AddOrderItem: why it's the only way to add items, validation rules
- OrderItems property: why IReadOnlyCollection, DDD encapsulation

**Include:**
- <remarks> for DDD patterns and consistency boundaries
- <exception> tags for invalid state transitions
- Start with: existing XML docs in Ordering.Domain, DDD aggregate patterns
```

---

## Prompt 2: Document Event-Driven Integration (Examples)

```
Add XML documentation with code examples to IEventBus in src\EventBus\Abstractions\IEventBus.cs.

**Interface-level:**
- Explain role in microservices event-driven architecture
- Document reliability guarantees (Outbox pattern, at-least-once delivery)
- Mention transport: RabbitMQ

**PublishAsync method:**
- <param>: IntegrationEvent for cross-service communication
- <returns>: Task completes when published to RabbitMQ (not consumed)
- <remarks>: Outbox pattern stores events transactionally
- <example>: Show publishing OrderStartedIntegrationEvent and event flow

**Include:**
- Real eShop event types in examples
- Reference Outbox pattern docs
- Start with: existing integration events, IntegrationEventLogService
```

---

## Prompt 3: Document Outbox Pattern Implementation (XML Docs)

```
Add XML documentation to IntegrationEventLogService in src\IntegrationEventLogEF\Services\IntegrationEventLogService.cs explaining the Transactional Outbox pattern.

**Class-level:**
- Implements Outbox pattern for reliable event publishing
- Events stored in same transaction as business data
- Event lifecycle: NotPublished → InProgress → Published/PublishedFailed
- Prevents event loss on crashes (transactional guarantee)

**Key methods:**
- SaveEventAsync: <param> transaction for atomic commit, <remarks> participates in business transaction
- RetrieveEventLogsPendingToPublishAsync: retrieves committed but unpublished events, preserves ordering

**Include:**
- Failure scenarios and guarantees
- Start with: Outbox pattern overview, event publishing flow
```

---

## Prompt 4: Document Complex Business Logic (Inline + XML)

```
Add documentation to CatalogItem stock management in src\Catalog.API\Model\CatalogItem.cs.

**RemoveStock method:**
- XML <summary>: Decrements available stock for order fulfillment
- XML <exception>: CatalogDomainException if insufficient stock
- Inline comment: Explain MaxStockThreshold silent capping behavior (why it caps instead of throwing)
- Inline comment: Why AvailableStock can never go negative (business invariant)

**AddStock method:**
- XML <summary>: Restores stock (from cancellations or restocking)
- Inline comment: Explain MaxStockThreshold limit and why (warehouse capacity)

**Properties:**
- RestockThreshold: when low stock alerts trigger
- OnReorder: flag to prevent duplicate restock events

**Include:**
- Business rules and edge cases
- Start with: existing CatalogItem methods, domain exceptions
```

---

## Prompt 5: Document Order State Machine (XML + Inline)

```
Add documentation to order status transition methods in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs (lines 100-160).

**Each status method:**
- XML <summary>: Business meaning of the transition
- XML <exception>: Invalid state transitions (e.g., can't ship unpaid orders)
- Inline comment: Why this enforces business rules (prevents real-world violations)
- Document preconditions and domain events raised

**Examples:**
- SetAwaitingValidationStatus: from Submitted only, triggers stock validation
- SetShippedStatus: from Paid only, prevents shipping unpaid orders
- SetCancelledStatus: not allowed after Paid/Shipped (requires refund process)

**Class-level <remarks>:**
- Explain this enforces the order state machine
- Document that methods are the only way to change status (aggregate encapsulation)
- Start with: existing status methods, OrderingDomainException patterns
```

---

## Prompt 6: Document gRPC Service Contract (XML Docs)

```
Add XML documentation to Basket gRPC service in src\Basket.API\Grpc\BasketService.cs.

**Service-level:**
- Explain gRPC API for basket operations
- Authentication required (JWT bearer tokens)
- Concurrency: last-write-wins semantics

**Key methods:**
- GetBasket: retrieves user's basket, returns empty if not found
- UpdateBasket: saves basket to Redis, authentication required
- DeleteBasket: removes basket, returns success even if doesn't exist (idempotent)

**Include:**
- <param> and <returns> for all methods
- <exception> for authentication failures, validation errors
- gRPC status codes: Unauthenticated, InvalidArgument, NotFound
- Start with: basket.proto definitions, BasketService authentication patterns
```

---

## Prompt 7: Document Service Configuration (XML Docs + Separate README)

```
Add documentation for ServiceDefaults extension methods and create a README explaining the pattern.

**XML docs for AddServiceDefaults() in src\eShop.ServiceDefaults\Extensions.cs:**
- <summary>: Configures OpenTelemetry, health checks, service discovery, resilience
- <param name="builder">: IHostApplicationBuilder to configure
- Explain what's included: logging, metrics, traces, health endpoints, HTTP client resilience
- <remarks>: All eShop services must call this for observability and reliability

**Separate README at src\eShop.ServiceDefaults\README.md:**
- Purpose: standardize service configuration across microservices
- What it configures: OpenTelemetry (OTLP exporter), health checks (/health, /alive), HTTP resilience (Polly)
- How to use: call builder.AddServiceDefaults() in Program.cs
- Aspire integration: automatic service discovery and telemetry aggregation
- Start with: existing AddServiceDefaults implementation, Aspire patterns
```

---

## Prompt 8: Document Integration Event Handler (XML Docs + Inline)

```
Add documentation to OrderStatusChangedToPaidIntegrationEventHandler in src\Catalog.API\IntegrationEvents\EventHandling\.

**Class-level:**
- Subscribes to OrderStatusChangedToPaidIntegrationEvent from Ordering.API
- Decrements catalog inventory when orders are paid
- Triggers low stock alerts if needed

**Handle method:**
- <summary>: Processes paid orders by reducing stock for each item
- Inline comment: Why we check OnReorder flag (prevent duplicate low stock events)
- Inline comment: Outbox pattern usage for publishing ProductLowStockIntegrationEvent
- <exception>: What happens if product not found, insufficient stock

**Include:**
- Event-driven flow explanation
- Transactional guarantees (event processing + stock update + new event publishing)
- Start with: integration event handler patterns, Outbox pattern usage
```

---

## Effectiveness Tips

**Choose the Right Documentation Type:**
- **XML docs** for public APIs, interfaces, complex domain logic (external consumers)
- **Inline comments** for non-obvious implementation details, workarounds, business rule explanations
- **Separate docs** for architecture, deployment, cross-cutting concerns (developers new to the system)

**What to Document:**
- Business rules and domain invariants (WHY, not WHAT)
- State machines, complex workflows, preconditions
- Integration points: events, gRPC contracts, API endpoints
- Failure modes, exception conditions, edge cases
- Architectural patterns: DDD, CQRS, Outbox, event-driven
- Transactional guarantees, concurrency semantics

**What NOT to Document:**
- Obvious code (getters/setters, simple CRUD)
- Implementation details that are self-explanatory
- Redundant information ("this method returns a value")

**Best Practices:**
- Use concrete examples with real eShop types (OrderStartedIntegrationEvent, not GenericEvent)
- Explain WHY decisions were made, not just WHAT the code does
- Reference architectural patterns by name (DDD aggregate root, Outbox pattern)
- Document failure scenarios and guarantees (especially in distributed systems)
- Keep it concise - trust developers to read code, use docs to explain context
