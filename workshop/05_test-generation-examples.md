# AI-Assisted Test Suite Generation for eShop

**Context**: These prompts demonstrate how to leverage Claude Code for comprehensive test suite generation. Focus on having AI identify test cases, discover edge cases, generate test data, and create proper assertions automatically.

**Category**: Test Generation & Quality Assurance
**Approach**: AI discovers what to test, generates test data, creates complete test suites

---

## AI-Assisted Test Generation Philosophy

Instead of specifying every test case, let Claude Code:
- **Analyze code** to identify testable scenarios and edge cases
- **Generate test data** that covers boundary conditions
- **Create assertions** based on expected behavior
- **Suggest additional tests** you might not have considered
- **Integrate with existing test infrastructure** automatically

---

## Prompt 1: Comprehensive Domain Aggregate Test Suite

```
Analyze the Order aggregate in src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs and generate a comprehensive test suite in tests\Ordering.UnitTests\Domain\OrderAggregateTest.cs.

**Your task:**
1. Identify all public methods and their business logic
2. Discover edge cases by analyzing validation logic, exceptions, and business rules
3. Generate test cases for:
   - Happy path scenarios
   - Boundary conditions (zero, negative, max values)
   - Invalid state transitions
   - Domain exception scenarios
   - Domain events being raised correctly
4. Create realistic test data (product IDs, prices, quantities)
5. Follow existing test patterns in the file (Arrange-Act-Assert, [TestMethod])

**Focus areas:**
- Status transition methods (SetAwaitingValidationStatus, SetPaidStatus, SetShippedStatus, etc.)
- AddOrderItem: duplicate handling, validation, quantity accumulation
- Order creation: address validation, buyer info, payment method
- State machine: valid and invalid transitions

Let me know what test scenarios you identify, then implement the complete test suite.
```

---

## Prompt 2: API Endpoint Test Suite with Edge Cases

```
Generate a comprehensive test suite for the Catalog API endpoints in tests\Catalog.FunctionalTests\CatalogApiTests.cs.

**Your task:**
1. Analyze CatalogApi.cs endpoints (GET, POST, PUT, DELETE)
2. Identify edge cases:
   - Non-existent IDs (404 scenarios)
   - Invalid input validation (400 scenarios)
   - Pagination edge cases (empty results, last page)
   - Concurrent modifications
   - Large datasets
3. Generate test data:
   - Valid catalog items with realistic data
   - Invalid items (missing fields, negative prices, empty strings)
   - Boundary values (price = 0, max price, long strings)
4. Create tests for both API versions (v1.0, v2.0) using [Theory] and [InlineData]
5. Use existing CatalogApiFixture and follow patterns in the file

**Include tests for:**
- GET /api/catalog/items (pagination, filtering, empty results)
- GET /api/catalog/items/{id} (exists, not found)
- POST /api/catalog/items (valid, invalid data, duplicate names)
- PUT /api/catalog/items (update existing, not found, concurrent updates)
- DELETE /api/catalog/items/{id} (success, not found, cascading effects)

Suggest test scenarios first, then implement.
```

---

## Prompt 3: Event Handler Test Suite with Scenarios

```
Create a complete test suite for OrderStatusChangedToPaidIntegrationEventHandler in src\Catalog.API\IntegrationEvents\EventHandling\.

**Your task:**
1. Analyze the handler logic (inventory reduction, low stock alerts)
2. Identify scenarios to test:
   - Successful stock reduction for multiple items
   - Insufficient stock scenarios
   - Product not found scenarios
   - Low stock alert triggering (AvailableStock <= RestockThreshold)
   - OnReorder flag preventing duplicate alerts
   - Multiple order items affecting same product
3. Generate test data:
   - Integration events with various order items
   - CatalogItem entities with different stock levels
   - Products at/below/above RestockThreshold
4. Mock dependencies: CatalogContext, ICatalogIntegrationEventService, ILogger
5. Verify behavior: stock decremented, events published, exceptions handled

**Test categories:**
- Happy path: stock reduced successfully
- Edge cases: stock at threshold, below threshold, zero stock
- Error handling: product not found, insufficient stock
- Business logic: OnReorder flag, low stock event publishing
- Concurrency: multiple events for same product

Generate the test suite with realistic scenarios and proper mocking.
```
---

## Prompt 6: Validation Logic Test Suite

```
Generate comprehensive tests for AddressValidationService (if it exists) or create tests for address validation in CreateOrderCommandHandler.

**Your task:**
1. Analyze validation rules in the code
2. Identify validation scenarios:
   - All fields valid
   - Missing required fields (street, city, zipcode, country, state)
   - Invalid formats (zipcode patterns, empty strings, whitespace)
   - Boundary lengths (min/max string lengths)
   - Special characters, unicode, SQL injection attempts
3. Generate test data covering:
   - Valid addresses (US, international)
   - Invalid addresses for each validation rule
   - Edge cases (single character, max length, emoji, null bytes)
4. Create meaningful test names describing what's being validated
5. Verify:
   - Validation passes for valid data
   - Validation fails with correct error messages
   - Multiple validation errors collected
   - Specific error messages for each rule

**Test structure:**
- ValidAddress_PassesValidation
- MissingStreet_FailsValidation
- InvalidZipCodeFormat_FailsValidation
- [Edge case tests based on code analysis]

Generate complete validation test suite with diverse test data.
```

---

## Prompt 7: Integration Test Suite for Complete Flow

```
Create integration tests for the complete order creation flow from Blazor UI to database.

**Your task:**
1. Analyze the flow: WebApp → Ordering.API → OrderRepository → Database
2. Identify test scenarios:
   - Create order with valid data (end-to-end success)
   - Invalid order data (validation failures)
   - Insufficient inventory (integration with Catalog)
   - Payment processing (integration with payment service)
   - Event publishing (integration events raised)
3. Generate test data:
   - Complete order DTOs with items, address, payment
   - Various product inventories in catalog
   - User authentication tokens
4. Use Aspire test fixtures to spin up full infrastructure
5. Verify:
   - Order persisted correctly
   - Inventory decremented
   - Domain events raised
   - Integration events published
   - Status transitions work

**Requirements:**
- Use WebApplicationFactory with Aspire host
- Require Docker for test containers (PostgreSQL, RabbitMQ, Redis)
- Test in isolation but with real infrastructure
- Verify database state, event bus messages, HTTP responses

Generate end-to-end integration test suite.
```

---

## Prompt 8: Mutation Testing - Generate Missing Tests

```
Analyze test coverage for CatalogItem.cs and generate tests for uncovered scenarios.

**Your task:**
1. Review existing tests in tests\Catalog.UnitTests\
2. Analyze CatalogItem business logic:
   - RemoveStock: MaxStockThreshold capping, negative stock prevention
   - AddStock: restoration, threshold limits
   - Stock alerts: RestockThreshold, OnReorder flag
3. Identify missing test scenarios:
   - Mutations that would pass existing tests
   - Edge cases not currently covered
   - Error paths not tested
4. Generate tests to kill potential mutants:
   - Boundary values for thresholds
   - Off-by-one errors in comparisons
   - Incorrect boolean logic
   - Missing null checks

**Approach:**
- Review what's currently tested
- Identify gaps in logic coverage
- Generate tests that would catch common bugs
- Focus on business rule enforcement

Suggest missing test scenarios, then implement them.
```

---

## Prompt 9: Property-Based Testing for Business Rules

```
Create property-based tests for Order aggregate business rules.

**Your task:**
1. Identify invariants that should always hold:
   - Order total = sum of (item price * quantity) for all items
   - Status transitions only move forward in workflow
   - Adding duplicate product IDs consolidates into single item
   - Domain events raised for every state change
2. Generate property tests using FsCheck or similar (if available), or create parameterized tests with [DataRow]
3. Create diverse test data:
   - Random product IDs, prices, quantities
   - Various order states
   - Multiple items with edge case values
4. Verify invariants hold across many random inputs:
   - Order total calculation correct
   - State machine integrity maintained
   - Item consolidation logic correct
   - Events properly raised

**Test structure:**
- Use [DataRow] with 10+ different input combinations
- Test invariants with random-like data
- Verify properties hold regardless of input
- Focus on business rules that must never break

Generate property-based test suite for critical business rules.
```

---

## Prompt 10: Performance Test Suite with Benchmarks

```
Create performance tests for Catalog API query operations.

**Your task:**
1. Analyze query-heavy endpoints:
   - GET /api/catalog/items (pagination, filtering)
   - GET /api/catalog/items/{id}
   - Semantic search endpoints
2. Identify performance scenarios:
   - Small dataset (10 items)
   - Medium dataset (1000 items)
   - Large dataset (10000 items)
   - Complex filters
   - Concurrent requests
3. Generate test data:
   - Seed database with various dataset sizes
   - Realistic product data
4. Measure and assert:
   - Response times (p50, p95, p99)
   - Database query count (no N+1 queries)
   - Memory allocations
   - Concurrent request handling
5. Use BenchmarkDotNet or simple stopwatch timing

**Performance targets:**
- Single item retrieval: < 50ms
- Paginated list (20 items): < 200ms
- No N+1 queries (verify with logging)
- 100 concurrent requests: < 1s average

Generate performance test suite with realistic scenarios.
```

---

## Effectiveness Tips

**Let AI Discover Test Cases:**
- Don't specify every test - ask Claude to analyze code and suggest scenarios
- Request edge case identification based on code analysis
- Let AI generate realistic test data appropriate for business domain

**Comprehensive Coverage:**
- Ask for "comprehensive test suite" not individual tests
- Request tests for: happy path, edge cases, error scenarios, concurrency
- Include: validation, state transitions, integration points, performance

**Test Data Generation:**
- Request "realistic test data" - AI will generate domain-appropriate values
- Ask for boundary values, invalid inputs, edge cases automatically
- Let AI create diverse scenarios (empty, single, many, max values)

**Integration with Existing Code:**
- Always reference existing test files to follow patterns
- Request use of existing fixtures (CatalogApiFixture, test helpers)
- Ask AI to match coding style and assertion patterns

**Iterative Approach:**
- Start with "suggest test scenarios" before implementation
- Review suggestions, add missing scenarios
- Then request full implementation
- This ensures nothing is missed

**What Makes These Prompts Effective:**
1. **Analysis-first approach** - AI examines code to discover what to test
2. **Edge case discovery** - AI identifies scenarios you might miss
3. **Realistic test data** - AI generates appropriate values for domain
4. **Complete coverage** - Asks for comprehensive suites, not single tests
5. **Pattern matching** - AI follows existing test conventions automatically
