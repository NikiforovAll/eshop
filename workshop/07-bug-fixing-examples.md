# Bug Fixing - Example Prompts for eShop

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

---

### Prompt 1: Race Condition in Catalog Stock Management

Fix the race condition in src\Catalog.API\Model\CatalogItem.cs RemoveStock method that causes negative inventory under concurrent order processing.

SYMPTOMS:
- AvailableStock becomes negative (e.g., -5, -12) after multiple rapid orders
- Exception logged: "Empty stock, product item 'Gaming Mouse' is sold out" but stock shows -7
- Occurs when 5+ concurrent orders attempt to purchase the same item
- Database shows CatalogItem records with AvailableStock < 0

EDGE CASES TO TEST:
- 100 concurrent orders for item with AvailableStock = 50 (50 should succeed, 50 should fail gracefully)
- Order quantity exceeds AvailableStock during concurrent access
- Multiple products in single order under concurrent modification
- Stock replenishment (AddStock) concurrent with RemoveStock


---

### Prompt 2: Memory Leak in OrderProcessor Background Service
Fix the memory leak in src\OrderProcessor\Services\GracePeriodManagerService.cs where IEventBus event handlers are never unsubscribed, causing memory to grow unbounded.

SYMPTOMS:
- OrderProcessor memory grows from 80MB to 3.5GB over 48 hours in production
- Application crashes with OutOfMemoryException after 2-3 days of uptime
- dotMemory profiler shows thousands of GracePeriodManagerService instances retained
- Event handler delegate count increases linearly with processed orders
- GC collections occur frequently but memory is not reclaimed

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

---

### Prompt 3: N+1 Query Problem in Orders API
Fix the N+1 query performance issue in src\Ordering.API\Apis\OrderServices.cs GetAllOrders endpoint that causes 500+ database queries and 15-second response times.

SYMPTOMS:
- GET /api/orders endpoint takes 12-18 seconds with 100 orders
- Application Insights shows 503 SQL queries for single API request
- Database logs show repeated queries: "SELECT * FROM OrderItems WHERE OrderId = @p0"
- Entity Framework logs: "Executing DbCommand [CommandType='Text'] SELECT [o].[Id]..." (executed 100+ times)
- CPU spikes to 80% on Ordering.API pods during order list retrieval
- Users report "Orders page extremely slow to load"

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

**Why this works**: Uses specific performance metrics (15 seconds, 500+ queries), shows actual EF Core log patterns developers see, explains N+1 problem with calculation (1 + 100 + 100), provides exact code fix with Include/AsSplitQuery, and defines measurable success criteria (< 500ms, 2-3 queries).
