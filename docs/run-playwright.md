# Running Playwright E2E Tests

## Prerequisites

- Node.js installed (`node`, `npm`)
- .NET Aspire CLI (`dotnet aspire`)
- Playwright browsers installed (see below)

## One-time setup

### Create `.env`

Playwright loads `.env` from the repo root (via `dotenv`). Create it with the seeded test credentials:

```bash
printf 'USERNAME1=alice\nPASSWORD=Pass123$\nESHOP_USE_HTTP_ENDPOINTS=1\n' > .env
```

> `ESHOP_USE_HTTP_ENDPOINTS=1` is critical. Without it, Aspire starts the WebApp on HTTPS (port 7298) but Playwright expects `http://localhost:5045`. This env var is read by `eShop.AppHost/Program.cs` and switches all endpoints to HTTP.

## Running the tests (local)

### Step 1 — Start the app

```bash
ESHOP_USE_HTTP_ENDPOINTS=1 dotnet aspire run
```

Wait until the Aspire dashboard reports all services healthy (postgres migrations run, WebApp is up). This typically takes 30–60 seconds on first run, faster on subsequent runs since postgres/redis containers are persistent.

### Step 2 — Run tests

Once the app is running on `http://localhost:5045`, Playwright will reuse it (`reuseExistingServer: true` for local runs):

```bash
# Login setup (saves auth session to playwright/.auth/user.json)
npx playwright test --project setup

# Run all authenticated tests
npx playwright test --project "e2e tests logged in"

# Run a single test file
npx playwright test TestName --project "e2e tests logged in"

# Run all tests (setup + authenticated + anonymous)
npx playwright test
```

## Test projects

| Project | Tests | Auth required |
|---|---|---|
| `setup` | `login.setup.ts` | — (performs login) |
| `e2e tests logged in` | `AddItemTest`, `RemoveItemTest` | Yes (uses saved session) |
| `e2e tests without logged in` | `BrowseItemTest` | No |

## Viewing results

```bash
npx playwright show-report
```

## Troubleshooting

### WebApp not reachable on port 5045
- Confirm `.env` contains `ESHOP_USE_HTTP_ENDPOINTS=1`.
- Confirm `dotnet aspire run` was started with that variable set (prefix in shell or export it, dotenv only affects Playwright's own process).
- Check the Aspire dashboard — if WebApp shows unhealthy, wait or check logs.

### Login setup fails
- Verify `USERNAME1=alice` and `PASSWORD=Pass123$` in `.env` match the seeded users in `src/Identity.API/UsersSeed.cs`.
- The Identity service must be fully started before the setup test runs.

### `address already in use` errors in webServer output
- This happens when Playwright tries to start a second AppHost instance while one is already running. It is harmless — Playwright detects `http://localhost:5045` is responsive and proceeds (`reuseExistingServer`).
