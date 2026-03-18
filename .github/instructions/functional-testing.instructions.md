---
description: Guidelines for writing functional (integration) tests against HTTP API endpoints in the eShop project
applyTo: 'tests/**FunctionalTests/**'
---

# Functional Test Guidelines

## Project Setup

- Use `Aspire.AppHost.Sdk` as the project SDK (see `Catalog.FunctionalTests.csproj`).
- Reference the API project with `IsAspireProjectResource="false"`.
- Add packages: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.TestHost`, `Aspire.Hosting.PostgreSQL`, `Asp.Versioning.Http.Client`, `xunit.v3.mtp-v2`.

## Fixture

- Each API test project has one shared `WebApplicationFactory<Program>` fixture that spins up real infrastructure (e.g. PostgreSQL via Aspire).
- The fixture implements `IAsyncLifetime` for async startup/teardown.
- Override `CreateHost` to inject connection strings and register the `AutoAuthorizeStartupFilter`.
- Always provide `Identity:Url` and `Identity:Audience` in the in-memory config so `AddDefaultAuthentication` does not skip auth registration:

```csharp
config.AddInMemoryCollection(new Dictionary<string, string>
{
    { $"ConnectionStrings:{Postgres.Resource.Name}", _postgresConnectionString },
    { "Identity:Url", "http://localhost" },
    { "Identity:Audience", "<service-name>" },
});
```

## Authentication in Tests

- Add an `AutoAuthorizeMiddleware` class that injects a fake `ClaimsIdentity` so every request is treated as authenticated:

```csharp
class AutoAuthorizeMiddleware
{
    public const string IDENTITY_ID = "9e3163b9-1ae6-4652-9dc6-7898ab7b7a00";

    private readonly RequestDelegate _next;

    public AutoAuthorizeMiddleware(RequestDelegate rd) => _next = rd;

    public async Task Invoke(HttpContext httpContext)
    {
        var identity = new ClaimsIdentity("cookies");
        identity.AddClaim(new Claim("sub", IDENTITY_ID));
        identity.AddClaim(new Claim("unique_name", IDENTITY_ID));
        identity.AddClaim(new Claim(ClaimTypes.Name, IDENTITY_ID));
        httpContext.User.AddIdentity(identity);
        await _next.Invoke(httpContext);
    }
}
```

- Register it in the fixture via `IStartupFilter` so it runs before the built-in auth middleware.

## Test Class Structure

- One test class per API area (e.g. `CatalogItemLikeTests`), injecting the shared fixture via `IClassFixture<TFixture>`.
- Store a `WebApplicationFactory<Program>` field and a shared `JsonSerializerOptions` with `JsonSerializerDefaults.Web`.
- Provide a private `CreateHttpClient(ApiVersion)` factory method using `ApiVersionHandler` + `QueryStringApiVersionWriter`.

```csharp
public sealed class MyFeatureTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public MyFeatureTests(CatalogApiFixture fixture) => _webApplicationFactory = fixture;

    private HttpClient CreateHttpClient(ApiVersion apiVersion)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), apiVersion);
        return _webApplicationFactory.CreateDefaultClient(handler);
    }
}
```

## Test Case Conventions

- Use `[Theory]` + `[InlineData(1.0)]` / `[InlineData(2.0)]` to cover all supported API versions.
- For tests that modify state (POST, PUT, DELETE), use **distinct item IDs per version variant** to prevent cross-test interference since the database is shared:

```csharp
[Theory]
[InlineData(1.0, 10)]
[InlineData(2.0, 11)]
public async Task ToggleItemLike_LikesItem(double version, int itemId) { ... }
```

- Always pass `TestContext.Current.CancellationToken` to every async call (`GetAsync`, `ReadAsStringAsync`, `SaveChangesAsync`, etc.).
- Call `response.EnsureSuccessStatusCode()` before deserializing the body.
- Deserialize the response body with the shared `_jsonSerializerOptions`.
- For multi-step flows (Act-1 → Act-2 → Assert), use inline comments like `// Act - 1`, `// Act - 2`, `// Assert - 1`.

## Naming

- Test method names follow the pattern: `MethodUnderTest_StateOrScenario_ExpectedBehavior`.
  - Examples: `GetItemLikes_ReturnsZeroForItemWithNoLikes`, `ToggleItemLike_LikesItem`, `ToggleItemLike_TogglesLikeOff`.

## Global Usings

Keep a `GlobalUsings.cs` with the common namespaces for the test project:

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http;
global using System.Security.Claims;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Xunit;
```
