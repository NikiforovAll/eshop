---
description: Implement an end-to-end feature in eShop (API + EF + UI)
---

Implement the following feature end-to-end in this eShop codebase:

**Feature:** ${input:feature_description}

---

## Codebase Map

Key files for reference:

- **API endpoints**: `src/Catalog.API/Apis/CatalogApi.cs` — minimal API, uses `[AsParameters] CatalogServices`
- **Domain models**: `src/Catalog.API/Model/` — entities go here
- **EF config**: `src/Catalog.API/Infrastructure/EntityConfigurations/` — one `IEntityTypeConfiguration<T>` per entity
- **DbContext**: `src/Catalog.API/Infrastructure/CatalogContext.cs`
- **Service config options**: `src/Catalog.API/CatalogOptions.cs` — add any configurable values here
- **appsettings**: `src/Catalog.API/appsettings.json` — must contain `Identity.Audience`, `Identity.Scopes` if any authorized endpoints are added
- **AppHost wiring**: `src/eShop.AppHost/Program.cs` — service references and env vars
- **Client service**: `src/WebAppComponents/Services/CatalogService.cs` + `ICatalogService.cs`
- **Client DTOs**: `src/WebAppComponents/Catalog/CatalogItem.cs`
- **Item page UI**: `src/WebApp/Components/Pages/Item/ItemPage.razor` + `ItemPage.razor.css`
- **WebApp DI**: `src/WebApp/Extensions/Extensions.cs` — HTTP client registrations (each uses `.AddAuthToken()`)

---

## Implementation Checklist

Work through each step in order. Build after each major step.

### 1. Data Layer
- [ ] Create entity class in `src/Catalog.API/Model/`
- [ ] Create `IEntityTypeConfiguration<T>` in `src/Catalog.API/Infrastructure/EntityConfigurations/`
- [ ] Add `DbSet<T>` to `src/Catalog.API/Infrastructure/CatalogContext.cs`
- [ ] Add configurable options to `CatalogOptions` if needed

### 2. API Endpoints
- [ ] Add endpoint methods to `src/Catalog.API/Apis/CatalogApi.cs`
- [ ] Register routes in `MapCatalogApi()`
- [ ] Apply `.RequireAuthorization()` only where a user identity is needed
- [ ] **If adding any authorized endpoint**: verify `appsettings.json` has `Identity.Audience` and `Identity.Scopes` (compare with `src/Basket.API/appsettings.json`)

### 3. EF Migration
- [ ] Run: `dotnet tool run dotnet-ef migrations add <MigrationName> --project src/Catalog.API`
- [ ] Review generated migration SQL

### 4. Build
- [ ] `dotnet build src/Catalog.API/Catalog.API.csproj -p:WarningLevel=0 /clp:ErrorsOnly`
- [ ] Fix any errors before continuing

### 5. Client Integration
- [ ] Add response DTO record to `src/WebAppComponents/Catalog/CatalogItem.cs`
- [ ] Add method signature to `src/WebAppComponents/Services/ICatalogService.cs`
- [ ] Implement method in `src/WebAppComponents/Services/CatalogService.cs`

### 6. UI
- [ ] Edit `src/WebApp/Components/Pages/Item/ItemPage.razor`
- [ ] **Blazor SSR rule**: interactions must use `<form method="post" @formname="..." @onsubmit="...">` — `@onclick` is a no-op in SSR mode
- [ ] **HTML rule**: forms cannot be nested — use sibling forms inside a flex wrapper `<div>` for multiple actions on one row
- [ ] Add scoped styles to `ItemPage.razor.css`

### 7. Final Build & Verify
- [ ] Build full solution
- [ ] Run AppHost and manually verify the feature end-to-end

---

## Key Constraints

**Auth config** — When the first `RequireAuthorization()` endpoint is added to a service, `appsettings.json` must have:
```json
"Identity": {
  "Audience": "<service-name>",
  "Scopes": { "<scope>": "<Description>" }
}
```
`Identity__Url` is injected at runtime by AppHost — do not hardcode it.

**Blazor SSR interactions** — Only form posts work. Pattern for each server action:
```razor
<form method="post" @formname="unique-name" @onsubmit="@HandlerMethod" data-enhance="@isLoggedIn">
    <AntiforgeryToken />
    <button type="submit">Action</button>
</form>
```

**Multiple actions on one line** — Wrap sibling forms in a flex div:
```razor
<div class="actions-row">
    <form ...>...</form>  @* action 1 *@
    <form ...>...</form>  @* action 2 *@
</div>
```

**`CatalogServices` parameter object** — Inject dependencies via it, not as separate parameters:
```csharp
// src/Catalog.API/Model/CatalogServices.cs
public class CatalogServices(CatalogContext context, IOptions<CatalogOptions> options, IMemoryCache cache, ...)
```
