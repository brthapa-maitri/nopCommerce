# nopCommerce 3.90 — Architecture Reference

**Generated:** 2026-09-16
**Last Updated:** 2026-09-16T05:30:00Z
**Branch:** `mad/retarget-net48`
**Status:** Active — retargeted from .NET Framework 4.5.1 to 4.8 (commit `cdbff03`)

> This is a **map**, not an essay. It points at code; it does not duplicate it.
> Update it whenever an Architecture Update Trigger fires (see the end of this doc).

---

## SYSTEM ASSETS & CONNECTION STRINGS

### Database

```
Type:              SQL Server, or SQL Server Compact 4.0 (default for a fresh install)
ORM:               Entity Framework 6.1.3 (code-first fluent mappings, no migrations)
DbContext:         src/Libraries/Nop.Data/NopObjectContext.cs
Providers:         SqlServerDataProvider.cs / SqlCeDataProvider.cs (Nop.Data)
Provider selection: src/Libraries/Nop.Core/Data/DataSettingsManager.cs
```

**The connection string does NOT live in `Web.config`.** It lives in a plain-text
settings file written by the installer:

```
src/Presentation/Nop.Web/App_Data/Settings.txt
```

Shape (from the shipped file — never commit real credentials here):

```
DataProvider: sqlce
DataConnectionString: Data Source=|DataDirectory|\Nop.Db.sdf;Persist Security Info=False
```

For SQL Server the same file holds a normal ADO.NET connection string. `Settings.txt`
and `Nop.Db.sdf` are **gitignored** — each developer installs their own store.

If `Settings.txt` is absent or empty, `Global.asax` routes every request to
`InstallController` (the web installer at `/install`).

### Authentication

```
Public store:  Forms authentication over a nopCommerce customer record
               src/Libraries/Nop.Services/Authentication/FormsAuthenticationService.cs
External auth: DotNetOpenAuth + OWIN 3.0.1
               src/Plugins/Nop.Plugin.ExternalAuth.Facebook
Admin:         Same customer identity, gated by ACL permission records
               src/Libraries/Nop.Services/Security/PermissionService.cs
```

### External Services

| Service | Where | Notes |
|---|---|---|
| PayPal (Standard + Direct) | `Nop.Plugin.Payments.PayPal*` | `PayPal` NuGet SDK |
| FedEx / UPS / USPS / Canada Post / Australia Post | `Nop.Plugin.Shipping.*` | SOAP/REST carrier APIs; see `Nop.Services/Web References` |
| European Central Bank FX | `Nop.Plugin.ExchangeRate.EcbExchange` | Daily rate feed |
| Google Analytics | `Nop.Plugin.Widgets.GoogleAnalytics` | Script injection widget |
| Google Shopping | `Nop.Plugin.Feed.GoogleShopping` | Product feed export |
| Facebook OAuth | `Nop.Plugin.ExternalAuth.Facebook` | External login |
| MaxMind GeoIP2 | `App_Data/GeoLite2-Country.mmdb` | Local DB file, country lookup |
| Redis (optional) | `Nop.Core/Caching/RedisCacheManager.cs` | Cache + session state for web farms |
| Azure Blob Storage (optional) | `WindowsAzure.Storage` | Picture storage provider |

---

## DIRECTORY STRUCTURE

```
nopCommerce-3.90/
├── docs/
│   ├── ARCHITECTURE_REFERENCE.md      # This document
│   └── memory/                        # maitri-memory task handoff files
├── .beads/                            # Beads backlog (issues.jsonl tracked, *.db local)
├── .claude/                           # Project agent-team config (settings.json tracked)
└── src/
    ├── NopCommerce.sln                # 26 projects
    ├── Libraries/
    │   ├── Nop.Core/                  # Domain entities, infra, no dependency on Data/Services
    │   │   ├── Domain/                # 27 bounded areas: Catalog, Orders, Customers, ...
    │   │   ├── Infrastructure/        # NopEngine, EngineContext, ITypeFinder, IStartupTask
    │   │   ├── Caching/               # Memory / PerRequest / Redis cache managers
    │   │   ├── Plugins/               # PluginManager, PluginFinder, BasePlugin
    │   │   ├── Data/                  # IRepository<T>, DataSettings, IDataProvider
    │   │   └── Events/                # EntityInserted/Updated/Deleted<T>
    │   ├── Nop.Data/                  # EF6 only — mappings + repository impl
    │   │   ├── Mapping/               # One NopEntityTypeConfiguration per entity, by area
    │   │   ├── NopObjectContext.cs    # The DbContext
    │   │   └── EfRepository.cs        # IRepository<T> implementation
    │   └── Nop.Services/              # ALL business logic. 30 service areas.
    │       ├── Installation/          # First-run schema + seed data
    │       ├── ExportImport/          # EPPlus (xlsx) + iTextSharp (pdf)
    │       └── Tasks/                 # Background TaskThread scheduler
    ├── Presentation/
    │   ├── Nop.Web.Framework/         # MVC plumbing shared by store + admin
    │   │   ├── DependencyRegistrar.cs # Autofac root registration
    │   │   ├── WebWorkContext.cs      # Current customer/currency/language
    │   │   ├── WebStoreContext.cs     # Current store (multi-store)
    │   │   ├── Themes/                # Theme provider + view engine
    │   │   └── Mvc/                   # Base controllers, model binders, filters
    │   └── Nop.Web/                   # The IIS application (both store AND admin)
    │       ├── Controllers/           # 27 public controllers
    │       ├── Factories/             # Model factories (entity → view model)
    │       ├── Themes/DefaultClean/   # The only shipped theme
    │       ├── Plugins/               # Plugin DLLs shadow-copied here at runtime
    │       ├── App_Data/              # Settings.txt, InstalledPlugins.txt, GeoIP, SDF
    │       └── Administration/        # Nop.Admin — an MVC *Area*, not a separate site
    │           ├── Controllers/       # 55 admin controllers
    │           └── AdminAreaRegistration.cs
    ├── Plugins/                       # 20 plugins, each its own csproj
    └── packages/                      # NuGet packages.config restore target
```

**Note on the admin:** `Nop.Admin` compiles to its own assembly but is registered as
an MVC **Area** inside `Nop.Web`. There is one IIS site, not two.

---

## DEPENDENCY GRAPH

### Project reference direction (strictly one-way)

```
Nop.Core  ←──  Nop.Data
   ↑              ↑
   └──────  Nop.Services
                  ↑
          Nop.Web.Framework
             ↑          ↑
         Nop.Web    Nop.Admin        Plugins ──→ Nop.Services / Nop.Web.Framework
```

`Nop.Core` references nothing in the solution. Never add a `Nop.Data` or
`Nop.Services` reference to it.

### Technology Stack

```
Runtime:
├── .NET Framework 4.8          (retargeted from 4.5.1 — all 31 csproj files)
└── ASP.NET MVC 5.2.3 + Razor 3.2.3

Web:
├── Microsoft.AspNet.WebApi 5.2.3   (present; the store itself is MVC, not Web API)
├── Microsoft.Owin 3.0.1            (external auth pipeline)
├── Microsoft.AspNet.Web.Optimization 1.1.3 + WebGrease 1.6.0  (bundling)
└── MiniProfiler 3.2.0              (opt-in perf profiling)

Application:
├── Autofac 4.4.0 + Autofac.Mvc5 4.0.1   (DI container)
├── AutoMapper 5.2.0                     (entity ↔ model mapping)
├── FluentValidation 6.4.0 + FluentValidation.MVC5  (model validation)
├── Newtonsoft.Json 9.0.1
└── System.Linq.Dynamic                  (runtime-built queries)

Data:
├── EntityFramework 6.1.3
├── EntityFramework.SqlServerCompact 6.1.3 + Microsoft.SqlServer.Compact 4.0
└── StackExchange.Redis.StrongName 1.2.1 + RedLock.net  (optional distributed cache/lock)

Media & Export:
├── ImageResizer + ImageResizer.Plugins.PrettyGifs  (thumbnails)
├── EPPlus                                          (xlsx export/import)
└── iTextSharp                                      (pdf invoices)

Test:
├── NUnit 3.6.1
└── RhinoMocks 3.6.1
```

---

## DATABASE SCHEMA

The schema is **not** defined by migrations. It is defined by:

1. **Entity classes** — `Nop.Core/Domain/<Area>/*.cs`, all deriving from `BaseEntity`
2. **Fluent mappings** — `Nop.Data/Mapping/<Area>/*Map.cs`, each a
   `NopEntityTypeConfiguration<TEntity>` that sets the table name, keys and relations
3. **Install scripts** — `Nop.Services/Installation/` seeds the first-run data

To add a table: add the entity to `Nop.Core/Domain`, add a `*Map.cs` under the matching
`Nop.Data/Mapping` folder, and let EF create it. There is no migration file to write.

### Core Entity Areas

| Area (`Nop.Core/Domain/…`) | Representative entities | Purpose |
|---|---|---|
| `Catalog` | Product, Category, Manufacturer, ProductAttribute, ProductReview, SpecificationAttribute | Product catalogue and attributes |
| `Orders` | Order, OrderItem, ShoppingCartItem, RecurringPayment, ReturnRequest, GiftCard | Cart → order lifecycle |
| `Customers` | Customer, CustomerRole, CustomerAttribute, ExternalAuthenticationRecord | Identity, roles, profile |
| `Directory` | Country, StateProvince, Currency, MeasureDimension, MeasureWeight | Reference data |
| `Discounts` | Discount, DiscountRequirement, DiscountUsageHistory | Promotions |
| `Shipping` | Shipment, ShippingMethod, Warehouse, DeliveryDate | Fulfilment |
| `Tax` | TaxCategory | Tax classification |
| `Payments` | — (settings only; payment data lives on `Order`) | Payment config |
| `Media` | Picture, Download | Binary assets (DB or filesystem) |
| `Localization` | Language, LocaleStringResource, LocalizedProperty | i18n |
| `Seo` | UrlRecord | Slug → entity routing |
| `Stores` | Store, StoreMapping | Multi-store |
| `Security` | PermissionRecord, AclRecord | ACL |
| `Configuration` | Setting | Key/value settings, store-scoped |
| `Logging` | Log, ActivityLog, ActivityLogType | Audit + error log |
| `Messages` | MessageTemplate, QueuedEmail, EmailAccount, NewsLetterSubscription | Transactional email |
| `Tasks` | ScheduleTask | Background job registry |
| `Blogs` / `News` / `Polls` / `Forums` / `Topics` | … | Content modules |
| `Affiliates` / `Vendors` | Affiliate, Vendor | Marketplace/referral |
| `Cms` | WidgetSettings | Widget zones |
| `Common` | Address, GenericAttribute, SearchTerm | Cross-cutting |

### Entity conventions

- Every entity derives from `Nop.Core.BaseEntity` (exposes `int Id`).
- Navigation properties are `virtual` (EF lazy loading is on).
- `GenericAttribute` is the escape hatch for adding a field to any entity without a
  schema change — check it before adding a column.

---

## API ENDPOINTS

**There is no REST API in 3.90.** `Microsoft.AspNet.WebApi` is referenced but the
storefront and admin are server-rendered ASP.NET MVC. "Endpoints" here means routes.

### Routing

Routes are registered by `IRouteProvider` implementations discovered at startup:

- Storefront: `src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs`
- Admin area: `src/Presentation/Nop.Web/Administration/AdminAreaRegistration.cs`
- Plugins: each plugin's own `RouteProvider.cs` (8 plugins add routes)

`MapLocalizedRoute` is the localized variant — it prefixes the SEO language code
(`/en/cart`) when "SEO friendly URLs with multiple languages" is enabled.

### Key storefront routes

| Route name | Path | Controller / Action |
|---|---|---|
| HomePage | `/` | `Home/Index` |
| Login | `/login/` | `Customer/Login` |
| Register | `/register/` | `Customer/Register` |
| Logout | `/logout/` | `Customer/Logout` |
| ShoppingCart | `/cart/` | `ShoppingCart/Cart` |
| EstimateShipping | `/cart/estimateshipping` | `ShoppingCart/GetEstimateShipping` |
| Wishlist | `/wishlist/{customerGuid}` | `ShoppingCart/Wishlist` |
| WidgetsByZone | `/widgetsbyzone/` | `Widget/WidgetsByZone` |
| Checkout | `/checkout/…` | `Checkout/*` |
| Product / Category / Manufacturer | slug-based | resolved via `UrlRecord` (see below) |

**Slug resolution:** product and category URLs are not literal routes. A generic
route hands the slug to `GenericUrlRouteProvider`, which looks it up in the
`UrlRecord` table and dispatches to the right controller. Changing a slug means
writing a `UrlRecord`, not editing routes.

### Controllers

| Surface | Location | Count |
|---|---|---|
| Storefront | `Nop.Web/Controllers/` | 27 |
| Admin | `Nop.Web/Administration/Controllers/` | 55 |

Base classes: `BasePublicController` (store) and `BaseAdminController` (admin), both
descending from `BaseController` in `Nop.Web.Framework/Controllers`.

---

## DATA FLOW DIAGRAMS

### Application startup

```
IIS → Global.asax  Application_Start
        │
        ├─ EngineContext.Initialize()
        │     └─ NopEngine
        │          ├─ ITypeFinder scans bin/ + Plugins/ for assemblies
        │          ├─ PluginManager shadow-copies plugin DLLs → Plugins/bin
        │          ├─ Autofac builds the container:
        │          │     Nop.Web.Framework/DependencyRegistrar.cs  (order 0)
        │          │     + every IDependencyRegistrar found, by Order
        │          └─ runs every IStartupTask by Order
        │                 (EfStartUpTask creates/updates the schema)
        │
        ├─ if App_Data/Settings.txt is missing/empty → redirect all → /install
        │
        ├─ RouteProvider(s) register routes, ordered by Priority
        ├─ Bundles, view engines (ThemeableRazorViewEngine), model binders
        └─ TaskManager starts background TaskThreads
```

### A storefront request

```
Request
  │
  ▼
Route table ── slug? ──► UrlRecord lookup ──► resolved controller/action
  │
  ▼
Action filters:  WebWorkContext (customer, language, currency, vendor)
                 WebStoreContext (which store)
                 StoreClosed / StoreIpAddress / CustomerLastActivity / CheckAffiliate
  │
  ▼
Controller (thin)
  │
  ├─► Nop.Services.*Service        ← business logic lives HERE
  │        │
  │        ├─► ICacheManager       ← check cache first
  │        └─► IRepository<T>      ← EfRepository → NopObjectContext → EF6 → DB
  │
  ├─► Nop.Web/Factories/*Factory   ← entity → view model
  │
  ▼
Razor view (theme-resolved: Themes/DefaultClean, falling back to Views/)
  │
  └─► Widget zones render plugin output via Widget/WidgetsByZone
```

### Domain events

```
EfRepository.Insert/Update/Delete
        │
        └─► IEventPublisher.EntityInserted<T>() / EntityUpdated<T>() / EntityDeleted<T>()
                  │
                  └─► every IConsumer<EntityInserted<T>> resolved from Autofac
                        (cache invalidation, search index, plugin reactions)
```

Add a subscriber by implementing `IConsumer<TEvent>` — Autofac auto-registers it.
Do not call cache-eviction code directly from a service; publish/consume instead.

### Plugin lifecycle

```
Plugins/<Name>/Description.txt   ← manifest: SystemName, Version, SupportedVersions,
        │                          FileName, Group, DisplayOrder, Author
        ▼
PluginFileParser → PluginDescriptor
        ▼
PluginManager shadow-copies the DLL into Nop.Web/Plugins/bin
        ▼
App_Data/InstalledPlugins.txt  ← which SystemNames are installed
        ▼
BasePlugin.Install() / Uninstall()  ← settings + locale resources seeded here
        ▼
Plugin implements a feature interface:
   IPaymentMethod · IShippingRateComputationMethod · ITaxProvider ·
   IWidgetPlugin · IExternalAuthenticationMethod · IDiscountRequirementRule ·
   IPickupPointProvider · IExchangeRateProvider · IMiscPlugin
```

`SupportedVersions` in `Description.txt` must include `3.90` or the plugin is
silently skipped. **Every bundled plugin was retargeted to net48 — new plugins must
target `v4.8` too.**

---

## ENVIRONMENT VARIABLES

This is a classic ASP.NET application: **it has no `.env` file and reads no environment
variables.** Configuration comes from three places:

```
1. src/Presentation/Nop.Web/App_Data/Settings.txt     — data provider + connection string
   DataProvider:            sqlserver | sqlce
   DataConnectionString:    <ADO.NET connection string>

2. src/Presentation/Nop.Web/Web.config  <appSettings>
   owin:AutomaticAppStartup       false
   webpages:Version               3.0.0.0
   webpages:Enabled               false
   PreserveLoginUrl               true
   ClientValidationEnabled        true
   UnobtrusiveJavaScriptEnabled   true
   # Commented out by default — enable behind a load balancer:
   Use_HTTP_CLUSTER_HTTPS         / Use_HTTP_X_FORWARDED_PROTO / ForwardedHTTPheader

3. The `Setting` database table — everything else (store name, tax, shipping,
   payment, email accounts). Edited in the admin UI, NOT in a config file.
```

Transform files `Web.Debug.config` / `Web.Release.config` apply per build configuration.

**Never commit:** `App_Data/Settings.txt`, `App_Data/Nop.Db.sdf`,
`App_Data/InstalledPlugins.txt` — all already gitignored.

---

## QUICK COMMANDS

This solution predates the `dotnet` CLI. It builds with **MSBuild** (Visual Studio
2017-era tooling) against .NET Framework 4.8.

```bash
# Restore (packages.config, not PackageReference — needs nuget.exe, not `dotnet restore`)
nuget restore src/NopCommerce.sln

# Build
msbuild src/NopCommerce.sln /p:Configuration=Debug /p:Platform="Any CPU"
msbuild src/NopCommerce.sln /p:Configuration=Release

# Run — F5 on Nop.Web in Visual Studio (IIS Express), or point an IIS site at
# src/Presentation/Nop.Web. First run lands on /install.

# Tests (NUnit 3 console runner against the built test assemblies)
nunit3-console src/Tests/Nop.Core.Tests/bin/Debug/Nop.Core.Tests.dll
nunit3-console src/Tests/Nop.Data.Tests/bin/Debug/Nop.Data.Tests.dll
nunit3-console src/Tests/Nop.Services.Tests/bin/Debug/Nop.Services.Tests.dll
nunit3-console src/Tests/Nop.Web.MVC.Tests/bin/Debug/Nop.Web.MVC.Tests.dll
```

**Prerequisite note:** the `dotnet` CLI is *not* installed on the current dev machine,
and neither is MSBuild standalone — building requires Visual Studio with the
.NET Framework 4.8 targeting pack, or the Build Tools. Confirm the 4.8 targeting pack
is present before assuming a build failure is a code problem.

---

## KEY BOUNDARIES

| Component | Responsibility | Never Does |
|---|---|---|
| `Nop.Core` | Domain entities, caching, plugin/DI infrastructure, `IRepository<T>` contract | Reference `Nop.Data` or `Nop.Services`; contain business logic; touch EF |
| `Nop.Data` | EF6 mappings, `DbContext`, `IRepository<T>` implementation | Contain business rules; be referenced by controllers directly |
| `Nop.Services` | **All** business logic, transactions, event publishing | Reference `System.Web` / HttpContext; return view models; render |
| `Nop.Web.Framework` | MVC plumbing shared by store + admin: work context, themes, filters, validators, DI root | Contain feature-specific business logic |
| `Nop.Web/Controllers` | Orchestrate a request: call services, hand off to a factory, return a view | Query `IRepository<T>` directly; contain business rules |
| `Nop.Web/Factories` | Build view models from entities | Mutate state; call `SaveChanges` |
| `Nop.Admin` | Admin Area controllers/views/validators | Be deployed as a separate site; bypass `PermissionService` |
| Plugins | Implement one feature interface; self-contained install/uninstall | Modify core tables; assume they are always installed |

### Rules a teammate will trip over

1. **Controllers must not touch `IRepository<T>`.** Go through a service.
2. **No EF migrations exist.** Schema changes = entity class + `*Map.cs`.
3. **Settings belong in the `Setting` table**, seeded in `BasePlugin.Install()`, not
   in `Web.config`.
4. **Localizable strings belong in `LocaleStringResource`**, not hardcoded in views.
   Use `T("Resource.Key")` / `_localizationService.GetResource(...)`.
5. **Cache invalidation goes through `IConsumer<EntityUpdated<T>>`**, not ad-hoc
   `_cacheManager.Remove` calls scattered through services.
6. **Views resolve through the theme first.** Overriding a view means adding it under
   `Themes/DefaultClean/Views/`, not editing `Views/`.
7. **New projects must target `v4.8`** and use `net48` package monikers — the whole
   solution was just retargeted and a `v4.5.1` project will break the build.

---

## RELATED DOCUMENTATION

| Document | Path | Purpose |
|---|---|---|
| Deployment notes | `src/Deploying.Readme.txt` | Upstream's publish instructions |
| License | `src/LICENSE.md` | nopCommerce Public License v3 |
| Task memory | `docs/memory/` | Per-task handoff files (maitri-memory) |
| Beads backlog | `.beads/issues.jsonl` | Tracked work items |
| Team config | `.claude/settings.json` | Agent-team enablement, base branch, beads prefix |
| Upstream docs | https://docs.nopcommerce.com/en/developer/ | Official 3.x developer guide |

---

## Document Maintenance

Update this document when:

| Change | Section to update |
|---|---|
| New/changed controller or `RouteProvider` | API Endpoints |
| New entity in `Nop.Core/Domain` or map in `Nop.Data/Mapping` | Database Schema |
| `packages.config` or a `.csproj` reference changes | Dependency Graph |
| New project, plugin, or top-level directory | Directory Structure |
| `Web.config` / `Settings.txt` shape changes | Environment Variables |
| New `IStartupTask`, `IDependencyRegistrar`, or `IConsumer` | Data Flow Diagrams |
| Target framework or build tooling changes | Quick Commands, Dependency Graph |

**Last Updated:** 2026-09-16
**Version:** 1.0
