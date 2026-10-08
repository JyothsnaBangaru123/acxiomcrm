# AcxiomCRM — Enterprise CRM Platform

**AcxiomCRM** is a production-grade, enterprise-ready Customer Relationship Management (CRM) web application built using **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**, **ASP.NET Core Identity**, **Bootstrap 5.3**, and **Chart.js**.

---

## 🌟 Executive Summary & Architectural Overview

AcxiomCRM implements a **strict 5-layer enterprise architecture** ensuring separation of concerns, testability, security, and maintainability:

```text
               ┌───────────────────────────────┐
               │    Users / Browser / REST     │
               └───────────────┬───────────────┘
                               │
                               ▼
               ┌───────────────────────────────┐
               │      Presentation Layer       │
               │   (AcxiomCRM.Web / Razor)     │
               └───────────────┬───────────────┘
                               │
                               ▼
               ┌───────────────────────────────┐
               │       Application Layer       │
               │   (Services, DTOs, Handlers)  │
               └───────────────┬───────────────┘
                               │
                               ▼
               ┌───────────────────────────────┐
               │         Domain Layer          │
               │  (Entities, Enums, Rules, DI) │
               └───────────────┬───────────────┘
                               │
                               ▼
               ┌───────────────────────────────┐
               │    Infrastructure / Data      │
               │  (EF Core, Repositories, UoW) │
               └───────────────┬───────────────┘
                               │
                               ▼
               ┌───────────────────────────────┐
               │      Database / Identity      │
               └───────────────────────────────┘

  Cross-Cutting: Authentication + Authorization + Validation + Audit Trail
```

---

## 🏗️ Solution Structure

```text
AcxiomCRM/
├── AcxiomCRM.sln
│
├── AcxiomCRM.Domain/                       # Core Enterprise Domain
│   ├── Entities/                           # Customer, Lead, Opportunity, FollowUp, Activity, AuditLog, User
│   ├── Enums/                              # LeadStatus, OpportunityStage, FollowUpType, Roles, etc.
│   ├── Interfaces/                         # Generic & entity-specific repositories, Unit of Work
│   └── Rules/                              # Pure business validation rules (Opportunity, FollowUp, Lead, Customer)
│
├── AcxiomCRM.Application/                  # Orchestration & Business Services
│   ├── DTOs/                               # Data Transfer Objects (Customers, Leads, Opps, FollowUps, Auth, Dashboard, Reports)
│   ├── Interfaces/                         # Service contracts (ICustomerService, ILeadService, etc.)
│   └── Services/                           # CustomerService, LeadService, OpportunityService, AuditService, DashboardService
│
├── AcxiomCRM.Infrastructure/               # Data Access & Persistence
│   ├── Data/                               # AcxiomCrmDbContext, DbInitializer (seed pipeline)
│   └── Repositories/                       # Generic EF Core Repository, UnitOfWork, specialized queries
│
├── AcxiomCRM.Web/                          # Presentation & API Layer
│   ├── Controllers/                        # MVC Controllers (Dashboard, Customers, Leads, Opportunities, FollowUps, etc.)
│   ├── Controllers/Api/                    # REST API Controllers (Customers, Leads, Opportunities, FollowUps, Auth, Reports)
│   ├── Views/                              # Razor Views with modern Bootstrap 5 styling & Chart.js
│   ├── Services/                           # CurrentUserService, HttpContext claims resolver
│   ├── wwwroot/                            # CSS Design System (site.css), JavaScript (site.js)
│   └── Program.cs                          # App bootstrap, DI registrations, Identity, Swagger, Security
│
└── AcxiomCRM.Tests/                        # xUnit Test Suite (51 Unit, Integration & API Tests)
    ├── UnitTests/                          # Business rule & validation tests
    ├── IntegrationTests/                   # Service & data access tests with In-Memory EF Core
    ├── ApiTests/                           # WebApplicationFactory REST API integration tests
    └── TestHelpers/                        # Test DbContext factory & test builders
```

---

## 🔐 Security & Identity Architecture

1. **Authentication**: Powered by **ASP.NET Core Identity**. No custom password tables or plain-text credentials are used. Passwords use PBKDF2 with HMAC-SHA256 and adaptive per-user salting.
2. **Password Policy**:
   * Minimum length: 8 characters
   * Required character classes: Upper case (`A-Z`), Lower case (`a-z`), Numeric (`0-9`), and Non-alphanumeric special symbols (`!@#$%^&*`).
3. **Account Lockout**:
   * Automatically locks user account after **5 consecutive failed login attempts** for **15 minutes**.
   * Successful authentication immediately resets the failed attempts counter.
   * Administrators can unlock accounts through the user management console.
4. **Anti-Forgery Protection**:
   * All state-changing MVC forms enforce `[ValidateAntiForgeryToken]` and `AutoValidateAntiforgeryToken`.
5. **Data Protection**:
   * Sensitive authentication tokens, passwords, and password hashes are **never returned in API responses, DTOs, views, or logs**.
6. **Append-Only Audit Trail**:
   * All login events (success/failure), logouts, lockouts, record creation, updates, deletions/deactivations, lead conversions, and role mutations are immutably logged with Timestamp, User, Action, Entity, RecordId, and IP Address.

---

## 👥 Role-Based Authorization & Scope Matrix

Server-side authorization is strictly enforced at the service layer:

| Module | Admin | Manager | Sales Executive |
| :--- | :--- | :--- | :--- |
| **Dashboard** | Full enterprise KPIs & Charts | Full team pipeline & KPIs | Assigned records & sales forecast only |
| **Customers** | All customers | All team customers | **Assigned customers only** (`OwnerId == UserId`) |
| **Leads** | All leads | All team leads | **Assigned leads only** (`AssignedTo == UserId`) |
| **Opportunities**| All opportunities | All team opportunities | **Assigned opportunities only** |
| **Follow-Ups** | All follow-ups | All team follow-ups | **Assigned follow-ups only** |
| **Activities** | All activities | All team activities | **Assigned activities only** |
| **Reports** | Enterprise-wide reports | Team & pipeline reports | Own performance & pipeline report |
| **Users & Roles**| Full user/role/lockout management | View-only team members | No access (403 Forbidden) |
| **Audit Logs** | Full audit trail & CSV export | Full audit view | No access (403 Forbidden) |
| **REST APIs** | Full CRUD on authorized endpoints | Full CRUD on authorized endpoints | Scoped to assigned records |

> **Crucial Rule**: A `SalesExecutive` cannot access, modify, or delete another representative's records by manipulating IDs in the URL or HTTP requests. The backend returns `403 Forbidden` / null access.

---

## 📋 Business Rules & Validations

| Business Rule | Enforced On | Expected Outcome / Error Message |
| :--- | :--- | :--- |
| **Opportunity Amount** | Client + Server | Active opportunities require Amount > 0. Reject: `Opportunity Amount must be greater than 0.` |
| **Win Probability** | Client + Server | Value must be between 0 and 100. Reject: `Probability must be between 0 and 100.` |
| **Close Date** | Client + Server | For active deals, close date cannot be in the past. Reject: `Expected Close Date cannot be in the past.` |
| **Follow-Up Date** | Client + Server | For new/planned follow-up: `FollowUpDate >= Today`. Reject: `Follow-up date cannot be earlier than today.` |
| **Customer Email** | Client + Server | Must be valid email format and unique across all customers. Reject: `A customer with this email address already exists.` |
| **Customer Phone** | Client + Server | Must be 10-15 digits. Reject: `Enter a valid phone number (10-15 digits).` |
| **Duplicate Customer**| Server | Prevents identical company name and email combination. Reject: `A customer with identical business name and email already exists.` |
| **Lead Status Workflow**| Server | Restricts invalid jumps. Converted leads are terminal. |
| **Weighted Pipeline** | Calculated | `WeightedPipeline = Amount * Probability / 100` |

---

## 🔄 Lead Conversion Workflow

When a prospective lead is qualified for conversion:
1. Validates the lead is qualified and accessible.
2. Checks for existing customer by email (to prevent duplicate customer creation) or creates a new `Customer` record.
3. If selected, creates an active `Opportunity` linked to the customer and lead with validated Amount, Stage, and Close Date.
4. Updates Lead status to `Converted`, sets `ConvertedCustomerId`, and stamps `ConvertedDate`.
5. Records the full conversion event in the **AuditLog**.

---

## 📊 Dashboard & KPI Analytics

The dashboard features live **Chart.js** charts and key performance indicators:
* **KPI Metrics**: Total Customers, Total Leads, Open Leads, Converted Leads, Total Opportunities, Open Opportunities, Won Opportunities, Lost Opportunities, Total Pipeline Value ($), Weighted Forecast Value ($), Pending Follow-ups, and Overdue Alert Counts.
* **Lead Funnel Doughnut Chart**: Status breakdown across New, Contacted, Qualified, Unqualified, Converted, and Lost.
* **Pipeline Stage Bar Chart**: Real-time monetary values in Qualification, Proposal, Negotiation, Won, and Lost.
* **Monthly Won Sales Trend**: Six-month historical trajectory of closed revenue.
* **Upcoming Schedule**: Quick-action follow-up checklist with completion and rescheduling modals.

---

## 🌐 REST API Endpoints & Swagger

AcxiomCRM includes a secured REST API documented with **Swagger UI** (`/swagger`):

| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Authenticate user & receive profile details | Public / Rate-limited |
| `POST` | `/api/auth/logout` | Terminate session & log audit trail | Authenticated |
| `GET` | `/api/customers` | List & filter customers (paged) | Authorized |
| `GET` | `/api/customers/{id}` | Get customer profile & relations | Authorized |
| `POST` | `/api/customers` | Create new customer | Authorized |
| `PUT` | `/api/customers/{id}` | Update customer | Authorized |
| `DELETE`| `/api/customers/{id}` | Deactivate customer | Authorized |
| `GET` | `/api/leads` | List & filter leads (paged) | Authorized |
| `GET` | `/api/leads/{id}` | Get lead details | Authorized |
| `POST` | `/api/leads` | Create new lead | Authorized |
| `PUT` | `/api/leads/{id}` | Update lead & workflow status | Authorized |
| `POST` | `/api/leads/{id}/convert` | Convert lead to customer & opportunity | Authorized |
| `GET` | `/api/opportunities` | List & filter opportunities | Authorized |
| `GET` | `/api/opportunities/{id}` | Get opportunity details | Authorized |
| `POST` | `/api/opportunities` | Create new opportunity | Authorized |
| `PUT` | `/api/opportunities/{id}` | Update opportunity | Authorized |
| `GET` | `/api/followups` | List & filter follow-ups | Authorized |
| `POST` | `/api/followups` | Schedule follow-up | Authorized |
| `POST` | `/api/followups/{id}/complete` | Mark follow-up as completed | Authorized |
| `POST` | `/api/followups/{id}/reschedule` | Reschedule follow-up | Authorized |
| `GET` | `/api/reports/pipeline` | Aggregate pipeline analytics | Manager / Admin |

---

## 🔑 Pre-Seeded Development Evaluation Credentials

The application automatically seeds representative CRM data and roles on initial run:

| Role | Email / Username | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@acxiomcrm.com` | `Admin@12345!` | Full system administration, all CRM data, users, roles, and audit trail |
| **Sales Manager** | `manager@acxiomcrm.com`| `Manager@12345!` | Full CRM pipeline, team overview, management reports, dashboard |
| **Sales Executive**| `sales@acxiomcrm.com` | `Sales@12345!` | Assigned customer accounts, leads, deals, and follow-ups |
| **Sales Executive**| `john.sales@acxiomcrm.com`| `Sales@12345!`| Assigned customer accounts, leads, deals, and follow-ups |

*(Quick-login buttons are built directly into the login screen for instant one-click evaluation)*

---

## 🧪 Test Suite

Run the full xUnit test suite (51 unit, integration, and API tests):

```bash
dotnet test
```

### Test Coverage Highlights
* `AuthenticationSecurityTests`: Verifies password complexity requirements and lockout duration configurations.
* `OpportunityRuleTests`: Tests amount > 0, probability bounds (0-100), past close date rejection, and weighted calculations.
* `FollowUpRuleTests`: Tests past follow-up date rejection and future date validation.
* `CustomerValidationTests`: Tests email format, 10-15 digit phone validation, and duplicate prevention.
* `LeadWorkflowTests`: Tests valid status transitions, illegal state jumps, and immutability of converted leads.
* `CustomerServiceTests`: Tests customer CRUD, unique email/phone enforcement, and audit trail logging.
* `LeadServiceTests`: Tests complete lead-to-customer conversion workflow.
* `AuthorizationScopeTests`: Tests that sales executives cannot access or tamper with other representatives' records.
* `AuditLoggingTests`: Verifies that mutations create immutable audit trail records with IP and user metadata.
* `CrmRestApiCrudTests`: End-to-end REST API testing using `WebApplicationFactory<Program>`.

---

## 🚀 How to Run Locally

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or .NET 8 / 9 / 10 compatible runtime)

### 1. Clone & Navigate to Project
```bash
git clone <your-repo-link>
cd AcxiomCRM
```

### 2. Build the Solution
```bash
dotnet build
```

### 3. Run the Web Application
```bash
cd AcxiomCRM.Web
dotnet run
```

### 4. Access the Application
* **Web UI**: Open `https://localhost:5001` or `http://localhost:5000` (or the port displayed in the console).
* **Swagger API Docs**: Navigate to `/swagger` (e.g. `https://localhost:5001/swagger`).
* **Database**: Uses SQLite by default (`AcxiomCRM.db`) with zero external database prerequisites. To use SQL Server, set `"DatabaseProvider": "SqlServer"` and configure your connection string in `appsettings.json`.

---

## ✅ Acceptance Test Checklist

- [x] 1. Open AcxiomCRM without authentication $\rightarrow$ protected CRM pages are inaccessible.
- [x] 2. Register/login with valid credentials $\rightarrow$ user reaches Dashboard.
- [x] 3. Create Customer with invalid email $\rightarrow$ client-side and server-side validation blocks submission.
- [x] 4. Create Customer with invalid phone $\rightarrow$ client-side and server-side validation blocks submission.
- [x] 5. Bypass browser validation with crafted request $\rightarrow$ server-side validation rejects invalid data.
- [x] 6. Create Opportunity with Amount $\le 0 \rightarrow$ rejected.
- [x] 7. Create Opportunity with Probability $> 100 \rightarrow$ rejected.
- [x] 8. Set active Opportunity ExpectedCloseDate to past $\rightarrow$ rejected.
- [x] 9. Create new Follow-Up dated before today $\rightarrow$ rejected.
- [x] 10. Login as SalesExecutive $\rightarrow$ only authorized/assigned records are accessible.
- [x] 11. Login as Manager $\rightarrow$ team pipeline/report access is available.
- [x] 12. Login as Admin $\rightarrow$ user/role/audit administration is available.
- [x] 13. Create a CRM record $\rightarrow$ AuditLog entry generated.
- [x] 14. Update a CRM record $\rightarrow$ AuditLog entry generated.
- [x] 15. Delete/deactivate a CRM record $\rightarrow$ AuditLog entry generated.
- [x] 16. Call GET `/api/customers` $\rightarrow$ authorized JSON response returned.
- [x] 17. Call API without authorization $\rightarrow$ unauthorized request rejected (401/403).
- [x] 18. Open Dashboard $\rightarrow$ KPI cards display accurate metrics.
- [x] 19. Open Dashboard charts $\rightarrow$ Chart.js charts display authorized data.
- [x] 20. Verify role-based dashboard data $\rightarrow$ Admin, Manager, and SalesExecutive see correct scopes.
