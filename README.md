# FUNewsTradingSystem

An automated, AI-powered trading analysis web application built with **ASP.NET Core MVC** (.NET 10). The system fetches real-time market news via NewsAPI and uses OpenAI LLMs to perform sentiment and fundamental analysis, ultimately producing actionable portfolio decisions — **BUY**, **SELL**, or **HOLD**.

**Live Application:** [https://prn222-project-funewstradingsystem.onrender.com/](https://prn222-project-funewstradingsystem.onrender.com/)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Features & Roles](#features--roles)
5. [Folder Structure](#folder-structure)
6. [Getting Started](#getting-started)
7. [Default Credentials](#default-credentials)
8. [Deployment](#deployment)
9. [Known Limitations](#known-limitations)

---

## Prerequisites

| Tool | Version / Notes |
|------|-----------------|
| **.NET SDK** | .NET 10 ([download](https://dotnet.microsoft.com/download)) |
| **SQL Server** | SQL Server Express LocalDB *(or any SQL Server instance)* |
| **Visual Studio** | 2022 or newer (recommended) |
| **NewsAPI.org** | Free API key at [newsapi.org](https://newsapi.org) |
| **OpenAI API** | API key with GPT-4o access at [platform.openai.com](https://platform.openai.com) |

---

## Architecture

The project follows a strict **3-tier layered architecture** to enforce separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                     FUNewsTradingSystem.sln                     │
├──────────────┬──────────────────────┬───────────────────────────┤
│     MVC      │   BusinessLayer      │   DataAccessLayer          │
│  (Presentation) │  (Business Logic)  │  (Data Access)             │
├──────────────┼──────────────────────┼───────────────────────────┤
│ Controllers  │ Repositories         │ Entity Models              │
│ Views         │ Services             │ DbContext                  │
│ ViewModels    │ TradingAgentService  │ DTOs                       │
│ wwwroot       │                      │ EF Core Migrations         │
└──────────────┴──────────────────────┴───────────────────────────┘
```

- **MVC** — HTTP pipeline, routing, authentication middleware, Razor views
- **BusinessLayer** — Repository pattern + Service layer + AI Trading Pipeline (NewsAPI + OpenAI)
- **DataAccessLayer** — Entity models, `FUNewsManagementContext`, EF Core migrations

### Authentication & Authorization

Cookie-based authentication with **Claims-based Role Authorization**:

| Role ID | Role Name | Description |
|---------|-----------|-------------|
| `1` | **Staff** | Can run AI analysis, manage categories/tags, view personal report history |
| `2` | **Lecturer/Guest** | Read-only public report viewer |
| `3` | **Admin** | Full account management + statistical reporting |

Policies wired in `Program.cs`:

```csharp
"StaffOnly"      → Role = "1"
"AdminOnly"     → Role = "3"
"StaffOrLecturer" → Role = "1" or "2"
```

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | Microsoft SQL Server (LocalDB for dev) |
| Frontend | HTML5, CSS3, Bootstrap 5.3 |
| External APIs | NewsAPI.org, OpenAI API (GPT-4o) |
| Auth | ASP.NET Core Cookie Authentication |
| Config | `appsettings.json` (secrets via env or `appsettings.Development.json`) |

---

## Features & Roles

### Admin (Role 3)

- **Account Management** — Full CRUD for all system accounts (create, edit, delete). Self-deletion is blocked.
- **Statistical Reporting** — Filter generated trading reports by custom UTC date range; results sorted descending by creation date.

### Staff (Role 1)

- **Run AI Analysis** — Select a sector (Category) and ticker (Tag) to trigger the full AI trading pipeline; view the resulting BUY/SELL/HOLD report.
- **Category Management** — Create, edit, soft-delete, and toggle active status of news categories. Supports parent-child hierarchy.
- **Tag Management** — Create and manage ticker symbols. Names are normalized to uppercase; duplicates are rejected.
- **Report History** — View personal reports only; toggle report status (active / archived).
- **Profile Management** — Update display name; change password (current password verification required).

### Lecturer / Guest (Role 2)

- **Public Report Viewer** — Browse all active (published) AI trading reports. No management controls.

---

## Folder Structure

```
FUNewsTradingSystem/
│
├── FUNewsTradingSystem.sln
├── Dockerfile
├── README.md
├── prn222_su26_project.sql               # Database creation script
│
├── FUNewsTradingSystem/
│   ├── DataAccessLayer/
│   │   ├── DataAccessLayer.csproj
│   │   ├── Models/
│   │   │   ├── DTOs/                    # NewsApiArticle, OpenAiRequest/Response, etc.
│   │   │   ├── Category.cs
│   │   │   ├── FUNewsManagementContext.cs
│   │   │   ├── NewsArticle.cs
│   │   │   ├── NewsTag.cs
│   │   │   ├── SystemAccount.cs
│   │   │   └── Tag.cs
│   │   └── Migrations/                  # EF Core migrations
│   │
│   ├── BusinessLayer/
│   │   ├── BusinessLayer.csproj
│   │   ├── Exceptions/
│   │   │   └── PipelineException.cs     # Thrown by TradingAgentService steps
│   │   ├── Repositories/
│   │   │   ├── Interfaces/             # ISystemAccountRepository, ICategoryRepository, etc.
│   │   │   └── Implements/              # Repository implementations
│   │   └── Services/
│   │       ├── Interfaces/              # IAccountService, ICategoryService, ITagService, etc.
│   │       └── Implements/              # Service implementations
│   │                                       + TradingAgentService (AI pipeline)
│   │
│   └── MVC/
│       ├── MVC.csproj
│       ├── Program.cs                   # DI, Auth, Middleware, Auto-migration
│       ├── appsettings.json             # Local secrets (gitignored)
│       ├── appsettings.json.example     # Safe to commit — placeholder values
│       ├── appsettings.Development.json # Dev overrides
│       ├── Controllers/
│       │   ├── AccountController.cs     # Login / Logout
│       │   ├── Admin/
│       │   │   ├── AdminAccountController.cs
│       │   │   └── AdminStatisticsController.cs
│       │   ├── NewsController.cs       # Public report viewer
│       │   └── Staff/
│       │       ├── CategoryController.cs
│       │       ├── StaffController.cs   # Dashboard, Profile, MyReports
│       │       ├── TagController.cs
│       │       └── RunAnalysisController.cs
│       ├── Extensions/
│       │   └── ClaimsPrincipalExtensions.cs
│       ├── Filters/
│       │   └── RoleAuthorizeAttribute.cs
│       ├── ViewModels/
│       │   ├── Account*.cs             # Login, Create, Edit ViewModels
│       │   ├── Category*.cs
│       │   ├── Tag*.cs
│       │   ├── Statistics*.cs
│       │   └── Profile*.cs
│       ├── Views/
│       │   ├── Account/Login.cshtml
│       │   ├── Admin/
│       │   │   ├── Accounts/
│       │   │   └── Statistics/
│       │   ├── News/
│       │   │   ├── Index.cshtml        # Public report list
│       │   │   └── Detail.cshtml       # Public report detail
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml      # Bootstrap 5 navbar, role-gated nav
│       │   │   ├── _ConfirmDeleteModal.cshtml
│       │   │   └── _ValidationScripts.cshtml
│       │   └── Staff/
│       │       ├── Categories/
│       │       ├── Dashboard/
│       │       ├── MyReports/
│       │       ├── Profile/
│       │       ├── RunAnalysis/
│       │       └── Tags/
│       └── wwwroot/
│           ├── css/
│           │   └── site.css            # Decision badges, modal fixes, card hover
│           └── js/
│               ├── accounts.js
│               ├── categories.js
│               ├── modal-helpers.js     # openModal, submitModalForm, confirmDelete
│               ├── run-analysis.js      # Pipeline trigger + spinner + result display
│               ├── tags.js
│               ├── toast-helpers.js     # showSuccess, showError
│               └── validate-extensions.js # dateRange, notSelf, passwordMatch validators
```

---

## Getting Started

### Step 1 — Clone the repository

```bash
git clone https://github.com/your-username/FUNewsTradingSystem.git
cd FUNewsTradingSystem
```

### Step 2 — Fill in `appsettings.json`

Copy the example file and populate your secrets:

```bash
copy FUNewsTradingSystem\MVC\appsettings.json.example FUNewsTradingSystem\MVC\appsettings.json
```

Open `appsettings.json` and replace the placeholder values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FUNewsTradingSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "AdminAccount": {
    "Email": "admin@FUNewsTradingSystem.org",
    "Password": "@@abc123@@",
    "Name": "System Admin"
  },
  "NewsApi": {
    "ApiKey": "YOUR_NEWSAPI_KEY_HERE",
    "BaseUrl": "https://newsapi.org/v2/everything"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "BaseUrl": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4o"
  }
}
```

> **Security note:** `appsettings.json` is gitignored. Never commit your actual API keys.
> For development, you can also override secrets in `appsettings.Development.json`.

### Step 3 — Apply database migrations

From the repository root, run:

```bash
dotnet ef database update --project FUNewsTradingSystem/DataAccessLayer --startup-project FUNewsTradingSystem/MVC
```

The application also **auto-applies pending migrations on startup** (wrapped in a try/catch so it won't crash if the DB is unreachable from the startup shell). Running the command above explicitly is recommended for a clean, verified setup.

### Step 4 — Run the application

```bash
dotnet run --project FUNewsTradingSystem/MVC
```

The application starts at `https://localhost:5001` (or `http://localhost:5000`).

### Step 5 — Seed data verification

On first run, the admin account is seeded automatically. Log in with the credentials below and verify:

- The navbar shows Staff links (Run Analysis, Categories, Tags, My Reports, Profile)
- The Admin section is accessible at `/Admin/Dashboard`
- Seed Categories (Technology, Healthcare, Finance, Energy, Cryptocurrencies, Consumer Goods) appear in the Category dropdown
- Seed Tags (AAPL, NVDA, MSFT, GOOGL, TSLA, BTC, ETH, AMZN) appear in the Tag dropdown

---

## Default Credentials

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@FUNewsTradingSystem.org` | `@@abc123@@` |

Additional accounts can be created by the Admin via `/Admin/Accounts`.

---

## Deployment

### Docker (recommended)

A multi-stage `Dockerfile` is included at the repository root:

```dockerfile
# Build stage — restore and publish all 3 projects
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish FUNewsTradingSystem/MVC -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FUNewsTradingSystem_MVC.dll"]
```

```bash
docker build -t funewstradingsystem .
docker run -p 8080:8080 funewstradingsystem
```

### Cloud platforms (Render, Railway, Azure App Service)

1. Set the runtime to **Docker** or **.NET 10**.
2. Provide the database connection string via the `ConnectionStrings__DefaultConnection` environment variable.
3. Provide API keys via environment variables:
   - `NewsApi__ApiKey`
   - `OpenAI__ApiKey`

> **Note:** On cloud platforms the `appsettings.json` values can be fully replaced by environment variables following the ASP.NET Core configuration binding convention (`Section__Key`).

---

## Known Limitations

- **NewsAPI Free Tier Restrictions** — The free NewsAPI tier limits queries to articles published within the last 30 days and may block requests from certain cloud providers (e.g., some cloud hosting IPs). If no news is found for a ticker, the pipeline throws `PipelineException("NO_NEWS")` and returns an error message to the UI.

- **LLM Output Variability** — AI trading decisions are generated by GPT-4o based on fetched headlines. The output quality depends on the volume and relevance of available news. The pipeline validates that the response contains a valid `decision` field (`BUY`, `SELL`, or `HOLD`); invalid responses throw `PipelineException("INVALID_DECISION")`.

- **Financial Disclaimer** — All generated analysis is for **demonstration and educational purposes only**. It must not be interpreted as financial advice or used for actual trading.

- **Self-deletion Blocked** — Admin accounts cannot delete their own account via `/Admin/Accounts/Delete/{id}`. This guard is enforced server-side in `AccountService.DeleteAccountAsync()`.

- **No Email Service** — The system does not include an email service for password reset or account notifications.
