# Tooling Generation Examples for eShop

**Context**: These prompts demonstrate how to create automation scripts, code generators, testing tools, and DevOps utilities that streamline development workflows and reduce repetitive tasks in the eShop microservices application.

**Category**: Tooling & Automation
**Focus**: Developer productivity, automation, testing infrastructure

---

## Prompt 1: Database Migration Rollback Script

```
Create a Bash script scripts/rollback-migrations.sh that safely rolls back EF Core migrations across eShop databases.

**Requirements:**
- Parameters: --target-migration, --database (all|catalog|ordering|identity|webhooks), --dry-run, --force
- Handle all four databases: catalogdb, orderingdb, identitydb, webhooksdb
- Show diff between current and target migration before applying
- Create backup SQL script before rollback: backups/rollback-{database}-{timestamp}.sql
- Verify success by querying __EFMigrationsHistory table
- Prompt for confirmation unless --force provided
- Support connection string override via environment variables
- Log operations to logs/migration-rollback-{timestamp}.log

**Safety features:**
- Dry-run mode to preview changes
- Automatic backup before rollback
- Verification step after rollback
- Clear error messages with recovery instructions

Include usage examples for common scenarios (rollback all databases, single database, dry-run).
```

---

## Prompt 2: Integration Event Generator from JSON Schema

```
Create a PowerShell script tools/generate-integration-event.ps1 that generates IntegrationEvent classes and handlers from JSON schema definitions.

**Input JSON schema format:**
{
  "eventName": "OrderStatusChangedToShipped",
  "properties": [
    {"name": "orderId", "type": "int", "required": true},
    {"name": "buyerName", "type": "string", "required": true},
    {"name": "orderDate", "type": "DateTime", "required": true}
  ],
  "targetService": "Ordering.API"
}

**Generated artifacts:**
1. Event class: {Service}/Application/IntegrationEvents/Events/{EventName}IntegrationEvent.cs
   - Record type with init-only properties
   - Inherits from IntegrationEvent
   - XML documentation
   - Follow patterns from existing events
2. Handler class: {Service}/Application/IntegrationEvents/EventHandling/{EventName}IntegrationEventHandler.cs
   - Implements IIntegrationEventHandler<T>
   - ILogger dependency injection
   - TODO comments for business logic
3. Update Extensions.cs with handler registration
4. Test stub: tests/{Service}.UnitTests/IntegrationEvents/{EventName}IntegrationEventHandlerTests.cs

**Output:**
- Summary of generated files
- Next steps checklist
- Example schema files for common patterns

Reduces boilerplate for event-driven architecture, ensures consistency across services.
```

---

## Prompt 3: Multi-Database Backup Script

```
Create a Bash script scripts/backup-databases.sh that backs up all PostgreSQL databases from the running Aspire application.

**Requirements:**
- Parameters: --format (sql|custom|directory), --output-dir, --compress, --aspire-container
- Discover PostgreSQL container from Aspire: docker ps with label com.docker.compose.project=aspire
- Backup each database to: backups/{YYYY-MM-DD}/{database}-{timestamp}.sql
- Use pg_dump options: --clean --if-exists --create --no-owner --no-acl
- If --compress: gzip files and create tar archive eShop-backup-{timestamp}.tar.gz

**Additional features:**
- Generate manifest.json with metadata (sizes, checksums, timestamps, PG version)
- Verify backup integrity by testing restore to temporary database
- Upload to S3 if AWS_S3_BACKUP_BUCKET environment variable set
- Retention policy: daily (7 days), weekly (4 weeks), monthly (6 months)
- Send notification to BACKUP_NOTIFICATION_URL on success/failure
- Verbose logging to logs/backup-{timestamp}.log

**Include:**
- Restoration guide: backups/RESTORE.md with step-by-step instructions
- Usage examples for different backup scenarios

Production-ready backup solution with verification and monitoring.
```

---

## Prompt 4: Test Data Generator for Load Testing

```
Create a C# console application tools/TestDataGenerator/ that generates realistic test data for load testing eShop.

**Command-line arguments (System.CommandLine):**
- --connection-string, --catalog-items (default: 1000), --users (default: 100)
- --orders (default: 500), --output-format (database|json|sql), --seed

**Data generation:**
1. Catalog data (Catalog.API):
   - Realistic product names from catalog.json patterns
   - Categories: Electronics, Fashion, Books, Home, Sports
   - Prices: $5-500 with realistic distribution
   - Stock: 0-1000 (some out-of-stock)
   - Insert via CatalogContext
2. User data (Identity.API):
   - Email: testuser{id}@eshop.com
   - Hashed passwords using PasswordHasher<ApplicationUser>
   - Complete address data
   - Insert via ApplicationDbContext
3. Order data (Ordering.API):
   - 1-5 items per order
   - Status distribution: Submitted(40%), Paid(30%), Shipped(20%), Cancelled(10%)
   - Dates within last 6 months
   - Follow DDD patterns from Order aggregate
   - Use OrderingContext

**Features:**
- Progress bar using Spectre.Console
- Validate data integrity (foreign keys, business rules)
- Performance target: 10,000 orders in < 60 seconds
- Output summary statistics

**Alternative output formats:**
- JSON: data files for import
- SQL: INSERT statements

Usage: dotnet run --project tools/TestDataGenerator -- --catalog-items 5000 --orders 2000
```

---

## Prompt 5: k6 Load Testing Script for Order Flow

```
Create a k6 load testing script tests/LoadTests/order-complete-flow.js that simulates complete order checkout process.

**Test scenarios:**
- Smoke: 1 VU for 30s
- Load: ramp to 50 VUs over 2min, sustain 5min, ramp down
- Stress: ramp to 200 VUs over 5min, sustain until failure
- Spike: sudden jump from 10 to 100 VUs

**User journey:**
1. GET /api/catalog/items - browse products (cache response)
2. POST /api/basket/{userId}/items - add 2-3 items
3. GET /api/basket/{userId} - view basket
4. POST /api/orders - submit order with address
5. GET /api/orders/{orderId} - verify order created

**Authentication:**
- POST /connect/token to get JWT
- Include Bearer token in authenticated requests

**Service endpoints (parameterized):**
- catalogApi: http://localhost:5301
- basketApi: http://localhost:5118
- orderingApi: http://localhost:5274
- identityApi: http://localhost:5223

**Custom metrics:**
- order_creation_duration: basket to order confirmation time
- basket_to_order_success_rate: conversion percentage
- catalog_cache_hit_rate: Redis cache effectiveness

**Thresholds (fail if exceeded):**
- http_req_duration: p95 < 500ms
- http_req_failed: < 1%
- order_creation_duration: p99 < 2000ms

**Additional features:**
- Setup/teardown: create/clean test users and orders
- HTML report: tests/LoadTests/reports/order-flow-{timestamp}.html
- CI integration: export to InfluxDB if INFLUX_URL set
- Realistic data using SharedArray

Run: k6 run --vus 50 --duration 5m tests/LoadTests/order-complete-flow.js
```

---

## Prompt 6: VS Code Tasks for Common Workflows

```
Create .vscode/tasks.json with common eShop development workflows.

**Build tasks:**
- "Build: All Services" - builds eShop.Web.slnf (Ctrl+Shift+B default)
- Problem matcher: $msCompile

**Run tasks:**
- "Run: Aspire AppHost" - runs AppHost, opens dashboard URL automatically
- "Run: Aspire AppHost (HTTP)" - with ESHOP_USE_HTTP_ENDPOINTS=1 for Playwright tests

**Test tasks:**
- "Test: Run All Tests" - executes dotnet test eShop.Web.slnf (default test task)
- "Test: Run Tests for Current File" - uses ${file} variable

**Database tasks:**
- "Database: Apply Migrations" - composite task for all databases with confirmation
- "Database: Reset Databases" - drops/recreates all databases (dangerous flag)

**Docker tasks:**
- "Docker: Cleanup Containers" - removes containers and volumes, cleans .docker-volumes/

**Tool tasks:**
- "Generate: Integration Event" - prompts for details, runs generation script
- "Analyze: Health Check" - runs health check script with colored output

**Also create:**
- .vscode/launch.json with debug configurations:
  - "Debug: Aspire AppHost" - attaches to all services
  - "Debug: Catalog.API", "Debug: Ordering.API" - individual services
  - "Debug: Current Test" - debugs test in active file
- .vscode/settings.json with recommended extensions
- .vscode/extensions.json recommending: C# Dev Kit, Aspire, REST Client

Provides one-click access to common workflows, improves developer onboarding.
```

---

## Prompt 9: Database Seed Data Script

```
Create a PowerShell script scripts/seed-data.ps1 that populates eShop databases with realistic demo data.

**Requirements:**
- Parameters: --environment (Development|Staging|Production), --reset (drops existing data)
- Seed data for all services:
  - Catalog: 50 products across 5 categories with realistic names, descriptions, prices, stock
  - Identity: 10 demo users (demouser1@eshop.com, password: Pass@word1)
  - Orders: 20 historical orders with various statuses
  - Webhooks: 3 webhook subscriptions for testing

**Data characteristics:**
- Use catalog.json patterns for product names
- Realistic prices ($5-500)
- Stock levels: varied (in-stock, low-stock, out-of-stock)
- Orders distributed over last 30 days
- Order statuses: mix of Submitted, Paid, Shipped, Delivered

**Safety:**
- Warn if --reset used on non-Development environment
- Confirm before dropping existing data
- Validate data integrity after seeding (foreign keys, business rules)

**Output:**
- Summary of seeded records per entity type
- List of demo user credentials
- Sample API calls to test seeded data

Usage for developer onboarding and demo environments.
```

---
---

## Effectiveness Tips

**Script Design:**
- Provide dry-run modes for destructive operations
- Include verbose/debug modes for troubleshooting
- Use clear parameter names with defaults
- Log operations with timestamps
- Support both interactive and non-interactive modes (CI/CD)

**Code Generators:**
- Follow existing code patterns and conventions
- Generate compilable code with proper namespaces
- Include XML documentation in generated code
- Provide test stubs alongside generated code
- Use Roslyn source generators when appropriate

**Database Tools:**
- Always backup before migrations or data operations
- Verify data integrity after operations (checksums, foreign keys)
- Include rollback procedures
- Use transactions for multi-step operations
- Support connection strings from environment variables

**Testing Tools:**
- Generate realistic test data respecting domain rules
- Include happy path and error scenarios
- Make tests independent and parallelizable
- Provide clear assertions with meaningful messages
- Support test data cleanup

**DevOps Tools:**
- Make workflows idempotent (safe to run multiple times)
- Include timeout limits to prevent hanging
- Provide clear error messages with actionable steps
- Cache dependencies (NuGet, Docker layers)
- Upload artifacts for failed runs

**Developer Productivity:**
- Document scripts with usage examples
- Provide VS Code tasks and shortcuts
- Include error handling with helpful messages
- Make tools discoverable (README, --help, task explorer)
- Support multiple output formats (console, JSON, markdown)

**Best Practices:**
- Write idiomatic code for each language (PowerShell, Bash, C#)
- Handle errors gracefully
- Follow security best practices (no hardcoded secrets, use env vars)
- Test on both Windows and Linux for cross-platform compatibility
- Make tools composable (output of one is input to another)
