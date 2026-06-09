# FUNewsTradingSystem — Architecture Notes

> This document describes the high-level architecture, DI registrations, auth flow, route map, and the TradingAgentService call chain.
> **Reviewed and signed off by: P1 (Tech Lead)**

---

## 1. Project Structure

```
FUNewsTradingSystem/
│
├── FUNewsTradingSystem.slnx                    # Solution file
│
├── FUNewsTradingSystem/
│   ├── MVC/                                   # Layer 3: Presentation
│   │   ├── Controllers/                       # Razor Controllers
│   │   ├── Views/                             # Razor Views
│   │   ├── ViewModels/                        # DTOs for Controller ↔ View
│   │   ├── Filters/                           # Custom authorization filters
│   │   ├── wwwroot/                           # Static files (CSS, JS, libs)
│   │   ├── Program.cs                         # DI, Auth, Middleware config
│   │   └── appsettings.json                   # Secrets (NOT committed)
│   │
│   ├── BusinessLayer/                         # Layer 2: Business Logic
│   │   ├── Services/
│   │   │   ├── Interfaces/                    # Service contracts
│   │   │   └── Implements/                    # Service implementations
│   │   └── Repositories/
│   │       ├── Interfaces/                    # Repository contracts
│   │       └── Implements/                    # Repository implementations
│   │
│   └── DataAccessLayer/                       # Layer 1: Data Access
│       ├── Models/                            # EF Core entity classes
│       ├── Migrations/                        # EF Core migrations
│       └── Context/                          # DbContext
│
└── Document/                                  # Documentation
```

---

## 2. 3-Layer Architecture Rules

```
┌─────────────────────────────────────────────────────────────┐
│  PRESENTATION (MVC Controllers + Views)                     │
│  - Receives HTTP requests                                    │
│  - Calls Service interfaces only                             │
│  - Never directly accesses DbContext or Repository           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  BUSINESS LOGIC (Services)                                   │
│  - Contains business rules and orchestration                 │
│  - Calls Repository interfaces only                          │
│  - Handles external API calls (NewsAPI, OpenAI)             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  DATA ACCESS (Repositories + DbContext)                      │
│  - Only layer that interacts with EF Core DbContext          │
│  - No business logic                                         │
│  - Returns entities to Services                              │
└─────────────────────────────────────────────────────────────┘
```

### Project References
- `MVC` → references `BusinessLayer`
- `BusinessLayer` → references `DataAccessLayer`

---

## 3. Dependency Injection Registrations (Program.cs)

```csharp
// Data Access
services.AddDbContext<FUNewsManagementContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories (Scoped)
services.AddScoped<ISystemAccountRepository, SystemAccountRepository>();
services.AddScoped<ICategoryRepository, CategoryRepository>();
services.AddScoped<ITagRepository, TagRepository>();
services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
services.AddScoped<INewsTagRepository, NewsTagRepository>();

// Services (Scoped, except TradingAgentService)
services.AddScoped<ISystemAccountService, SystemAccountService>();
services.AddScoped<ICategoryService, CategoryService>();
services.AddScoped<ITagService, TagService>();
services.AddScoped<INewsArticleService, NewsArticleService>();

// TradingAgentService is Singleton — reuses HttpClient
services.AddSingleton<HttpClient>(sp =>
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    return client;
});
services.AddSingleton<ITradingAgentService, TradingAgentService>();

// Authentication
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Authorization Policies
services.AddAuthorization(options => {
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireClaim("AccountRole", "1"));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("AccountRole", "3"));
    options.AddPolicy("StaffOrLecturer", policy =>
        policy.RequireClaim("AccountRole", "1", "2"));
});
```

---

## 4. Authentication & Authorization Flow

```
User submits Login form
        │
        ▼
AccountController.Login (POST)
        │
        ├── Query SystemAccount by email
        ├── Verify password hash via IPasswordHasher
        │
        ├── FAIL ──► Return inline error "Invalid email or password."
        │
        └── SUCCESS ──► Create ClaimsPrincipal
                         - ClaimTypes.NameIdentifier = AccountID
                         - ClaimTypes.Name = AccountEmail
                         - ClaimTypes.Role = AccountRole
                              │
                              ▼
                        HttpContext.SignInAsync()
                              │
                              ▼
                        Redirect by role:
                         - Role=1 → /Staff/Index (Dashboard)
                         - Role=2 → /Report/Index
                         - Role=3 → /Admin/Index
```

### Role-Based Access Control
| Role | Value | Access |
|------|-------|--------|
| Staff | 1 | Run Analysis, Categories, Tags, My Reports, Profile |
| Lecturer | 2 | Report Viewer (read-only) |
| Admin | 3 | Account Management, Statistics |
| Guest | - | Public Report List (no login) |

### Custom Authorization Filter
`RoleAuthorizeAttribute` is a custom `IAuthorizationFilter` that:
1. Reads `AccountRole` claim from the authenticated user.
2. Compares against the required role(s).
3. Returns 403 Forbidden or redirects to Login on violation.

---

## 5. Route Map

| Route | Controller | Action | Policy | Description |
|-------|-----------|--------|--------|-------------|
| `/` | Account | Login | AllowAnonymous | Default landing page |
| `/Account/Login` | Account | Login | AllowAnonymous | Login page |
| `/Account/Logout` | Account | Logout | Authenticated | Logout (POST) |
| `/Staff/Index` | Staff | Index | StaffOnly | Staff Dashboard |
| `/Staff/Profile` | Staff | Profile | StaffOnly | Profile Management |
| `/Staff/Profile/UpdateName` | Staff | UpdateName | StaffOnly | Update name (POST) |
| `/Staff/Profile/ChangePassword` | Staff | ChangePassword | StaffOnly | Change password (POST) |
| `/Analysis/Index` | Analysis | Index | StaffOnly | Run Analysis UI |
| `/Analysis/Run` | Analysis | Run | StaffOnly | Execute pipeline (POST) |
| `/Category/Index` | Category | Index | StaffOnly | Category list |
| `/Category/Create` | Category | Create | StaffOnly | Create category (POST) |
| `/Category/Edit` | Category | Edit | StaffOnly | Edit category (POST) |
| `/Category/ToggleActive` | Category | ToggleActive | StaffOnly | Toggle active (POST) |
| `/Category/Delete` | Category | Delete | StaffOnly | Delete category (POST) |
| `/Tag/Index` | Tag | Index | StaffOnly | Tag list |
| `/Tag/Create` | Tag | Create | StaffOnly | Create tag (POST) |
| `/Tag/Edit` | Tag | Edit | StaffOnly | Edit tag (POST) |
| `/Tag/Delete` | Tag | Delete | StaffOnly | Delete tag (POST) |
| `/Report/Index` | Report | Index | AllowAnonymous | Public report list |
| `/Report/Details/{id}` | Report | Details | AllowAnonymous | Report detail |
| `/Report/History` | Report | History | StaffOnly | Staff's report history |
| `/Report/ToggleStatus/{id}` | Report | ToggleStatus | StaffOnly | Toggle visibility (POST) |
| `/Admin/Index` | Admin | Index | AdminOnly | Admin Dashboard |
| `/Admin/Accounts` | Admin | Accounts | AdminOnly | Account list |
| `/Admin/Accounts/Create` | Admin | Create | AdminOnly | Create account (POST) |
| `/Admin/Accounts/Edit` | Admin | Edit | AdminOnly | Edit account (POST) |
| `/Admin/Accounts/Delete/{id}` | Admin | Delete | AdminOnly | Delete account (POST) |
| `/Admin/StatisticalReport` | Admin | StatisticalReport | AdminOnly | Statistics filter |

---

## 6. TradingAgentService Call Chain

The `TradingAgentService` orchestrates the full AI Trading Pipeline. It is registered as a **Singleton** to reuse the `HttpClient` instance.

```
RunAnalysisAsync(tagId, categoryId, createdByAccountId)
│
├── 1. FetchNewsAsync(tickerName)
│       │
│       └── GET https://newsapi.org/v2/everything
│           ?q={tickerName}&sortBy=publishedAt&pageSize=10
│           │
│           ├── SUCCESS ──► Parse response → List<NewsApiArticle>
│           │                    └─► Extract top 10 as numbered list
│           │
│           └── 0 results ──► throw PipelineException("NO_NEWS")
│
├── 2. RunSentimentAgentAsync(tickerName, headlines)
│       │
│       └── POST https://api.openai.com/v1/chat/completions
│           Body: { model: "gpt-4o", messages: [...], temperature: 0.3 }
│           Prompt: Sentiment Agent prompt (plain text output)
│               │
│               ├── SUCCESS ──► Return sentiment paragraph
│               │
│               └── FAIL ──► throw PipelineException("LLM_TIMEOUT" or "LLM_ERROR")
│
├── 3. RunFundamentalAgentAsync(tickerName, headlines, sentimentOutput)
│       │
│       └── POST https://api.openai.com/v1/chat/completions
│           Body: { model: "gpt-4o", messages: [...], temperature: 0.3 }
│           Prompt: Fundamental Agent prompt (plain text output)
│               │
│               ├── SUCCESS ──► Return fundamental analysis paragraph
│               │
│               └── FAIL ──► throw PipelineException("LLM_TIMEOUT" or "LLM_ERROR")
│
├── 4. RunPortfolioManagerAsync(tickerName, sentimentOutput, fundamentalOutput)
│       │
│       └── POST https://api.openai.com/v1/chat/completions
│           Body: { model: "gpt-4o", messages: [...], temperature: 0.3 }
│           Prompt: Portfolio Manager prompt (JSON output required)
│               │
│               ├── SUCCESS ──► Get raw response string
│               │                  │
│               │                  └──► PreprocessJsonResponse(raw)
│               │                        - Strip ```json and ``` if present
│               │                        - Trim whitespace
│               │                        │
│               │                        ▼
│               │                   Deserialize to PortfolioManagerResponse
│               │                        │
│               │                        ▼
│               │                   ValidatePortfolioResponse(r)
│               │                        - decision = ToUpperInvariant()
│               │                        - Assert decision ∈ {"BUY","SELL","HOLD"}
│               │                        - Assert all 5 fields non-empty
│               │                        │
│               │                        └── Valid ──► Return PortfolioManagerResponse
│               │
│               └── FAIL ──► throw PipelineException("LLM_TIMEOUT" or "LLM_ERROR")
│
└── 5. SaveReportAsync(portfolioResponse, categoryId, tagId, accountId)
        │
        └── BEGIN TRANSACTION
            │
            ├── INSERT NewsArticle
            │   - NewsTitle = $"[{decision}] {tagName} Automated Analysis"
            │   - CreatedByID = accountId
            │   - CreatedDate = DateTime.UtcNow
            │   - NewsStatus = 1
            │
            ├── INSERT NewsTag
            │   - NewsArticleID = newArticle.NewsArticleID
            │   - TagID = tagId
            │
            └── COMMIT
                │
                └── FAIL ──► ROLLBACK + throw PipelineException("DB_ERROR")

Return TradingAgentResult { Success = true, NewsArticleID = id }
```

### Error Handling Strategy
- All external API calls wrapped in `try/catch`.
- `PipelineException` is thrown with error codes:
  - `NO_NEWS` — NewsAPI returned 0 results
  - `LLM_TIMEOUT` — OpenAI request timed out
  - `LLM_ERROR` — OpenAI returned an error
  - `JSON_PARSE_ERROR` — Failed to parse LLM response
  - `INVALID_DECISION` — LLM returned invalid decision
  - `DB_ERROR` — Database operation failed
- All errors are logged via `ILogger`.
- User-friendly message returned to controller → displayed in UI.

---

## 7. Singleton Pattern

**Why Singleton for TradingAgentService?**
- `HttpClient` is expensive to create (socket connection pool).
- Singleton ensures one shared `HttpClient` instance across all requests.
- `HttpClient` is thread-safe for concurrent requests.
- `DbContext` remains Scoped (injected per-request) — not affected by Singleton service.

---

## 8. Session Configuration

- **Cookie-based authentication** (not JWT).
- **Sliding expiration:** 60 minutes. Timer resets on each authenticated request.
- **Cookie settings:** `HttpOnly=true`, `IsEssential=true`, `Secure` (in production).
- **No "Remember Me":** Sessions are browser-session only.

---

## 9. Database Schema Summary

| Entity | Key Fields |
|--------|-----------|
| SystemAccount | AccountID (PK), AccountName, AccountEmail (UNIQUE), AccountRole, AccountPassword |
| Category | CategoryID (PK), CategoryName, ParentCategoryID (FK self), IsActive |
| Tag | TagID (PK), TagName (UNIQUE, uppercase) |
| NewsArticle | NewsArticleID (PK), NewsTitle, CategoryID (FK), CreatedByID (FK, SET NULL), NewsStatus, CreatedDate |
| NewsTag | NewsArticleID (PK, FK), TagID (PK, FK) |

### Key Constraints
- `NewsTag` uses composite PK (NewsArticleID + TagID).
- `Category.ParentCategoryID` is self-referencing FK.
- `SystemAccount.AccountEmail` has unique index.
- `Tag.TagName` has unique index.
- `NewsArticle.CreatedByID` has `ON DELETE SET NULL`.
- `NewsArticle.UpdatedByID` has `ON DELETE NO ACTION`.

---

## 10. API Keys Configuration

All secrets are in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FUNewsManagement;Trusted_Connection=True;MultipleActiveResultSets=true"
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
    "ApiKey": "YOUR_OPENAI_KEY_HERE",
    "BaseUrl": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4o"
  }
}
```

---

**Document Status:** Final
**Reviewed By:** P1 (Tech Lead)
**Last Updated:** 2026
