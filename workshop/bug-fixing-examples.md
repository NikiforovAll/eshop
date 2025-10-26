# Bug Fixing - Example Prompts for eShop

## Context
These prompts demonstrate bug fixing techniques for the eShop reference application - a .NET 9 microservices-based e-commerce system using .NET Aspire, Domain-Driven Design, event-driven architecture with RabbitMQ, and distributed data stores (PostgreSQL, Redis).

**Category**: Bug Fixing
**Sub Category**: Distributed Systems, Microservices, Concurrency, Performance, Integration

---

### Prompt 1: Race Condition in Catalog Stock Management
```
Fix the race condition in src\Catalog.API\Model\CatalogItem.cs RemoveStock method that causes negative inventory under concurrent order processing.

SYMPTOMS:
- AvailableStock becomes negative (e.g., -5, -12) after multiple rapid orders
- Exception logged: "Empty stock, product item 'Gaming Mouse' is sold out" but stock shows -7
- Occurs when 5+ concurrent orders attempt to purchase the same item
- Database shows CatalogItem records with AvailableStock < 0

ROOT CAUSE:
The RemoveStock method performs read-modify-write without optimistic concurrency control. Between checking AvailableStock and decrementing it, another transaction can modify the same row, leading to lost updates.

FIX APPROACH:
1. Add RowVersion/Timestamp column to CatalogItem entity for optimistic concurrency
2. Update src\Catalog.API\Infrastructure\EntityConfigurations\CatalogItemEntityTypeConfiguration.cs to configure concurrency token
3. Add retry logic with exponential backoff to handle DbUpdateConcurrencyException
4. Update src\Catalog.API\Apis\CatalogApi.cs endpoints that modify stock

VERIFICATION:
1. Run tests\Catalog.FunctionalTests\CatalogApiTests.cs with added concurrent order test
2. Create load test: 10 concurrent requests removing stock from same CatalogItem
3. Verify AvailableStock never goes negative
4. Check Application Insights logs for DbUpdateConcurrencyException (should retry and succeed)
5. Confirm stock changes are atomic across concurrent requests

EDGE CASES TO TEST:
- 100 concurrent orders for item with AvailableStock = 50 (50 should succeed, 50 should fail gracefully)
- Order quantity exceeds AvailableStock during concurrent access
- Multiple products in single order under concurrent modification
- Stock replenishment (AddStock) concurrent with RemoveStock
```
**Why this works**: Provides specific file paths, concrete symptoms with actual error messages and data states, explains the technical root cause (read-modify-write race), prescribes the standard solution (optimistic concurrency with RowVersion), and includes comprehensive verification steps with measurable outcomes.

---

### Prompt 2: Memory Leak in OrderProcessor Background Service
```
Fix the memory leak in src\OrderProcessor\Services\GracePeriodManagerService.cs where IEventBus event handlers are never unsubscribed, causing memory to grow unbounded.

SYMPTOMS:
- OrderProcessor memory grows from 80MB to 3.5GB over 48 hours in production
- Application crashes with OutOfMemoryException after 2-3 days of uptime
- dotMemory profiler shows thousands of GracePeriodManagerService instances retained
- Event handler delegate count increases linearly with processed orders
- GC collections occur frequently but memory is not reclaimed

ROOT CAUSE:
The GracePeriodManagerService subscribes to IEventBus events (OrderStartedIntegrationEvent, PaymentSucceededIntegrationEvent) in the constructor but never unsubscribes. Each event subscription creates a strong reference to the service instance, preventing garbage collection even after the service should be disposed. The EventBusRabbitMQ implementation maintains a static event handler collection that retains all subscribers.

FIX APPROACH:
1. Implement IAsyncDisposable on GracePeriodManagerService
2. Store event subscription tokens/handles during Subscribe calls
3. In DisposeAsync, unsubscribe all event handlers from IEventBus
4. Ensure the background service's StopAsync calls DisposeAsync
5. Review src\EventBusRabbitMQ\EventBusRabbitMQ.cs for proper unsubscribe implementation
6. Add CancellationToken support to all async event handlers

VERIFICATION:
1. Run OrderProcessor locally with dotMemory profiler attached
2. Process 1000 orders and capture memory snapshot
3. Verify GracePeriodManagerService instance count remains stable (1-2 instances)
4. Check event handler delegate count in EventBusRabbitMQ (should not accumulate)
5. Run stress test: process 10,000 orders over 2 hours, memory should stabilize < 200MB
6. Add unit test in tests\Ordering.UnitTests for proper disposal pattern
7. Monitor GC heap size in Application Insights after deployment

EDGE CASES TO TEST:
- Service restart during active event processing (handlers should cleanup gracefully)
- Multiple OrderProcessor instances (Kubernetes replicas) subscribing to same events
- Exception thrown during event handling (should not leak subscription)
- Rapid start/stop cycles of the background service
```
**Why this works**: Identifies a specific class and architectural component (background service with event bus), describes memory growth with concrete metrics (80MB to 3.5GB), explains the garbage collection problem with technical detail (strong references preventing GC), provides the exact solution pattern (IAsyncDisposable with unsubscribe), and includes profiling-based verification steps.

---

### Prompt 3: N+1 Query Problem in Orders API
```
Fix the N+1 query performance issue in src\Ordering.API\Apis\OrderServices.cs GetAllOrders endpoint that causes 500+ database queries and 15-second response times.

SYMPTOMS:
- GET /api/orders endpoint takes 12-18 seconds with 100 orders
- Application Insights shows 503 SQL queries for single API request
- Database logs show repeated queries: "SELECT * FROM OrderItems WHERE OrderId = @p0"
- Entity Framework logs: "Executing DbCommand [CommandType='Text'] SELECT [o].[Id]..." (executed 100+ times)
- CPU spikes to 80% on Ordering.API pods during order list retrieval
- Users report "Orders page extremely slow to load"

ROOT CAUSE:
The GetAllOrders endpoint loads Order entities without eager loading related OrderItems collection. Entity Framework executes 1 query for orders list, then separate query for each Order's OrderItems (classic N+1 problem). With 100 orders averaging 5 items each, this becomes 1 + 100 + (100 * 1 for Address) = 201+ queries.

Current problematic code pattern:
```csharp
var orders = await dbContext.Orders
    .Where(o => o.BuyerId == buyerId)
    .ToListAsync();
// Later access to order.OrderItems triggers lazy loading
```

FIX APPROACH:
1. Add .Include(o => o.OrderItems) to the Orders query in src\Ordering.API\Apis\OrderServices.cs
2. Add .Include(o => o.Address) for the Address value object (EF Core owned entity)
3. Add .AsSplitQuery() if result set is large to avoid cartesian explosion
4. Consider projection to DTO to avoid loading unnecessary navigation properties
5. Add query logging in Development to catch future N+1 issues
6. Update src\Ordering.Infrastructure\OrderingContext.cs OnConfiguring to log slow queries

EXAMPLE FIXED CODE:
```csharp
var orders = await dbContext.Orders
    .Include(o => o.OrderItems)
    .Where(o => o.BuyerId == buyerId)
    .AsSplitQuery()
    .ToListAsync();
```

VERIFICATION:
1. Run tests\Ordering.FunctionalTests\OrderingApiTests.cs with EF query logging enabled
2. Verify total queries reduced from 200+ to 2-3 (1 for Orders, 1 for OrderItems, 1 for Address if needed)
3. Measure response time: should drop from 15s to < 500ms for 100 orders
4. Use SQL Profiler to capture exact query count during test execution
5. Run load test with Apache Bench: ab -n 100 -c 10 http://localhost:5001/api/orders
6. Check Application Insights query duration metrics (p95 < 1s)
7. Add automated performance test with query count assertion

EDGE CASES TO TEST:
- Orders with 0 OrderItems (empty collection should not cause issues)
- Orders with 50+ OrderItems (ensure AsSplitQuery prevents cartesian explosion)
- Pagination scenarios (Skip/Take should maintain efficient queries)
- Filtering by date range with includes (compound predicates)
```
**Why this works**: Uses specific performance metrics (15 seconds, 500+ queries), shows actual EF Core log patterns developers see, explains N+1 problem with calculation (1 + 100 + 100), provides exact code fix with Include/AsSplitQuery, and defines measurable success criteria (< 500ms, 2-3 queries).

---

### Prompt 4: gRPC Authentication Failure in Basket Service
```
Fix the JWT token validation failure in src\Basket.API\Grpc\BasketService.cs where authenticated requests are rejected with "Unauthenticated" error after 5 minutes.

SYMPTOMS:
- WebApp successfully authenticates user but gRPC calls to Basket.API fail after 5 minutes
- Error in logs: "RpcException: Status(StatusCode='Unauthenticated', Detail='The caller is not authenticated.')"
- context.GetUserIdentity() in BasketService returns null/empty for valid JWT tokens
- Works immediately after login, fails after token age > 5 minutes
- Issue does not occur with HTTP REST endpoints, only gRPC
- JWT token is present in gRPC metadata headers but not validated

ROOT CAUSE:
The Basket.API gRPC service is not properly configured to validate JWT tokens in gRPC calls. While HTTP endpoints use standard ASP.NET Core authentication middleware, gRPC services require explicit JWT bearer configuration. The token validation parameters might have incorrect issuer/audience, or the gRPC authentication middleware order is incorrect.

Current issue: AddServiceDefaults() configures authentication for HTTP but gRPC requires additional configuration in Program.cs.

FIX APPROACH:
1. Review src\Basket.API\Program.cs authentication configuration
2. Ensure AddAuthentication() is called before AddGrpc()
3. Configure JwtBearer options with correct Issuer, Audience, and TokenValidationParameters
4. Add .RequireAuthorization() to gRPC service endpoints if missing
5. Verify Identity.API is issuing tokens with correct claims (sub, aud, iss)
6. Check src\eShop.ServiceDefaults\Extensions.cs AddDefaultAuthentication method
7. Ensure gRPC metadata includes "authorization" header with "Bearer {token}" format

EXAMPLE CONFIGURATION:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Url"];
        options.Audience = "basket";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// Ensure gRPC uses authentication
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
}).AddServiceOptions<BasketService>(options =>
{
    options.Interceptors.Add<AuthenticationInterceptor>();
});
```

VERIFICATION:
1. Run tests\Basket.UnitTests\BasketServiceTests.cs with authentication context
2. Test gRPC call with valid JWT token: token age = 1 min, 5 min, 10 min, 29 min
3. Test with expired token (should get proper "token expired" error, not generic Unauthenticated)
4. Test with invalid signature (should reject)
5. Use grpcurl to manually test: grpcurl -H "authorization: Bearer {token}" ...
6. Check logs for JWT validation failures with detailed error messages
7. Verify context.GetUserIdentity() returns correct userId claim from token

EDGE CASES TO TEST:
- Token with missing "sub" claim
- Token issued by different authority (wrong issuer)
- Token with wrong audience claim
- Clock skew scenarios (token issued in future, slightly expired)
- Multiple concurrent gRPC calls with different tokens
- Token refresh during active gRPC streaming call
```
**Why this works**: Describes a time-based failure pattern (5 minutes) that points to token expiration/validation, contrasts gRPC vs HTTP behavior to isolate the issue, explains the architectural gap (gRPC needs explicit auth config), provides complete configuration code, and includes grpcurl for manual verification.

---

### Prompt 5: Event Ordering Issue Causing Order State Corruption
```
Fix the out-of-order event processing in src\Ordering.API that causes orders to get stuck in "AwaitingValidation" state despite payment success.

SYMPTOMS:
- 2-5% of orders remain in "AwaitingValidation" status indefinitely
- OrderStatusChangedToPaidIntegrationEvent arrives BEFORE OrderStatusChangedToAwaitingValidationIntegrationEvent
- Database shows Order.OrderStatus = AwaitingValidation but PaymentProcessor logs show successful payment
- Error in logs: "Cannot transition order from Paid to AwaitingValidation - invalid state transition"
- Orders are never shipped because OrderStatus is stuck
- Customers complain about paid orders not being processed

ROOT CAUSE:
RabbitMQ does not guarantee message order across different publishers/consumers, especially under load. The OrderingIntegrationEventService publishes events immediately after SaveChanges, but network delays or broker rebalancing can cause later events to arrive first. The Order aggregate's state machine properly validates transitions, but the application doesn't handle idempotent/out-of-order event replay.

Event flow:
1. Order created → OrderStartedIntegrationEvent ✓
2. Payment validates → OrderStatusChangedToAwaitingValidationIntegrationEvent (delayed in RabbitMQ)
3. Payment succeeds → OrderStatusChangedToPaidIntegrationEvent (arrives first) ✓
4. Delayed event arrives → State machine rejects transition ✗

FIX APPROACH:
1. Add sequence number or timestamp to IntegrationEvent base class in src\EventBus\IntegrationEvent.cs
2. Implement event versioning/ordering in src\Ordering.API\Application\IntegrationEvents\OrderingIntegrationEventService.cs
3. Store last processed event sequence number in Order aggregate or separate EventSequence table
4. Buffer out-of-order events and replay when gap is filled (event sourcing pattern)
5. Alternative: Make event handlers idempotent and state transitions check current state before applying
6. Update src\Ordering.Domain\AggregatesModel\OrderAggregate\Order.cs state transition methods to handle replays

EXAMPLE IDEMPOTENT STATE TRANSITION:
```csharp
public void SetPaidStatus()
{
    // Allow transition only from valid previous states
    if (OrderStatus.Id == OrderStatus.AwaitingValidation.Id ||
        OrderStatus.Id == OrderStatus.Paid.Id) // Already paid is OK (idempotent)
    {
        OrderStatus = OrderStatus.Paid;
        AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));
    }
    else
    {
        // Log warning but don't throw - event might be duplicate/out-of-order
        _logger.LogWarning("Order {OrderId} cannot transition to Paid from {CurrentStatus}",
            Id, OrderStatus.Name);
    }
}
```

VERIFICATION:
1. Add integration test that publishes events out of order
2. Simulate network delay: publish Paid event, sleep 100ms, publish AwaitingValidation event
3. Verify Order reaches Paid status regardless of event arrival order
4. Check Order.OrderStatus in database after both events processed
5. Add RabbitMQ chaos testing: random delays, consumer restarts during event processing
6. Monitor Application Insights for "invalid state transition" warnings (should decrease to 0)
7. Run tests\Ordering.UnitTests\Domain\OrderAggregateTest.cs with new out-of-order test cases

EDGE CASES TO TEST:
- 3+ events arriving in completely reversed order
- Duplicate event delivery (RabbitMQ redelivery after consumer crash)
- Events for non-existent Order (late event after Order deletion)
- Concurrent event processing on multiple OrderProcessor instances
- Event published but transaction rolled back (phantom event)
```
**Why this works**: Identifies a distributed systems problem (event ordering in message queue) with specific symptoms (2-5% stuck orders, specific state), explains RabbitMQ's lack of ordering guarantees, provides two solution approaches (sequence numbers or idempotent handlers), and includes realistic chaos testing scenarios.

---

### Prompt 6: Connection Pool Exhaustion in Ordering.API
```
Fix the connection pool exhaustion in src\Ordering.API causing "Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool" errors under load.

SYMPTOMS:
- Application crashes during peak traffic (500+ concurrent users)
- Exception: "System.InvalidOperationException: Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool. This may have occurred because all pooled connections were in use and max pool size was reached."
- Response times spike from 200ms to 30s before failure
- PostgreSQL shows only 15-20 active connections (well below max_connections = 100)
- Application Insights shows DbContext instances not being disposed
- Issue starts after ~50 concurrent requests, worsens rapidly

ROOT CAUSE:
DbContext instances are not properly disposed in some code paths, especially during exceptions or async operations. Connections are checked out from the pool but never returned, leading to pool exhaustion even though database server has capacity. Common causes:
- Missing using statements or try/finally blocks
- Async methods not awaited (fire-and-forget)
- Captured DbContext in closures/lambdas that outlive the request scope
- Long-running transactions not committed/rolled back

FIX APPROACH:
1. Audit all OrderingContext usage in src\Ordering.API\Apis\OrderServices.cs
2. Ensure all DbContext injections use dependency injection scoped lifetime (not manual instantiation)
3. Add explicit connection lifetime configuration in src\Ordering.API\Program.cs:
   ```csharp
   builder.Services.AddNpgsqlDbContext<OrderingContext>("orderingdb",
       configureDbContextOptions: options =>
       {
           options.UseNpgsql(options =>
           {
               options.EnableRetryOnFailure(3);
               options.CommandTimeout(30);
           });
       });
   ```
4. Review src\Ordering.API\Application\IntegrationEvents\OrderingIntegrationEventService.cs for captured DbContext
5. Add connection pooling metrics to Application Insights
6. Configure Npgsql connection string with explicit pool settings: "Maximum Pool Size=50;Minimum Pool Size=5;Connection Lifetime=600"

VERIFICATION:
1. Run load test with k6: 100 concurrent users for 5 minutes
2. Monitor PostgreSQL connection count: SELECT count(*) FROM pg_stat_activity WHERE datname = 'OrderingDB';
3. Verify connection pool metrics remain healthy (available connections > 0)
4. Check for DbContext disposal in logs (enable EnableSensitiveDataLogging in Development)
5. Run tests\Ordering.FunctionalTests\OrderingApiTests.cs with connection pool monitoring
6. Add health check that monitors connection pool status
7. Use dotMemory to verify DbContext instances are garbage collected after requests

LOAD TEST SCRIPT:
```javascript
import http from 'k6/http';
export let options = { vus: 100, duration: '5m' };
export default function() {
  http.get('http://localhost:5002/api/orders');
  http.post('http://localhost:5002/api/orders', JSON.stringify({...}));
}
```

EDGE CASES TO TEST:
- Exception thrown before SaveChangesAsync (connection should be released)
- Timeout on long-running query (connection should return to pool)
- Rapid connection open/close cycles (pool should reuse connections)
- Database restart during active connections (resilience testing)
- Multiple DbContext types in same service (separate pools)
```
**Why this works**: Provides the exact exception message developers see, explains the pool exhaustion paradox (connections in use but DB has capacity), identifies common coding patterns that cause leaks (missing disposal, fire-and-forget async), gives concrete configuration with connection string parameters, and includes a ready-to-run k6 load test script.

---

### Prompt 7: Distributed Transaction Failure in Event Publishing
```
Fix the data inconsistency issue where Order state changes are committed to database but corresponding IntegrationEvents are never published to RabbitMQ, leaving the system in inconsistent state.

SYMPTOMS:
- Orders exist in database with Paid status but PaymentProcessor never receives OrderStatusChangedToPaidIntegrationEvent
- Inventory is not decremented in Catalog.API despite successful order creation
- Event log table (IntegrationEventLog) shows events stuck in "InProgress" or "NotPublished" state
- ~1% of transactions under load result in missing events
- RabbitMQ logs show connection drops: "Connection lost: broker forced connection closure"
- Manual database inspection shows Order saved but corresponding event in EventLog table has EventStateId = 1 (NotPublished)

ROOT CAUSE:
The outbox pattern implementation in src\IntegrationEventLogEF\Services\IntegrationEventLogService.cs and src\Ordering.API\Application\IntegrationEvents\OrderingIntegrationEventService.cs has a race condition. The pattern:
1. SaveChanges (Order + EventLog in same transaction) ✓
2. Publish event to RabbitMQ ✗ (happens AFTER transaction commit)

If RabbitMQ connection fails or application crashes between step 1 and 2, events are lost. The retry mechanism doesn't reliably recover these orphaned events.

Current flow:
```csharp
await _orderingContext.SaveChangesAsync(); // Commits transaction
await _eventBus.PublishAsync(integrationEvent); // Can fail, transaction already committed
```

FIX APPROACH:
1. Implement reliable event publishing using the outbox pattern correctly:
   - Save Order + IntegrationEventLog in single transaction ✓
   - Separate background worker polls EventLog for NotPublished events
   - Publish to RabbitMQ
   - Update EventLog to Published only after RabbitMQ confirms
2. Add retry logic with exponential backoff in src\IntegrationEventLogEF\Services\IntegrationEventLogService.cs
3. Update src\Ordering.API\Application\IntegrationEvents\OrderingIntegrationEventService.cs to mark events as "InProgress" before publishing
4. Create background service EventPublisherBackgroundService that:
   - Queries EventLog for NotPublished or InProgress (stale) events every 30 seconds
   - Attempts to publish to RabbitMQ
   - Handles RabbitMQ connection failures gracefully
5. Add distributed tracing correlation ID to link database transaction with event publishing

EXAMPLE BACKGROUND SERVICE:
```csharp
public class EventPublisherBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var unpublishedEvents = await _eventLogService.RetrieveEventLogsPendingToPublishAsync();
                foreach (var evt in unpublishedEvents)
                {
                    await _eventLogService.MarkEventAsInProgressAsync(evt.EventId);
                    await _eventBus.PublishAsync(evt.IntegrationEvent);
                    await _eventLogService.MarkEventAsPublishedAsync(evt.EventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing pending events");
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

VERIFICATION:
1. Add integration test that simulates RabbitMQ failure between SaveChanges and PublishAsync
2. Use Testcontainers to stop RabbitMQ container mid-transaction
3. Verify events are eventually published when RabbitMQ comes back online
4. Query IntegrationEventLog table: SELECT * FROM IntegrationEventLog WHERE StateId = 1 (should be 0)
5. Run chaos test: random RabbitMQ restarts during order creation load test
6. Add Application Insights metric for event publishing lag (time between SaveChanges and Published state)
7. Monitor RabbitMQ management UI: confirm message count matches orders created

EDGE CASES TO TEST:
- RabbitMQ unavailable for 5 minutes (events should queue and publish when available)
- Application crashes after SaveChanges, before publish (background service should recover on restart)
- Duplicate event publish attempts (should be idempotent on consumer side)
- Very high event volume (background service should not fall behind)
- Database available but RabbitMQ down (eventual consistency should be maintained)
```
**Why this works**: Identifies a classic distributed systems problem (two-phase commit across database and message broker) with concrete symptoms (events stuck in NotPublished state), explains the exact failure point in the transaction flow, provides the correct outbox pattern implementation with background worker, and includes chaos engineering verification steps.

---

### Prompt 8: CORS Policy Blocking WebApp to Basket.API gRPC Calls
```
Fix the CORS policy error preventing WebApp (Blazor) from making gRPC-Web calls to Basket.API, causing "Access to fetch at 'https://localhost:5001/basket.Basket/GetBasket' from origin 'https://localhost:5002' has been blocked by CORS policy".

SYMPTOMS:
- Browser console error: "Access to fetch at 'https://localhost:5001/basket.Basket/GetBasket' from origin 'https://localhost:5002' has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present"
- gRPC-Web calls from Blazor WebApp to Basket.API fail immediately
- HTTP REST calls to same service work fine (only gRPC-Web affected)
- Issue only occurs in browser (Blazor WebAssembly), not in tests or backend services
- Network tab shows OPTIONS preflight request returns 405 Method Not Allowed
- gRPC service responds with 200 OK when called from grpcurl but 0 (CORS error) from browser

ROOT CAUSE:
gRPC-Web requires special CORS configuration because it uses HTTP/2 and custom headers (grpc-*). Standard ASP.NET Core CORS middleware is configured in src\Basket.API\Program.cs but does not expose required gRPC-Web headers or allow gRPC methods. The middleware order might also be incorrect - CORS must come before UseGrpcWeb().

Missing headers:
- Access-Control-Expose-Headers: grpc-status, grpc-message, grpc-encoding, grpc-accept-encoding
- Access-Control-Allow-Headers: x-grpc-web, content-type, authorization

FIX APPROACH:
1. Update src\Basket.API\Program.cs to add comprehensive CORS policy:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowWebApp", policy =>
       {
           policy.WithOrigins("https://localhost:5002", "http://localhost:5002")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("grpc-status", "grpc-message", "grpc-encoding", "grpc-accept-encoding")
               .AllowCredentials();
       });
   });
   ```
2. Ensure middleware order is correct:
   ```csharp
   app.UseCors("AllowWebApp"); // MUST be before UseGrpcWeb
   app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
   app.MapGrpcService<BasketService>().EnableGrpcWeb().RequireCors("AllowWebApp");
   ```
3. Update src\eShop.AppHost\Program.cs to configure WebApp URL as allowed origin for all gRPC services
4. Add CORS configuration to src\eShop.ServiceDefaults\Extensions.cs if this should be standard across all services
5. Test with browser DevTools Network tab to verify preflight OPTIONS request succeeds

VERIFICATION:
1. Open browser DevTools Network tab
2. Load WebApp at https://localhost:5002 and trigger basket operation
3. Verify OPTIONS preflight request returns 200 OK with headers:
   - Access-Control-Allow-Origin: https://localhost:5002
   - Access-Control-Allow-Methods: POST, OPTIONS
   - Access-Control-Expose-Headers: grpc-status, grpc-message
4. Verify subsequent gRPC-Web POST request succeeds (status code 200)
5. Check no CORS errors in browser console
6. Test from different origin (http vs https) to ensure both work
7. Add automated browser test using Playwright in tests\WebApp.FunctionalTests

EXAMPLE PLAYWRIGHT TEST:
```csharp
[Test]
public async Task GrpcWebCall_ShouldNotHaveCorsError()
{
    await Page.GotoAsync("https://localhost:5002");
    await Page.ClickAsync("text=View Basket");

    var consoleErrors = new List<string>();
    Page.Console += (_, msg) => { if (msg.Type == "error") consoleErrors.Add(msg.Text); };

    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    Assert.That(consoleErrors, Has.None.Contains("CORS"));
}
```

EDGE CASES TO TEST:
- WebApp deployed to different domain (production scenario)
- Multiple WebApp origins (dev, staging, prod)
- gRPC streaming calls (bi-directional) with CORS
- Authentication tokens in CORS preflight (credentials: include)
- Different browsers (Chrome, Firefox, Safari - CORS behavior varies slightly)
```
**Why this works**: Provides the exact browser console error developers see, explains gRPC-Web's special CORS requirements (custom headers, HTTP/2), shows the critical middleware ordering (CORS before GrpcWeb), includes complete working configuration code, and provides a Playwright test to prevent regression.

---

## Effectiveness Tips

### When Reporting Bugs
- **Include exact error messages and stack traces** - Copy/paste from logs, not paraphrased
- **Provide reproduction steps with specific inputs** - "Create order with 5 items, product ID 123" not "create an order"
- **Show actual vs expected behavior with data** - "AvailableStock = -7 (expected >= 0)"
- **Specify when it started** - "After deploying v2.3.0" or "Under 100+ concurrent users"

### When Diagnosing Root Causes
- **Identify the exact file and method** - src\Ordering.API\Apis\OrderServices.cs line 42
- **Explain the mechanism of failure** - "Race condition between lines 42-45 where..."
- **Use profiling data when available** - dotMemory, Application Insights, SQL Profiler
- **Consider distributed system failure modes** - network partitions, clock skew, event ordering

### When Fixing Bugs
- **Apply proven patterns** - Optimistic concurrency, idempotency, circuit breaker, outbox pattern
- **Fix root cause, not symptoms** - Don't catch and swallow exceptions, fix the underlying issue
- **Add tests that fail without the fix** - TDD approach ensures fix actually works
- **Consider backwards compatibility** - Database migrations, API contracts, event schemas

### When Verifying Fixes
- **Create reproducible test case** - Unit test, integration test, or load test script
- **Measure before and after** - Response time, query count, memory usage, error rate
- **Test edge cases explicitly** - Concurrent access, large datasets, network failures
- **Add monitoring/alerting** - Application Insights metrics to catch regressions early
- **Run load tests** - Bugs often only appear under realistic traffic patterns

### Domain-Specific Tips

**For Concurrency Bugs:**
- Use optimistic concurrency (RowVersion) for high-read, low-write scenarios
- Use pessimistic locking (database locks) for critical inventory/financial operations
- Always test with concurrent requests (10+ simultaneous calls)

**For Memory Leaks:**
- Profile with dotMemory or PerfView before and after
- Check event subscriptions, timers, and static collections
- Ensure IDisposable/IAsyncDisposable is implemented and called

**For Performance Issues:**
- Enable EF Core query logging to identify N+1 problems
- Use Application Insights or SQL Profiler to measure actual database time
- Consider caching, pagination, and async operations

**For Distributed System Issues:**
- Implement idempotent operations (safe to retry/replay)
- Add distributed tracing (OpenTelemetry) to correlate cross-service requests
- Use outbox pattern for reliable event publishing
- Design for eventual consistency, not immediate consistency

**For Integration Failures:**
- Add circuit breaker pattern (Polly) for resilience
- Implement exponential backoff retry for transient failures
- Log correlation IDs across service boundaries
- Test with chaos engineering (Toxiproxy, Simmy)

---

## Additional Resources

**eShop Architecture Patterns:**
- Domain-Driven Design: src\Ordering.Domain\AggregatesModel
- Event-Driven: src\EventBus, src\EventBusRabbitMQ
- Outbox Pattern: src\IntegrationEventLogEF
- gRPC Services: src\Basket.API\Grpc
- Service Defaults: src\eShop.ServiceDefaults

**Testing Patterns:**
- Unit Tests: tests\Ordering.UnitTests, tests\Basket.UnitTests
- Functional Tests: tests\Ordering.FunctionalTests, tests\Catalog.FunctionalTests
- Test Fixtures with Aspire: CatalogApiFixture, OrderingApiFixture

**Monitoring and Diagnostics:**
- OpenTelemetry configured in eShop.ServiceDefaults
- Application Insights integration
- Health checks: /health and /alive endpoints
- Distributed tracing correlation
