# Tactical Code Documentation Examples for eShop

**Context**: These prompts show how to add effective documentation to the eShop codebase. Focus on documenting complex logic, domain patterns, and integration points where documentation adds real value.

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

## Effectiveness Tips

**Choose the Right Documentation Type:**
- **XML docs** for public APIs, interfaces, complex domain logic (external consumers)
- **Inline comments** for non-obvious implementation details, workarounds, business rule explanations
- **Separate docs** for architecture, deployment, cross-cutting concerns, onboarding documents, integration flows

**What NOT to Document:**
- Obvious code (getters/setters, simple CRUD)
- Implementation details that are self-explanatory
- Redundant information ("this method returns a value")

---

## Prompt 1: Document DDD Aggregate Root (XML Docs)

```
Add XML documentation to the Order aggregate root in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs.
```

---

## Prompt 2: Document Order State Machine (XML + Inline)

```
Add documentation to order status transition methods in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs

**Each status method:**
- XML <summary>: Business meaning of the transition
- XML <exception>: Invalid state transitions (e.g., can't ship unpaid orders)
- Inline comment: Why this enforces business rules (prevents real-world violations)
- Document preconditions and domain events raised
```

---

## Prompt 3: Document Integration Event Flow (Separate Doc)

```
Create a markdown document at docs\IntegrationEvents\OrderCancellationFlow.md documenting the order cancellation integration event flow between Ordering.API and Catalog.API.

Use mermaid diagrams to illustrate event publishing and handling.
**Document should include:**
- Overview of the OrderCancelledIntegrationEvent purpose
- Sequence diagram of event flow (Ordering.API publishes, Catalog.API handles)
```