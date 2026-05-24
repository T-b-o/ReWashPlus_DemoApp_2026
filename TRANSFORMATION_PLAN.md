# RewashPlus — SaaS Transformation Plan
**Prepared for:** Principal .NET SaaS Architecture Review  
**Current State:** Blazor WASM Demo (single-tenant, LocalStorage-backed)  
**Target State:** Multi-tenant, Multi-branch, Offline-first, Azure-deployable SaaS Platform  
**Approach:** Incremental evolution — preserve working code, refactor in phases.

---

## 1. ARCHITECTURE REVIEW — Current State

### 1.1 What Was Found

| Area | Current State |
|---|---|
| **Frontend** | Blazor WASM PWA — one project `ReWashPlus_DemoApp` |
| **Backend** | **Does not exist yet** — referenced in ARCHITECTURE.md but not built |
| **Database** | None — all data in `Blazored.LocalStorage` (browser key-value) |
| **Auth** | None |
| **Multi-tenancy** | None — no `TenantId` anywhere |
| **Offline** | Partial — LocalStorage sync pattern exists in `BookingService`, `JobService`, `PaymentService` |
| **CQRS / MediatR** | None |
| **Vertical Slice** | None |
| **Audit log** | None |
| **Feature toggles** | None |

### 1.2 What Is Working and Well-Structured

- ✅ Booking model is solid — `JobStatus`, `JobType` enums, `Services` list, timeline fields (`ArrivedAt`, `StartedAt`, `CompletedAt`)
- ✅ `JobService` pattern (pending/synced split) is the correct offline-first skeleton
- ✅ `Car` model has all required fields already (`Make`, `Model`, `Color`, `Plate`)
- ✅ `Payment` model covers method, status, reference, and paid timestamp
- ✅ `Service` model includes `DurationMinutes` and `Price`
- ✅ `JobService` line-item (`JobService.cs`) captures `PriceAtTime` — correct historical pricing
- ✅ Tailwind + ClickUp dark theme is consistently applied
- ✅ NavMenu collapsible sidebar with mobile overlay is production-quality
- ✅ Code-behind pattern (`BookingsHistory.razor.cs`) established
- ✅ `ServiceWorker` and PWA manifest already wired

### 1.3 Critical Gaps Found

| Gap | File(s) | Impact |
|---|---|---|
| `BookingsHistory.razor.cs` references a local `Booking` type with `Date`, `Time`, `BookingStatus` fields that don't match `Models/Booking.cs` | `BookingsHistory.razor.cs` | Compile break |
| `Booking.CarDescription` is a string — no FK to `Car` | `Models/Booking.cs` | Violates design intent |
| `Customer` has no `UserId` — no auth composition | `Models/Customer.cs` | Blocks real auth |
| No backend project in solution | `.sln` | Cannot ship |
| `BookingService.cs` and `JobService.cs` both manage bookings via LocalStorage — overlapping responsibility | `Services/` | Confusion |
| All mock data is hardcoded in `.razor` components | All pages | Cannot replace with real API |
| `ServiceService` seeds hardcoded defaults — no tenant-scoped catalog | `Services/ServiceService.cs` | Wrong for multi-tenant |
| `NavMenu.razor.cs` referenced but `NavMenu.razor.css` appears twice | `Layout/` | Minor |

---

## 2. EXISTING vs PROPOSED COMPARISON

### 2.1 Entity Comparison

| Entity | Current | Change | Action |
|---|---|---|---|
| `Booking` | Has `CarDescription` string, `AssignedStaffId` int, no `TenantId`/`BranchId` | Add `TenantId`, `BranchId`, `VehicleId`, `RowVersion`; remove `CarDescription` | **MODIFY** |
| `Car` → `Vehicle` | `Car` with `Plate`, `Make`, `Model`, `Color` | Rename to `Vehicle`, add `TenantId`, `VehicleCategory`, `RowVersion` | **MODIFY** (rename + extend) |
| `Customer` | Flat, no `UserId` | Add `UserId` (FK to `AppUser`), `TenantId` | **MODIFY** |
| `Payment` | Has method/status/reference — good | Add `TenantId`, `BranchId`, `RowVersion` | **MODIFY** |
| `Service` | Name, price, duration — good | Add `TenantId`, `BranchId` (branch-specific pricing) | **MODIFY** |
| `JobService` (line item) | Correct historical price capture | No change needed | **KEEP** |
| `Tenant` | Does not exist | New | **ADD** |
| `Branch` | Does not exist | New | **ADD** |
| `BranchSettings` | Does not exist | New | **ADD** |
| `AppUser` | Does not exist | New (ASP.NET Identity base) | **ADD** |
| `StaffProfile` | Does not exist | New (composition with `AppUser`) | **ADD** |
| `TenantFeature` | Does not exist | New | **ADD** |
| `AuditLog` | Does not exist | New | **ADD** |
| `OutboxMessage` | Does not exist | New (for reliable async events) | **ADD** |

### 2.2 Screen / Page Comparison

| Page | Current State | Action |
|---|---|---|
| `Home.razor` | Hardcoded mock stats | Wire to real API; scope to `BranchId` | **MODIFY** |
| `BookWash.razor` | Uses `BookingService`, basic form | Extend form: vehicle lookup, branch selector, service multi-select | **MODIFY** |
| `BookingsHistory.razor` | Mock data, references wrong `Booking` type | Fix type mismatch; connect to `JobService` | **FIX + MODIFY** |
| `Payments.razor` | Stub page | Implement payment list + recording | **IMPLEMENT** |
| `Profile.razor` | Stub page | Implement user/vehicle/loyalty UI | **IMPLEMENT** |
| `Notifications.razor` | Stub page | Wire Firebase push | **IMPLEMENT** |
| `Admin/AdminDashboard.razor` | Mock stats, good layout | Wire to API; add branch/tenant filter | **MODIFY** |
| `Admin/AdminManageBookings.razor` | Mock data, working layout | Connect to `JobService` / API | **MODIFY** |
| `Admin/ManagePayments.razor` | (not reviewed — assumed stub) | Implement | **IMPLEMENT** |
| `Admin/Reports.razor` | Stub | Implement | **IMPLEMENT** |
| `Admin/Settings.razor` | Stub | Branch settings + feature toggles UI | **IMPLEMENT** |
| `Admin/Users.razor` | Stub | Staff/user management | **IMPLEMENT** |

### 2.3 Services Comparison

| Service | Current | Action |
|---|---|---|
| `BookingService` | Pending/synced split to LocalStorage | Merge into `JobService` or rename; only one should own booking persistence | **MERGE INTO `JobService`** |
| `JobService` | Pending/synced + HTTP sync | Promote to primary — add `TenantId`/`BranchId` scoping | **KEEP + EXTEND** |
| `CustomerService` | LocalStorage CRUD — solid | Add `TenantId` scoping; prepare API switch | **KEEP + EXTEND** |
| `CarService` | LocalStorage CRUD | Rename to `VehicleService`; add `TenantId`, `VehicleCategory` | **RENAME + EXTEND** |
| `PaymentService` | Pending/synced pattern | Add `TenantId`/`BranchId`; keep pattern | **KEEP + EXTEND** |
| `ServiceService` | Seeds hardcoded defaults | Scope to `TenantId`/`BranchId`; load from API when online | **MODIFY** |
| `PageTitleService` | Simple event — fine | Keep as-is | **KEEP** |
| `ConnectivityService` | Does not exist | New | **ADD** |
| `OfflineDatabaseService` | Does not exist (LocalStorage is used directly) | New — wrap IndexedDB via JS interop | **ADD** |
| `SyncService` | Partially in `JobService` / `BookingService` | Extract into dedicated `SyncService` | **EXTRACT** |
| `FeatureToggleService` | Does not exist | New | **ADD** |
| `TenantContextService` | Does not exist | New — holds current `TenantId`, `BranchId` from token/storage | **ADD** |

---

## 3. REFACTOR ROADMAP

### Phase 0 — Fix Existing Breaks (Do First — 1–2 days)

These are bugs that exist today, blocking compilation or correctness:

1. **Fix `BookingsHistory.razor.cs`** — `Booking` type used there has `Date`, `Time`, `BookingStatus` fields that don't exist in `Models/Booking.cs`. Create a local `BookingListItem` view model or align the code-behind with the real model.
2. **Remove `BookingService.cs`** — it duplicates `JobService.cs`. Consolidate offline booking persistence entirely into `JobService`.
3. **Rename `CarService` → `VehicleService`** and `Car` → `Vehicle` throughout.
4. **Unify `Program.cs` registrations** after above renames.

### Phase 1 — Solution Restructure (3–5 days)

Split the monolithic WASM project into a proper solution with multiple projects:

```
RewashPlus.sln
├── src/
│   ├── RewashPlus.Client/          ← Current demo app (WASM PWA) — RENAMED/MOVED
│   ├── RewashPlus.Api/             ← New ASP.NET Core 8 Web API
│   ├── RewashPlus.Application/     ← CQRS Commands/Queries/Handlers (MediatR)
│   ├── RewashPlus.Domain/          ← Entities, Enums, Domain Events, Interfaces
│   ├── RewashPlus.Infrastructure/  ← EF Core DbContext, Repos, Azure Blob, Firebase
│   └── RewashPlus.Contracts/       ← Shared DTOs/Request/Response models (referenced by Client + Api)
└── tests/
    ├── RewashPlus.Application.Tests/
    └── RewashPlus.Api.Tests/
```

**What moves where:**

| Current Location | Moves To |
|---|---|
| `Models/*.cs` | `RewashPlus.Domain/Entities/` |
| `Services/*.cs` (client-side) | Stay in `RewashPlus.Client/Services/` |
| New API controllers | `RewashPlus.Api/Features/` (Vertical Slice) |
| New CQRS handlers | `RewashPlus.Application/Features/` |
| EF Core DbContext | `RewashPlus.Infrastructure/Data/` |

### Phase 2 — Domain Model Evolution (5–7 days)

Apply all entity changes described in Section 2.1. All changes are additive first:

1. Add `Tenant` and `Branch` entities (new)
2. Add `TenantId` to `Customer`, `Vehicle`, `Service` (nullable initially for migration safety)
3. Add `BranchId` to `Booking`, `Payment`, `StaffProfile`, `Service` (nullable initially)
4. Add `RowVersion` (byte[]) to `Booking`, `Payment`, `Customer`, `Vehicle`, `Service`
5. Replace `Booking.CarDescription` with `Booking.VehicleId` (keep `CarDescription` as deprecated nullable until migration completes)
6. Add `AppUser`, `StaffProfile`, `TenantFeature`, `BranchSettings`, `AuditLog`, `OutboxMessage`
7. Enforce non-nullable `TenantId`/`BranchId` after data migration

### Phase 3 — Backend API (7–10 days)

Build `RewashPlus.Api` with Vertical Slice + CQRS using MediatR:

```
RewashPlus.Api/
└── Features/
    ├── Bookings/
    │   ├── CreateBooking/      (Command)
    │   ├── GetBookings/        (Query)
    │   ├── UpdateBookingStatus/(Command)
    │   └── SyncBookings/       (Command — offline sync endpoint)
    ├── Customers/
    ├── Vehicles/
    ├── Payments/
    ├── Services/
    ├── Tenants/
    ├── Branches/
    ├── Auth/
    └── Sync/
        ├── UploadSync/         POST /api/v1/sync/upload
        └── DownloadSync/       GET  /api/v1/sync/download
```

### Phase 4 — Offline-First Evolution (5–7 days)

Upgrade offline strategy from LocalStorage → IndexedDB-backed with proper sync state machine.

### Phase 5 — Multi-Tenancy (5–7 days)

Apply tenant resolution middleware, row-level security, branch context injection.

### Phase 6 — Feature Toggles + Branch Settings (3 days)

Implement `TenantFeature` table + client-side `FeatureToggleService`.

### Phase 7 — Auth (3–5 days)

Add ASP.NET Identity + JWT tokens; wire `TenantId`/`BranchId` into claims.

---

## 4. DATABASE MIGRATION STRATEGY

### 4.1 Starting State

There is currently no database. The first migration will create the entire schema. This is an advantage — no legacy data to migrate.

### 4.2 EF Core Migration Sequence

```
Migration 001 — Initial schema
    Creates: AppUsers, Tenants, Branches, Customers, Vehicles,
             Services, Bookings, JobServices, Payments,
             StaffProfiles, TenantFeatures, BranchSettings,
             AuditLogs, OutboxMessages

Migration 002 — Seed reference data
    Inserts: Default tenant, default branch, default service catalog

Migration 003 — Add RowVersion columns
    Alters: Bookings, Payments, Customers, Vehicles, Services
    (RowVersion is byte[] — concurrency token for sync conflict detection)

Migration 004 — Indexes for tenant isolation
    Adds: Composite indexes on (TenantId, BranchId) for all tenant-scoped tables
```

### 4.3 Key EF Core Conventions

```csharp
// In DbContext.OnModelCreating — apply TenantId filter to all tenant-scoped entities
modelBuilder.Entity<Booking>().HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<Customer>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
// Repeat for Vehicle, Payment, Service, StaffProfile, TenantFeature, BranchSettings
```

### 4.4 RowVersion for Conflict Resolution

```csharp
// On all synced entities:
public byte[] RowVersion { get; set; } = Array.Empty<byte>();

// In EF Core config:
modelBuilder.Entity<Booking>().Property(b => b.RowVersion).IsRowVersion();
```

**Conflict rule:** Last-write-wins unless `RowVersion` differs from what the client sent. If conflict detected, server returns `HTTP 409 Conflict` with the current server values. Client UI shows merge dialog.

### 4.5 Azure SQL Deployment

```
Development:  LocalDB / SQL Server Express
Staging:      Azure SQL Basic tier (S0)
Production:   Azure SQL Standard (S2+) with geo-redundancy
```

Use EF Core `MigrationBundle` for deployment via Azure DevOps pipeline.

---

## 5. FOLDER CHANGES

### 5.1 Current Folder Structure (Demo App)

```
ReWashPlus_DemoApp/
├── Admin/          (6 razor pages)
├── Layout/         (NavMenu, MainLayout)
├── Models/         (6 entity classes)
├── Pages/          (7 razor pages)
├── Services/       (7 service classes)
└── wwwroot/
```

### 5.2 Proposed Solution Folder Structure

```
RewashPlus/                              ← Solution root
├── RewashPlus.sln
│
├── src/
│   │
│   ├── RewashPlus.Domain/               ← Zero dependencies
│   │   ├── Entities/
│   │   │   ├── Tenant.cs               NEW
│   │   │   ├── Branch.cs               NEW
│   │   │   ├── AppUser.cs              NEW
│   │   │   ├── Customer.cs             MODIFIED (from Models/)
│   │   │   ├── Vehicle.cs              RENAMED from Car.cs
│   │   │   ├── Booking.cs              MODIFIED
│   │   │   ├── Payment.cs              MODIFIED
│   │   │   ├── Service.cs              MODIFIED
│   │   │   ├── JobService.cs           MOVED (keep as-is)
│   │   │   ├── StaffProfile.cs         NEW
│   │   │   ├── TenantFeature.cs        NEW
│   │   │   ├── BranchSettings.cs       NEW
│   │   │   ├── AuditLog.cs             NEW
│   │   │   └── OutboxMessage.cs        NEW
│   │   ├── Enums/
│   │   │   ├── JobStatus.cs            MOVED from Booking.cs
│   │   │   ├── JobType.cs              MOVED from Booking.cs
│   │   │   ├── PaymentMethod.cs        MOVED from Payment.cs
│   │   │   ├── PaymentStatus.cs        MOVED from Payment.cs
│   │   │   ├── SyncStatus.cs           NEW
│   │   │   └── VehicleCategory.cs      NEW
│   │   ├── Events/
│   │   │   ├── BookingCreatedEvent.cs  NEW
│   │   │   └── PaymentCompletedEvent.cs NEW
│   │   └── Interfaces/
│   │       ├── ITenantEntity.cs        NEW
│   │       ├── IBranchEntity.cs        NEW
│   │       └── IAuditableEntity.cs     NEW
│   │
│   ├── RewashPlus.Application/          ← MediatR handlers; depends on Domain
│   │   └── Features/
│   │       ├── Bookings/
│   │       │   ├── CreateBooking/
│   │       │   │   ├── CreateBookingCommand.cs
│   │       │   │   ├── CreateBookingHandler.cs
│   │       │   │   └── CreateBookingValidator.cs
│   │       │   ├── GetBookings/
│   │       │   │   ├── GetBookingsQuery.cs
│   │       │   │   └── GetBookingsHandler.cs
│   │       │   └── SyncBookings/
│   │       │       ├── SyncUploadCommand.cs
│   │       │       └── SyncUploadHandler.cs
│   │       ├── Customers/
│   │       ├── Vehicles/
│   │       ├── Payments/
│   │       ├── Services/
│   │       ├── Tenants/
│   │       └── Branches/
│   │
│   ├── RewashPlus.Infrastructure/       ← EF Core, Repos, External services
│   │   ├── Data/
│   │   │   ├── RewashDbContext.cs
│   │   │   ├── Configurations/          ← IEntityTypeConfiguration per entity
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   │   ├── AzureBlobStorageService.cs
│   │   │   └── FirebaseNotificationService.cs
│   │   └── BackgroundJobs/
│   │       └── OutboxProcessorJob.cs    ← Azure Function or hosted service
│   │
│   ├── RewashPlus.Api/                  ← ASP.NET Core 8 Web API
│   │   ├── Features/                    ← Vertical slice — one folder per feature
│   │   │   ├── Bookings/
│   │   │   │   └── BookingsController.cs
│   │   │   ├── Sync/
│   │   │   │   └── SyncController.cs
│   │   │   ├── Customers/
│   │   │   ├── Vehicles/
│   │   │   ├── Payments/
│   │   │   └── Auth/
│   │   ├── Middleware/
│   │   │   ├── TenantResolutionMiddleware.cs
│   │   │   └── AuditMiddleware.cs
│   │   └── Program.cs
│   │
│   ├── RewashPlus.Contracts/            ← Shared DTOs; referenced by Client + Api
│   │   ├── Bookings/
│   │   │   ├── BookingDto.cs
│   │   │   ├── CreateBookingRequest.cs
│   │   │   └── BookingSyncPayload.cs
│   │   ├── Customers/
│   │   ├── Vehicles/
│   │   ├── Payments/
│   │   ├── Sync/
│   │   │   ├── SyncUploadRequest.cs
│   │   │   └── SyncDownloadResponse.cs
│   │   └── Auth/
│   │       └── LoginRequest.cs
│   │
│   └── RewashPlus.Client/               ← Current demo app — RENAMED/MOVED here
│       ├── Admin/                       KEEP — wire to API
│       ├── Layout/                      KEEP as-is
│       ├── Pages/                       KEEP — fix type mismatches
│       ├── Services/                    KEEP — extend with TenantContext
│       │   ├── ConnectivityService.cs   NEW
│       │   ├── OfflineDatabaseService.cs NEW
│       │   ├── SyncService.cs           EXTRACTED from JobService
│       │   ├── FeatureToggleService.cs  NEW
│       │   └── TenantContextService.cs  NEW
│       └── wwwroot/
│
└── tests/
    ├── RewashPlus.Application.Tests/
    └── RewashPlus.Api.Tests/
```

---

## 6. API CHANGES

### 6.1 API Conventions

- Base URL: `https://api.rewashplus.co.za`
- Versioning: URL path — `/api/v1/`
- Auth: JWT Bearer token (claims include `TenantId`, `BranchId`, `UserId`)
- Tenant isolation: enforced server-side via `TenantResolutionMiddleware` + EF Core query filters
- Response envelope: `ApiResponse<T>` with `Success`, `Data`, `Errors`

### 6.2 Endpoint Inventory

#### Bookings
```
GET    /api/v1/bookings                     List (filtered by TenantId/BranchId from token)
GET    /api/v1/bookings/{id}                Single booking
POST   /api/v1/bookings                     Create
PUT    /api/v1/bookings/{id}/status         Update status only
DELETE /api/v1/bookings/{id}               Cancel
```

#### Customers
```
GET    /api/v1/customers
GET    /api/v1/customers/{id}
POST   /api/v1/customers
PUT    /api/v1/customers/{id}
GET    /api/v1/customers/search?q={term}
```

#### Vehicles
```
GET    /api/v1/vehicles?customerId={id}
POST   /api/v1/vehicles
PUT    /api/v1/vehicles/{id}
```

#### Payments
```
GET    /api/v1/payments?bookingId={id}
POST   /api/v1/payments
```

#### Services (Catalog)
```
GET    /api/v1/services?branchId={id}       Branch-scoped pricing
POST   /api/v1/services                     (Admin only)
PUT    /api/v1/services/{id}
```

#### Sync (Offline-First)
```
POST   /api/v1/sync/upload                  Batch upsert from client
GET    /api/v1/sync/download?since={ts}     Changes since timestamp
```

#### Tenants / Branches (Platform Admin)
```
GET    /api/v1/tenants
POST   /api/v1/tenants
GET    /api/v1/tenants/{id}/branches
POST   /api/v1/tenants/{id}/branches
GET    /api/v1/tenants/{id}/features
PUT    /api/v1/tenants/{id}/features/{code}
```

#### Branch Settings
```
GET    /api/v1/branches/{id}/settings
PUT    /api/v1/branches/{id}/settings
```

#### Auth
```
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/register
```

### 6.3 Sync Payload Shape

```csharp
// POST /api/v1/sync/upload
public record SyncUploadRequest(
    Guid TenantId,
    Guid BranchId,
    string DeviceId,
    List<SyncableBooking>  Bookings,
    List<SyncablePayment>  Payments,
    List<SyncableCustomer> Customers,
    List<SyncableVehicle>  Vehicles
);

// Each syncable record carries local state
public record SyncableBooking(
    Guid   LocalId,
    Guid?  ServerId,
    byte[] RowVersion,  // for conflict detection
    SyncStatus SyncStatus,
    BookingDto Data
);

// SyncStatus enum
public enum SyncStatus { Pending, Synced, Failed, Conflict }
```

### 6.4 Conflict Resolution Contract

```
Client sends:  { LocalId, ServerId, RowVersion, Data }
Server checks: Does stored RowVersion == sent RowVersion?
  YES → update accepted → returns new RowVersion
  NO  → HTTP 409 Conflict → returns { ConflictedRecord, ServerVersion }
Client action on 409: Show merge UI or last-write-wins based on business rule
```

---

## 7. OFFLINE-FIRST INTEGRATION CHANGES

### 7.1 Current Offline Approach

- `Blazored.LocalStorage` (key-value JSON in browser `localStorage`)
- Services maintain in-memory `_pendingX` / `_syncedX` lists
- HTTP sync on demand via `SyncPendingAsync()`
- **Problem:** `localStorage` is limited (~5MB), not queryable, not transactional

### 7.2 Target Offline Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Blazor Client                          │
│                                                          │
│  Pages / Components                                      │
│       │                                                  │
│       ▼                                                  │
│  Domain Services (CustomerService, JobService, etc.)     │
│       │  reads/writes                                    │
│       ▼                                                  │
│  OfflineDatabaseService  ←──  IndexedDB (via JS interop) │
│       │                                                  │
│  SyncService  ─── ConnectivityService                    │
│       │  when online                                     │
│       ▼                                                  │
│  HttpClient → /api/v1/sync/upload                        │
│            ← /api/v1/sync/download                       │
└─────────────────────────────────────────────────────────┘
```

### 7.3 New Services to Add

#### `ConnectivityService`
```csharp
// Services/ConnectivityService.cs (Client project)
public class ConnectivityService
{
    public bool IsOnline { get; private set; }
    public event Action? ConnectivityChanged;

    // Calls navigator.onLine via JSInterop
    // Listens to window online/offline events
    public async Task InitializeAsync(IJSRuntime js) { ... }
}
```

#### `OfflineDatabaseService`
```csharp
// Services/OfflineDatabaseService.cs (Client project)
// Wraps IndexedDB via JS interop (use Blazored.IndexedDB or custom wrapper)
public class OfflineDatabaseService
{
    // Local record — every offline entity follows this shape
    // LocalId: client-generated GUID
    // ServerId: null until synced
    // SyncStatus: Pending | Synced | Failed | Conflict
    // RowVersion: byte[] from server after first sync

    Task<List<T>> GetAllAsync<T>(string storeName);
    Task UpsertAsync<T>(string storeName, T item);
    Task<List<T>> GetPendingAsync<T>(string storeName);
    Task MarkSyncedAsync(string storeName, Guid localId, Guid serverId, byte[] rowVersion);
}
```

#### `SyncService`
```csharp
// Services/SyncService.cs (Client project)
// Extracted from BookingService + JobService
public class SyncService
{
    // Called on app start and on connectivity restored
    public async Task SyncAllAsync();

    // Per-entity sync
    public async Task SyncBookingsAsync();
    public async Task SyncPaymentsAsync();
    public async Task SyncCustomersAsync();
    public async Task SyncVehiclesAsync();

    // Download changes from server since last sync
    public async Task DownloadChangesAsync(DateTimeOffset since);
}
```

### 7.4 Migration from LocalStorage to IndexedDB

This migration is done in Phase 4, incrementally:

1. **Step 1:** Add `OfflineDatabaseService` alongside existing `LocalStorage` usage
2. **Step 2:** On app init, check for existing LocalStorage data and migrate to IndexedDB
3. **Step 3:** Switch each service (`CustomerService`, `JobService`, etc.) to use `OfflineDatabaseService` one at a time
4. **Step 4:** Remove `Blazored.LocalStorage` dependency once all services migrated

### 7.5 Local Record Schema

Every offline-synced entity carries sync metadata:

```csharp
public abstract class SyncableRecord
{
    public Guid    LocalId      { get; set; } = Guid.NewGuid();
    public Guid?   ServerId     { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public byte[]  RowVersion   { get; set; } = Array.Empty<byte>();
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    public bool    IsDeleted    { get; set; }
}
```

---

## 8. MULTI-TENANT MIGRATION STEPS

### Step 1 — Add Tenant and Branch Entities

```csharp
// RewashPlus.Domain/Entities/Tenant.cs
public class Tenant
{
    public Guid   TenantId    { get; set; } = Guid.NewGuid();
    public string CompanyName { get; set; } = string.Empty;
    public string Slug        { get; set; } = string.Empty;  // URL-friendly key e.g. "abc-car-wash"
    public bool   IsActive    { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// RewashPlus.Domain/Entities/Branch.cs
public class Branch
{
    public Guid   BranchId  { get; set; } = Guid.NewGuid();
    public Guid   TenantId  { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Address   { get; set; } = string.Empty;
    public bool   IsActive  { get; set; } = true;
    public Tenant Tenant    { get; set; } = null!;
}
```

### Step 2 — Apply ITenantEntity Interface

```csharp
// RewashPlus.Domain/Interfaces/ITenantEntity.cs
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

// RewashPlus.Domain/Interfaces/IBranchEntity.cs
public interface IBranchEntity : ITenantEntity
{
    Guid BranchId { get; set; }
}
```

Apply to: `Customer`, `Vehicle`, `Booking`, `Payment`, `Service`, `StaffProfile`

### Step 3 — Modify Existing Entities (Additive)

```csharp
// Booking.cs — add these properties
public Guid   TenantId  { get; set; }
public Guid   BranchId  { get; set; }
public Guid   VehicleId { get; set; }        // replaces CarDescription
public string? CarDescription { get; set; } // keep nullable — deprecate gradually
public byte[] RowVersion { get; set; } = Array.Empty<byte>();
```

### Step 4 — Tenant Resolution Middleware (API)

```csharp
// RewashPlus.Api/Middleware/TenantResolutionMiddleware.cs
public class TenantResolutionMiddleware
{
    // Resolve TenantId from JWT claim "tenant_id"
    // Inject into ITenantContext scoped service
    // All controllers get TenantId without manual extraction
}

// Registration in Program.cs
app.UseMiddleware<TenantResolutionMiddleware>();
```

### Step 5 — EF Core Global Query Filters

```csharp
// RewashDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Booking>()
        .HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
    // ...repeat for all tenant-scoped entities
}
```

### Step 6 — Client-Side Tenant Context

```csharp
// Services/TenantContextService.cs (Client project)
public class TenantContextService
{
    public Guid   TenantId  { get; private set; }
    public Guid   BranchId  { get; private set; }
    public string BranchName { get; private set; } = string.Empty;

    // Populated from JWT claims on login
    // Persisted to LocalStorage for offline access
    public Task LoadFromTokenAsync(string jwt) { ... }
}
```

### Step 7 — Apply TenantId to All Client Services

Each service call appends `TenantId`/`BranchId` from `TenantContextService`:

```csharp
// Example in JobService
public async Task<Booking> CreateAsync(Booking booking)
{
    booking.TenantId = _tenantContext.TenantId;
    booking.BranchId = _tenantContext.BranchId;
    // ... persist to IndexedDB
}
```

### Step 8 — Feature Toggles

```csharp
// RewashPlus.Domain/Entities/TenantFeature.cs
public class TenantFeature
{
    public int    Id          { get; set; }
    public Guid   TenantId   { get; set; }
    public string FeatureCode { get; set; } = string.Empty;  // "MobileWash", "Loyalty", etc.
    public bool   IsEnabled  { get; set; }
}

// Known feature codes — use constants to avoid magic strings
public static class FeatureCodes
{
    public const string MobileWash      = "MobileWash";
    public const string PickupDropoff   = "PickupDropoff";
    public const string Loyalty         = "Loyalty";
    public const string Inventory       = "Inventory";
    public const string FleetAccounts   = "FleetAccounts";
    public const string QueueDisplay    = "QueueDisplay";
}
```

```csharp
// Services/FeatureToggleService.cs (Client)
public class FeatureToggleService
{
    private List<TenantFeature> _features = new();

    public bool IsEnabled(string featureCode) =>
        _features.FirstOrDefault(f => f.FeatureCode == featureCode)?.IsEnabled ?? false;

    // Load from /api/v1/tenants/{id}/features on login; cache offline
    public async Task LoadFeaturesAsync() { ... }
}
```

Usage in Razor:
```razor
@inject FeatureToggleService Features

@if (Features.IsEnabled(FeatureCodes.Loyalty))
{
    <LoyaltyPointsWidget />
}
```

---

## 9. RISKS

| Risk | Severity | Likelihood | Mitigation |
|---|---|---|---|
| **Type mismatch in `BookingsHistory.razor.cs`** blocks compile immediately | High | Certain | Fix in Phase 0 (1 day) |
| **Dual ownership of bookings** between `BookingService` and `JobService` causes data loss if both are used | High | Likely | Merge into `JobService` in Phase 0 |
| **LocalStorage 5MB limit** hit by large offline datasets (fleet accounts, many bookings) | High | Likely for fleets | Migrate to IndexedDB in Phase 4 |
| **No backend yet** — all API calls in existing services point to placeholder URLs | High | Certain | Phase 3 priority |
| **Conflict resolution UX** — users may not understand merge dialogs | Medium | Likely | Design simple "server wins" as default; flag for manual review |
| **IndexedDB JS interop complexity** — Blazor WASM + IndexedDB requires careful JS isolation | Medium | Medium | Use well-maintained wrapper (Blazored.IndexedDB or TG.Blazor.IndexedDB) |
| **EF Core Global Query Filters accidentally bypassed** via `IgnoreQueryFilters()` | Medium | Low | Code review gate; wrap in audit |
| **JWT claims not carrying TenantId** if auth is added carelessly | High | Medium | Define claim schema before auth implementation; use constants |
| **Feature toggle bloat** — too many flags create combinatorial testing complexity | Low | Low | Max 10 named features; document each |
| **Azure Function cold starts** for Outbox processor add latency | Low | Low | Keep alive with timer trigger every 30s |
| **Mobile washing scenario** — vehicle is offline for hours | Medium | Likely | Sync queue designed for large gaps; timestamp-based download |
| **Tenant data leakage** if query filter is missed on a new entity | Critical | Low | Create `ITenantEntity` interface enforced by base config; write integration test per entity |

---

## 10. INCREMENTAL IMPLEMENTATION PLAN

### Sprint 0 — Stabilize Demo (Week 1)
**Goal:** Get existing demo compiling and consistent before any new work.

| Task | File(s) | Effort |
|---|---|---|
| Fix `BookingsHistory.razor.cs` type mismatch | `Pages/BookingsHistory.razor.cs` | 2h |
| Remove duplicate `BookingService` — merge into `JobService` | `Services/BookingService.cs` | 2h |
| Rename `Car` → `Vehicle`, `CarService` → `VehicleService` | `Models/Car.cs`, `Services/CarService.cs` | 2h |
| Update `Program.cs` after service renames | `Program.cs` | 30min |
| Verify all pages compile and render | All `.razor` | 1h |

### Sprint 1 — Solution Structure (Week 2)
**Goal:** Create multi-project solution; move existing code into correct projects.

| Task | Effort |
|---|---|
| Create new `RewashPlus.sln` with project stubs | 4h |
| Move `Models/` → `RewashPlus.Domain/Entities/` | 2h |
| Create `RewashPlus.Contracts/` with initial DTOs | 4h |
| Create `RewashPlus.Api/` stub with `Program.cs` + health check | 2h |
| Rename demo WASM project to `RewashPlus.Client/` | 2h |
| Verify solution builds end-to-end | 1h |

### Sprint 2 — Domain Entities + Database (Week 3–4)
**Goal:** Full domain model; EF Core context + first migration.

| Task | Effort |
|---|---|
| Add `Tenant`, `Branch`, `AppUser`, `StaffProfile` entities | 4h |
| Modify `Booking`, `Customer`, `Vehicle`, `Payment`, `Service` (add `TenantId`, `BranchId`, `RowVersion`) | 4h |
| Add `TenantFeature`, `BranchSettings`, `AuditLog`, `OutboxMessage` | 3h |
| Create `RewashDbContext` with entity configs | 4h |
| Apply EF Core global query filters | 2h |
| Run `Migration 001 — Initial` | 1h |
| Test migrations against local SQL Server | 2h |

### Sprint 3 — API Foundation (Week 5–6)
**Goal:** Core API endpoints working with Postman.

| Task | Effort |
|---|---|
| `TenantResolutionMiddleware` | 3h |
| `AuditMiddleware` | 3h |
| Bookings CRUD endpoints | 6h |
| Customers CRUD endpoints | 4h |
| Vehicles CRUD endpoints | 3h |
| Payments endpoints | 3h |
| Services catalog endpoints | 3h |
| JWT auth (`/api/v1/auth/login`, `/refresh`) | 4h |
| Wire `TenantId`/`BranchId` from JWT claims into EF query filters | 2h |

### Sprint 4 — Offline-First Upgrade (Week 7–8)
**Goal:** Proper IndexedDB + SyncService replacing LocalStorage approach.

| Task | Effort |
|---|---|
| Add `ConnectivityService` with `navigator.onLine` + event listeners | 3h |
| Add `OfflineDatabaseService` (IndexedDB wrapper) | 6h |
| Migrate `CustomerService` to `OfflineDatabaseService` | 3h |
| Migrate `JobService` to `OfflineDatabaseService` | 3h |
| Migrate `PaymentService` to `OfflineDatabaseService` | 3h |
| Migrate `VehicleService` to `OfflineDatabaseService` | 2h |
| Extract `SyncService` with `SyncAllAsync()` | 4h |
| Implement `POST /api/v1/sync/upload` + `GET /api/v1/sync/download` | 5h |
| Conflict resolution — HTTP 409 handling with client fallback | 4h |

### Sprint 5 — Multi-Tenancy (Week 9–10)
**Goal:** Tenant and branch isolation live end-to-end.

| Task | Effort |
|---|---|
| `TenantContextService` (client) — load from JWT, persist offline | 4h |
| Apply `TenantId`/`BranchId` injection in all client services | 4h |
| `FeatureToggleService` + `FeatureCodes` constants | 3h |
| Feature-gated UI components (Loyalty, QueueDisplay, etc.) | 4h |
| `BranchSettings` — API + `Admin/Settings.razor` implementation | 5h |
| End-to-end test: two tenants, verify data isolation | 4h |

### Sprint 6 — Admin Portal + Reporting (Week 11–12)
**Goal:** Working admin dashboard scoped to branch.

| Task | Effort |
|---|---|
| Wire `Admin/AdminDashboard.razor` to real API | 4h |
| Implement `Admin/ManagePayments.razor` | 4h |
| Implement `Admin/Reports.razor` (revenue, booking counts per branch) | 6h |
| Implement `Admin/Users.razor` (staff management) | 4h |
| Implement `Admin/Settings.razor` (branch settings + feature toggles) | 5h |

### Sprint 7 — Azure Deployment (Week 13–14)
**Goal:** Both Client and API running in Azure.

| Task | Effort |
|---|---|
| Azure SQL — provision and run `MigrationBundle` | 3h |
| Azure Static Web Apps — deploy `RewashPlus.Client` | 3h |
| Azure App Service — deploy `RewashPlus.Api` | 3h |
| Azure Blob Storage — wire for receipt/document uploads | 3h |
| Azure Functions — Outbox processor | 4h |
| Firebase — push notification setup | 4h |
| CI/CD pipeline (GitHub Actions or Azure DevOps) | 5h |

---

## APPENDIX A — Entity Design Reference

### Booking (Modified)
```csharp
public class Booking : IBranchEntity
{
    public Guid   BookingId       { get; set; } = Guid.NewGuid();
    public string JobNumber       { get; set; } = string.Empty;
    public Guid   TenantId        { get; set; }
    public Guid   BranchId        { get; set; }
    public Guid   CustomerId      { get; set; }
    public Guid   VehicleId       { get; set; }          // replaces CarDescription
    public string? CarDescription { get; set; }          // deprecated — kept for migration
    public List<JobService> Services { get; set; } = new();
    public JobStatus  Status      { get; set; } = JobStatus.Waiting;
    public JobType    Type        { get; set; } = JobType.WalkIn;
    public DateTime   AppointmentAt { get; set; }
    public DateTime?  ArrivedAt   { get; set; }
    public DateTime?  StartedAt   { get; set; }
    public DateTime?  CompletedAt { get; set; }
    public decimal    TotalAmount { get; set; }
    public Guid?      AssignedStaffProfileId { get; set; }  // FK to StaffProfile
    public string     Notes       { get; set; } = string.Empty;
    public byte[]     RowVersion  { get; set; } = Array.Empty<byte>();
    public DateTime   CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime   UpdatedAt   { get; set; } = DateTime.UtcNow;
}
```

### Vehicle (Renamed from Car + Extended)
```csharp
public class Vehicle : ITenantEntity
{
    public Guid   VehicleId           { get; set; } = Guid.NewGuid();
    public Guid   TenantId            { get; set; }
    public Guid   CustomerId          { get; set; }
    public string RegistrationNumber  { get; set; } = string.Empty;  // was Plate
    public string Make                { get; set; } = string.Empty;
    public string Model               { get; set; } = string.Empty;
    public string Color               { get; set; } = string.Empty;
    public int?   Year                { get; set; }
    public string VinNumber           { get; set; } = string.Empty;
    public VehicleCategory Category   { get; set; } = VehicleCategory.Sedan;
    public byte[] RowVersion          { get; set; } = Array.Empty<byte>();
    public bool   IsActive            { get; set; } = true;
    public DateTime CreatedAt         { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt         { get; set; } = DateTime.UtcNow;
}

public enum VehicleCategory { Sedan, SUV, Bakkie, Minibus, Motorbike, Truck, Other }
```

### StaffProfile (New — Composition)
```csharp
public class StaffProfile : IBranchEntity
{
    public Guid   StaffProfileId { get; set; } = Guid.NewGuid();
    public Guid   UserId         { get; set; }   // FK to AppUser
    public Guid   TenantId       { get; set; }
    public Guid   BranchId       { get; set; }
    public string Role           { get; set; } = string.Empty;  // "Washer", "Cashier", "Manager"
    public bool   IsActive       { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
}
```

### BranchSettings (New)
```csharp
public class BranchSettings
{
    public Guid   BranchSettingsId      { get; set; } = Guid.NewGuid();
    public Guid   BranchId              { get; set; }
    public int    MaxBookingsPerHour    { get; set; } = 10;
    public double ServiceRadiusKm      { get; set; } = 0;
    public bool   SupportsPickupDropoff { get; set; }
    public bool   QueueDisplayEnabled  { get; set; }
    public bool   AllowWalkIns         { get; set; } = true;
    public string OperatingHoursJson   { get; set; } = "{}";  // JSON of daily hours
}
```

### AuditLog (New)
```csharp
public class AuditLog
{
    public long   AuditLogId   { get; set; }
    public Guid   TenantId     { get; set; }
    public Guid   UserId       { get; set; }
    public string EntityName   { get; set; } = string.Empty;
    public string EntityId     { get; set; } = string.Empty;
    public string Action       { get; set; } = string.Empty;  // "Create", "Update", "Delete"
    public string? OldValues   { get; set; }  // JSON
    public string? NewValues   { get; set; }  // JSON
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
```

---

## APPENDIX B — What Stays Exactly As-Is

These files require **no changes** in initial phases:

- `Layout/MainLayout.razor` — keep
- `Layout/MainLayout.razor.css` — keep
- `Layout/NavMenu.razor` — keep (extend links later)
- `Layout/NavMenu.razor.cs` — keep
- `Models/JobService.cs` — keep (line item model is correct)
- `Models/Payment.cs` enums — keep (`PaymentMethod`, `PaymentStatus`)
- `Models/Booking.cs` enums — keep (`JobStatus`, `JobType`) — move to Domain/Enums/ later
- `wwwroot/service-worker.js` — keep
- `wwwroot/manifest.webmanifest` — keep
- `wwwroot/css/` — keep all Tailwind config
- `tailwind.config.js` — keep
- `postcss.config.js` — keep
- `Services/PageTitleService.cs` — keep as-is
