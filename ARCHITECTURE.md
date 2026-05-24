# ReWash+ PWA - Complete System Architecture

## Executive Summary
ReWash+ is a Progressive Web App (PWA) designed for **Fourways Mall Car Wash** to digitize and optimize daily operations. The system is built on **Blazor WebAssembly** (frontend) with offline-first capability and **ASP.NET Core Web API** (backend) for data synchronization.

---

## 1. SYSTEM ARCHITECTURE OVERVIEW

### 1.1 Architecture Pattern: Clean Architecture + Layered

```
???????????????????????????????????????????????????????????
?                    FRONTEND (WASM)                      ?
?  ReWashPlus_DemoApp (Blazor WebAssembly + PWA)         ?
?  - Components (Pages & Layouts)                         ?
?  - Services (Business Logic)                            ?
?  - Models (DTOs)                                        ?
?  - Offline Storage (IndexedDB via Blazored.LocalStorage)?
???????????????????????????????????????????????????????????
                         ? HTTP/HTTPS
                         ?
???????????????????????????????????????????????????????????
?                  BACKEND (WEB API)                      ?
?        ReWashPlus.Api (.NET 8 Web API)                  ?
???????????????????????????????????????????????????????????
? Controllers Layer                                       ?
? ??? JobController        (Job/Booking Management)      ?
? ??? CustomerController   (Customer Management)         ?
? ??? CarController        (Car Management)              ?
? ??? ServiceController    (Service & Pricing)           ?
? ??? PaymentController    (Payment Tracking)            ?
? ??? DashboardController  (Analytics & Reporting)       ?
???????????????????????????????????????????????????????????
? Application Services Layer                              ?
? ??? JobService           (Job operations)              ?
? ??? CustomerService      (Customer operations)         ?
? ??? CarService           (Car operations)              ?
? ??? ServiceService       (Service operations)          ?
? ??? PaymentService       (Payment operations)          ?
? ??? ReportService        (Analytics)                   ?
???????????????????????????????????????????????????????????
? Domain Layer (Core Business Logic)                      ?
? ??? Entities/            (Job, Customer, Car, etc.)    ?
? ??? Enums/               (JobStatus, PaymentMethod, etc.)|
? ??? Specifications/      (Query Specifications)        ?
???????????????????????????????????????????????????????????
? Infrastructure Layer                                    ?
? ??? Data/                (DbContext, Migrations)       ?
? ??? Repositories/        (CRUD operations)             ?
? ??? ExternalServices/    (Payment APIs, etc.)          ?
???????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????
?            DATABASE (SQL SERVER)                        ?
?  ReWashPlus_DB                                          ?
???????????????????????????????????????????????????????????
```

---

## 2. DATABASE SCHEMA (SQL Server)

### 2.1 Core Tables

#### **Customers Table**
```sql
CREATE TABLE Customers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PhoneNumber NVARCHAR(20) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    PreferredPaymentMethod INT, -- 0: Cash, 1: Card, 2: EFT
    TotalSpent DECIMAL(10,2) DEFAULT 0,
    LoyaltyPoints INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1,
    INDEX IX_PhoneNumber (PhoneNumber),
    INDEX IX_Email (Email)
);
```

#### **Cars Table**
```sql
CREATE TABLE Cars (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    Plate NVARCHAR(20) NOT NULL UNIQUE,
    Make NVARCHAR(100) NOT NULL,        -- Brand: Toyota, BMW, etc.
    Model NVARCHAR(100) NOT NULL,       -- Model: Corolla, X5, etc.
    Year INT,
    Color NVARCHAR(50),
    VinNumber NVARCHAR(50),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    INDEX IX_CustomerId (CustomerId),
    INDEX IX_Plate (Plate)
);
```

#### **Services Table**
```sql
CREATE TABLE Services (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL UNIQUE,  -- "Basic Wash", "Deluxe", etc.
    Description NVARCHAR(500),
    Price DECIMAL(10,2) NOT NULL,
    DurationMinutes INT DEFAULT 30,      -- Estimated time
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_Name (Name)
);
```

#### **Jobs Table** (Main business entity)
```sql
CREATE TABLE Jobs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    CarId INT NOT NULL,
    JobNumber NVARCHAR(50) NOT NULL UNIQUE, -- JOB-20250120-001
    Status INT NOT NULL DEFAULT 0,      -- 0: Waiting, 1: In Progress, 2: Completed, 3: Cancelled
    JobType INT NOT NULL DEFAULT 0,     -- 0: Walk-in, 1: Pre-booked
    ScheduledDate DATETIME2,            -- For pre-bookings
    ArrivedAt DATETIME2,
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    AssignedStaffId INT,                -- Can be NULL for unassigned
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (CarId) REFERENCES Cars(Id),
    INDEX IX_CustomerId (CustomerId),
    INDEX IX_CarId (CarId),
    INDEX IX_Status (Status),
    INDEX IX_CreatedAt (CreatedAt),
    INDEX IX_JobNumber (JobNumber)
);
```

#### **JobServices Table** (Join table: Job -> Services)
```sql
CREATE TABLE JobServices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    JobId INT NOT NULL,
    ServiceId INT NOT NULL,
    Quantity INT DEFAULT 1,
    PriceAtTime DECIMAL(10,2) NOT NULL, -- Historical price
    AddedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (JobId) REFERENCES Jobs(Id) ON DELETE CASCADE,
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    INDEX IX_JobId (JobId),
    UNIQUE (JobId, ServiceId)
);
```

#### **Payments Table**
```sql
CREATE TABLE Payments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    JobId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Method INT NOT NULL,                -- 0: Cash, 1: Card, 2: EFT
    Status INT NOT NULL DEFAULT 0,      -- 0: Pending, 1: Completed, 2: Failed
    Reference NVARCHAR(100),            -- Card/EFT reference
    PaidAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (JobId) REFERENCES Jobs(Id),
    INDEX IX_JobId (JobId),
    INDEX IX_Status (Status),
    INDEX IX_PaidAt (PaidAt)
);
```

#### **Staff Table** (Optional: For job assignment)
```sql
CREATE TABLE Staff (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50),                  -- "Washer", "Manager", etc.
    Phone NVARCHAR(20),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

#### **AuditLog Table** (For compliance & troubleshooting)
```sql
CREATE TABLE AuditLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EntityType NVARCHAR(100),           -- "Job", "Payment", etc.
    EntityId INT,
    Action NVARCHAR(50),                -- "Created", "Updated", "Deleted"
    ChangedData NVARCHAR(MAX),          -- JSON
    ChangedBy NVARCHAR(100),            -- User or "System"
    ChangedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### 2.2 Database Relationships Diagram

```
Customers (1) ??????????? (M) Cars
    ?                          ?
    ?                          ?
    ????????????????????????????
    ?             ?            ?
    ?             ?            ?
   Jobs ?????????? JobServices ???? Services
    ?
    ?? Payments
    ?? (Optional) JobHistory (Denormalized view)

Staff (1) ??????????? (M) Jobs (via AssignedStaffId)
```

---

## 3. API ENDPOINT DESIGN

### 3.1 Customer Endpoints
```
POST   /api/customers              Create new customer
GET    /api/customers/{id}         Get customer details
PUT    /api/customers/{id}         Update customer
GET    /api/customers/search        Search by phone/name
GET    /api/customers/{id}/cars     Get customer's cars
GET    /api/customers/{id}/jobs     Get customer's job history
GET    /api/customers/{id}/loyalty  Get loyalty points
```

### 3.2 Car Endpoints
```
POST   /api/cars                   Create new car
GET    /api/cars/{id}              Get car details
PUT    /api/cars/{id}              Update car
DELETE /api/cars/{id}              Soft-delete car
GET    /api/cars/{id}/jobs         Get car's wash history
```

### 3.3 Service Endpoints
```
GET    /api/services               List all services
GET    /api/services/{id}          Get service details
POST   /api/services               Create service (Admin)
PUT    /api/services/{id}          Update service (Admin)
```

### 3.4 Job/Booking Endpoints
```
POST   /api/jobs                   Create new job (walk-in or booking)
GET    /api/jobs/{id}              Get job details
PUT    /api/jobs/{id}              Update job status
GET    /api/jobs/active             List active jobs
GET    /api/jobs/today              Today's jobs
POST   /api/jobs/{id}/services      Add service to job
DELETE /api/jobs/{id}/services/{sid} Remove service from job
POST   /api/jobs/{id}/complete      Mark job as completed
```

### 3.5 Payment Endpoints
```
POST   /api/payments               Record payment
GET    /api/payments/{id}          Get payment details
GET    /api/jobs/{jobId}/payments  Get payments for a job
GET    /api/payments/daily-summary  Daily revenue summary
```

### 3.6 Dashboard/Analytics Endpoints
```
GET    /api/dashboard/summary      Daily summary (total cars, revenue, etc.)
GET    /api/dashboard/jobs-status  Job count by status
GET    /api/dashboard/revenue      Revenue over time
GET    /api/reports/daily          Detailed daily report
```

---

## 4. FOLDER STRUCTURE

### Frontend (Blazor WASM)
```
ReWashPlus_DemoApp/
??? Pages/                          # Page components
?   ??? Home.razor                  # Dashboard/Home
?   ??? BookWash.razor              # Book/Create new job
?   ??? JobDetail.razor             # Job details & management
?   ??? CustomerSearch.razor        # Find customer
?   ??? CustomerProfile.razor       # Customer details
?   ??? CarManagement.razor         # Add/edit cars
?   ??? PaymentRecord.razor         # Record payment
?   ??? JobHistory.razor            # Customer job history
?   ??? Admin/
?       ??? AdminDashboard.razor    # Overview
?       ??? AdminManageBookings.razor
?       ??? AdminManagePayments.razor
?       ??? AdminManageServices.razor
?       ??? AdminManageStaff.razor
?       ??? AdminReports.razor
??? Layout/
?   ??? MainLayout.razor
?   ??? NavMenu.razor
?   ??? NavMenu.razor.cs
??? Components/                     # Reusable components
?   ??? JobCard.razor
?   ??? PaymentForm.razor
?   ??? ServiceSelector.razor
?   ??? CustomerForm.razor
?   ??? StatusBadge.razor
?   ??? ConfirmDialog.razor
??? Models/                         # DTOs
?   ??? Booking.cs
?   ??? Customer.cs
?   ??? Car.cs
?   ??? Job.cs
?   ??? Service.cs
?   ??? Payment.cs
?   ??? Enums/
?       ??? JobStatus.cs
?       ??? PaymentMethod.cs
?       ??? JobType.cs
??? Services/                       # Business logic
?   ??? PageTitleService.cs
?   ??? JobService.cs              # Job CRUD + sync
?   ??? CustomerService.cs         # Customer CRUD + sync
?   ??? CarService.cs              # Car CRUD + sync
?   ??? PaymentService.cs          # Payment CRUD + sync
?   ??? ServiceService.cs          # Service catalog
?   ??? SyncService.cs             # Offline-first sync
?   ??? NotificationService.cs     # Toast/notifications
?   ??? AuthService.cs             # (Future) Authentication
??? wwwroot/
?   ??? index.html
?   ??? manifest.webmanifest        # PWA manifest
?   ??? service-worker.js           # Service worker
?   ??? service-worker.published.js
?   ??? css/
?   ?   ??? app.css
?   ?   ??? tailwind.css
?   ??? icons/
?   ?   ??? icon-192.png
?   ?   ??? icon-512.png
?   ?   ??? favicon.png
?   ??? images/
?       ??? (brand assets)
??? _Imports.razor                  # Global imports
??? App.razor
??? Program.cs
??? ReWashPlus_DemoApp.csproj
```

### Backend (ASP.NET Core Web API - Future)
```
ReWashPlus.Api/
??? Controllers/
?   ??? CustomersController.cs
?   ??? CarsController.cs
?   ??? JobsController.cs
?   ??? ServicesController.cs
?   ??? PaymentsController.cs
?   ??? DashboardController.cs
?   ??? ReportsController.cs
??? Application/
?   ??? Services/
?   ?   ??? CustomerService.cs
?   ?   ??? CarService.cs
?   ?   ??? JobService.cs
?   ?   ??? PaymentService.cs
?   ?   ??? ReportService.cs
?   ??? DTOs/
?   ?   ??? CreateJobDto.cs
?   ?   ??? JobDetailDto.cs
?   ?   ??? CreatePaymentDto.cs
?   ?   ??? DashboardSummaryDto.cs
?   ??? Mappings/
?       ??? MappingProfile.cs
??? Domain/
?   ??? Entities/
?   ?   ??? Customer.cs
?   ?   ??? Car.cs
?   ?   ??? Job.cs
?   ?   ??? Service.cs
?   ?   ??? Payment.cs
?   ?   ??? Staff.cs
?   ??? Enums/
?   ?   ??? JobStatus.cs
?   ?   ??? PaymentMethod.cs
?   ?   ??? PaymentStatus.cs
?   ??? Specifications/
?       ??? JobSpecification.cs
?       ??? CustomerSpecification.cs
??? Infrastructure/
?   ??? Data/
?   ?   ??? ReWashPlusDbContext.cs
?   ?   ??? Migrations/
?   ?   ??? DatabaseSeeder.cs
?   ??? Repositories/
?   ?   ??? GenericRepository.cs
?   ?   ??? CustomerRepository.cs
?   ?   ??? JobRepository.cs
?   ?   ??? ...
?   ??? ExternalServices/
?       ??? EmailService.cs (future)
?       ??? SmsService.cs (future)
??? Middleware/
?   ??? ErrorHandlingMiddleware.cs
?   ??? LoggingMiddleware.cs
??? appsettings.json
??? Program.cs
??? ReWashPlus.Api.csproj
```

---

## 5. CORE WORKFLOWS

### 5.1 Create Job (Walk-in Customer)

```
User Input ? Search/Create Customer
    ?
Select/Add Car
    ?
Select Services & Qty
    ?
Create Job (Status: Waiting)
    ?
[OFFLINE] Store in LocalStorage
    ?
[ONLINE] POST /api/jobs ? Backend
    ?
Server returns Job ID
    ?
Sync successful ? Update local record (IsSynced=true)
    ?
Display Job Details
```

### 5.2 Complete Job Workflow

```
Job Status: Waiting
    ?
Staff Clicks "Start Job" ? Status: In Progress, StartedAt = Now
    ?
[OFFLINE] Update LocalStorage
    ?
[ONLINE] PUT /api/jobs/{id} ? Backend
    ?
Staff Completes Services
    ?
Click "Mark Complete" ? Status: Completed, CompletedAt = Now
    ?
[OFFLINE] Update LocalStorage
    ?
[ONLINE] PUT /api/jobs/{id} ? Backend
    ?
Display "Ready for Payment" Alert
```

### 5.3 Payment Processing Flow

```
Job Completed
    ?
Display Payment Form (Services + Total)
    ?
Select Payment Method (Cash/Card/EFT)
    ?
Record Payment
    ?
[OFFLINE] Store Payment record in LocalStorage
    ?
[ONLINE] POST /api/payments ? Backend
    ?
Server confirms payment
    ?
Update Job Payment Status
    ?
[OPTIONAL] Add Loyalty Points to Customer
    ?
Print Receipt / Send SMS/Email Notification
```

### 5.4 Offline-First Sync Logic

```
App Starts
    ?
Check Browser Navigator.onLine
    ?
IF Online:
  ?? Load pending records from LocalStorage
  ?? Batch sync to backend
  ?? On success: mark as synced, move to history
  ?? On failure: retry later, show notification
  ?? Refresh UI with server data
ELSE (Offline):
  ?? Load data from LocalStorage only
  ?? Enable form submission (stored locally)
  ?? Show "Offline Mode" badge
    ?
Monitor online/offline events
    ?
When connection restored ? Auto-sync pending records
```

---

## 6. TECHNICAL IMPLEMENTATION DETAILS

### 6.1 Frontend Technologies
- **Blazor WebAssembly** (.NET 8) - UI & business logic
- **Blazored.LocalStorage** - Offline data persistence
- **Tailwind CSS** - Styling (mobile-first, dark theme)
- **Font Awesome** - Icons
- **Service Worker** - Offline support & caching

### 6.2 Backend Technologies (Future Implementation)
- **ASP.NET Core 8 Web API** - RESTful backend
- **Entity Framework Core 8** - ORM
- **SQL Server** - Primary database
- **AutoMapper** - DTO mapping
- **Specification Pattern** - Query abstraction

### 6.3 Key Packages
```xml
<!-- Frontend -->
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.19" />

<!-- Backend (Future) -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.0" />
```

---

## 7. DATA MODELS (C# Classes)

### Core Entities for Blazor WASM

**Job (Booking)**
```csharp
public class Job
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int CarId { get; set; }
    public string JobNumber { get; set; }  // JOB-20250120-001
    public JobStatus Status { get; set; }  // Waiting, In Progress, Completed
    public JobType Type { get; set; }      // WalkIn, PreBooked
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<JobService> Services { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Customer**
```csharp
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }  // Unique
    public string Email { get; set; }
    public List<Car> Cars { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal TotalSpent { get; set; }
}
```

---

## 8. OFFLINE-FIRST STRATEGY

### 8.1 Local Storage Keys
```
rw_customers        ? List<Customer>
rw_cars             ? List<Car>
rw_services         ? List<Service> (cached)
rw_pending_jobs     ? List<Job> (unsync'd)
rw_synced_jobs      ? List<Job> (sync'd)
rw_pending_payments ? List<Payment> (unsync'd)
rw_sync_queue       ? Queue of pending operations
rw_last_sync        ? DateTime
```

### 8.2 Sync Rules
- **Conflict Resolution**: Last-write-wins
- **Retry Strategy**: Exponential backoff (1s, 2s, 4s, 8s, 16s)
- **Batch Size**: Max 50 records per sync request
- **Stale Data**: Cache services for 24 hours

---

## 9. SECURITY CONSIDERATIONS

1. **Authentication** (Future):
   - JWT token in Authorization header
   - Refresh token rotation
   - Store token in secure cookie (HttpOnly)

2. **Data Protection**:
   - HTTPS only (enforce in appsettings)
   - Encrypt sensitive fields in DB (phone, email)
   - Audit log all changes

3. **Input Validation**:
   - Server-side validation (never trust client)
   - Sanitize HTML input
   - Rate limiting on API endpoints

4. **CORS**:
   - Whitelist only trusted origins
   - No credentials for public endpoints

---

## 10. SCALING CONSIDERATIONS

### 10.1 Single Location (Current)
- SQLite backup for dev
- LocalStorage for mobile offline
- Simple REST API

### 10.2 Multi-Branch (Future)
```
Add these tables:
- Branches (Id, Name, Location, Address)
- BranchStaff (StaffId, BranchId)
- BranchServices (ServiceId, BranchId, Price)

Modify endpoints:
- GET /api/branches/{branchId}/jobs
- GET /api/branches/{branchId}/dashboard
- POST /api/branches/{branchId}/jobs

Add multi-tenancy:
- Tenant header in requests
- Row-level security in DB
- Separate caches per tenant
```

### 10.3 Performance Optimization
- **Caching**: Redis for frequently accessed data (services, loyalty rules)
- **CDN**: Serve static assets (CSS, JS, images) from CDN
- **Database Indexes**: On frequently queried columns (Status, CustomerId, CreatedAt)
- **API Response Compression**: Gzip compression for large responses
- **Pagination**: Limit results to 50 records per page
- **Query Optimization**: Use Select() to fetch only needed columns

---

## 11. MONITORING & LOGGING

### 11.1 Frontend Logging
```csharp
// Log sync events, errors, offline mode changes
// Store in LocalStorage for later analysis
```

### 11.2 Backend Logging
- Structured logging with Serilog (future)
- Log all API requests
- Log sync failures
- Alert on critical errors

### 11.3 Analytics
- Track job completion rate
- Monitor API response times
- Track user actions

---

## 12. DEPLOYMENT

### Frontend (Blazor WASM + PWA)
1. Build: `dotnet publish -c Release`
2. Deploy to:
   - Azure Static Web Apps (recommended)
   - AWS S3 + CloudFront
   - Netlify
   - On-premises IIS

### Backend (ASP.NET Core API - Future)
1. Build: `dotnet publish -c Release`
2. Deploy to:
   - Azure App Service
   - Docker Container
   - On-premises IIS

### Database (SQL Server)
1. Create DB from migration scripts
2. Run seeders for initial data
3. Setup backups

---

## 13. TESTING STRATEGY

### Unit Tests
- Service layer logic
- Helper functions
- Business rules

### Integration Tests
- API endpoint tests
- Database operations
- Sync logic

### E2E Tests (Cypress/Playwright)
- Complete job workflow
- Payment flow
- Offline-online transition

### Performance Tests
- Load testing (k6 or JMeter)
- Sync time benchmarks
- UI responsiveness on slow connections

---

## 14. CURRENT PROGRESS & NEXT STEPS

### ? COMPLETED
- [x] Basic Blazor WASM project setup
- [x] PWA manifest & service worker
- [x] Tailwind CSS theming (ClickUp dark theme)
- [x] Navigation layout & sidebar
- [x] Booking model
- [x] BookingService (basic offline storage)
- [x] Page components (Home, BookWash, BookingsHistory)

### ?? IN PROGRESS
- [ ] Complete missing models (Customer, Car, Service, Payment)
- [ ] Create remaining page components
- [ ] Implement full sync service
- [ ] Add data validation

### ?? TODO (Phase 2+)
- [ ] Backend API development (.NET 8 Web API)
- [ ] Database design & EF Core models
- [ ] Authentication & authorization
- [ ] Advanced analytics dashboard
- [ ] Mobile app icons & branding
- [ ] Payment gateway integration
- [ ] SMS/Email notifications
- [ ] Multi-branch support

---

## 15. KEY DESIGN PATTERNS & PRINCIPLES

- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
- **Clean Architecture**: Separation of concerns, testability
- **Offline-First**: Graceful degradation, eventual consistency
- **Mobile-First CSS**: Responsive, touch-friendly UI
- **DRY (Don't Repeat Yourself)**: Reusable components & services
- **Progressive Enhancement**: Works without JavaScript, enhanced with interactivity

---

## CONCLUSION

ReWash+ is designed as a **modern, scalable, offline-capable PWA** for car wash operations. The architecture supports growth from a single location to multiple branches, with clean separation of concerns and emphasis on reliability in offline scenarios.

The current implementation focuses on the **frontend MVP** with basic offline capability. The **backend API** will be developed in Phase 2 to enable multi-device sync and team collaboration.
