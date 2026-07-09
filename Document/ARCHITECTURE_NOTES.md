# FUNewsTradingSystem — Architecture Notes

> **Reviewer: P1 (Tech Lead)** — please read and sign off before submission.
> Date reviewed: \_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_ &nbsp;&nbsp; Signed off by: \_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_

---

## Table of Contents

1. [Dependency Injection Registrations](#1--dependency-injection-registrations)
2. [Authentication & Authorisation Flow](#2--authentication--authorisation-flow)
3. [Route Map](#3--route-map)
4. [TradingAgentService Call Chain](#4--tradingagentservice-call-chain)

---

## 1 — Dependency Injection Registrations

All registrations live in `Program.cs`.

### Database

| Service | Lifetime | Notes |
|---------|----------|-------|
| `FUNewsManagementContext` | Scoped | EF Core `DbContext`; resolved by controllers and repositories |

### Repositories (Scoped)

| Interface | Implementation |
|-----------|---------------|
| `ISystemAccountRepository` | `SystemAccountRepository` |
| `ICategoryRepository` | `CategoryRepository` |
| `ITagRepository` | `TagRepository` |
| `INewsArticleRepository` | `NewsArticleRepository` |
| `INewsTagRepository` | `NewsTagRepository` *(placeholder — unused)* |

### Services (Scoped)

| Interface | Implementation |
|-----------|---------------|
| `ISystemAccountService` | `SystemAccountService` |
| `ICategoryService` | `CategoryService` |
| `ITagService` | `TagService` |
| `INewsArticleService` | `NewsArticleService` |

### TradingAgentService (Singleton)

| Interface | Implementation |
|-----------|---------------|
| `ITradingAgentService` | `TradingAgentService` |
| `HttpClient` | Singleton `new HttpClient { Timeout = 10 s }` |

> **Why Singleton for TradingAgentService?**
> The service is stateless (only reads `IConfiguration` and `HttpClient`). A single instance avoids
> creating a new object graph per request. The `HttpClient` is wrapped in a `Singleton` so it is
> shared and its connection pool is reused across pipeline invocations.

### Authentication Infrastructure (Scoped)

| Service | Lifetime |
|---------|----------|
| `IPasswordHasher<SystemAccount>` | Scoped — `PasswordHasher<SystemAccount>` |

### Authorization Policies

| Policy name | Requirement |
|-------------|-------------|
| `StaffOnly` | Claim `ClaimTypes.Role == "1"` |
| `AdminOnly` | Claim `ClaimTypes.Role == "3"` |
| `StaffOrLecturer` | Claim `ClaimTypes.Role` is `"1"` or `"2"` |

---

## 2 — Authentication & Authorisation Flow

### 2.1 — Role Number Mapping

| Role integer (DB) | Role name | Description |
|-------------------|-----------|-------------|
| 1 | Staff | Can manage categories, tags, run analyses, view own reports |
| 2 | Lecturer | Read-only access to published reports |
| 3 | Admin | User administration, statistics |

### 2.2 — Login Sequence (`AccountController.Login` POST)

```
1.  Validate model
2.  Check if email matches appsettings.json AdminAccount
    → If yes: build in-memory SystemAccount with Role=3 (no DB lookup)
    → If no:  look up by email via ISystemAccountService.GetByEmailAsync(...)
3.  If account not found → "Invalid email or password."
4.  Verify password
    a. Try ASP.NET Identity hash via IPasswordHasher<SystemAccount>.VerifyHashedPassword(...)
    b. If FormatException thrown (legacy plaintext), fall back to direct string equality
    c. If neither succeeds → "Invalid email or password."
5.  Build ClaimsPrincipal with claims:
       "AccountID"   → account.AccountID.ToString()
       ClaimTypes.Email  → account.AccountEmail
       ClaimTypes.Role   → account.AccountRole.ToString()
       ClaimTypes.Name   → account.AccountName
6.  SignInAsync with CookieAuthenticationDefaults (session cookie, 60 min sliding)
7.  Redirect by role (or returnUrl if safe):
       Role 1 → /Staff/Dashboard
       Role 2 → /News/Index
       Role 3 → /Admin/Dashboard
```

### 2.3 — Page-Level Authorisation

Two mechanisms coexist:

**A. `[Authorize(Policy = "…")]`** — ASP.NET Core policy authorisation on the controller class.
Applied to: `StaffController` (`StaffOnly`), `AdminAccountController` / `AdminStatisticsController` (`AdminOnly`),
`NewsController` (`StaffOrLecturer`).

**B. `[RoleAuthorize]`** — Custom `IAuthorizationFilter` defined in `MVC/Filters/RoleAuthorizeAttribute.cs`.

Behaviour summary:

| Scenario | Result |
|----------|--------|
| Unauthenticated + `RedirectToLogin = true` | Redirect to `/Account/Login?returnUrl=…` |
| Unauthenticated + `RedirectToLogin = false` (default) | HTTP 403 Forbidden (raw HTML page) |
| Authenticated, wrong role | HTTP 403 Forbidden (raw HTML page) |

`RoleAuthorize` is **not currently applied to any controller** — it is wired up but reserved for future fine-grained use.

---

## 3 — Route Map

Default convention: `{controller=Account}/{action=Login}/{id?}`.

| Method | Route | Auth required | Notes |
|--------|-------|---------------|-------|
| `GET` | `/Account/Login` | Anonymous | |
| `POST` | `/Account/Login` | Anonymous | |
| `POST` | `/Account/Logout` | Authenticated | |
| `GET` | `/Account/AccessDenied` | Anonymous | |
| `GET` | `/` | — | Falls through to `/Account/Login` |
| `GET` | `/Home/Error/{code?}` | — | Status-code re-execute handler |
| `GET` | `/Home/Privacy` | — | |
| `GET` | `/News/Index` | `StaffOrLecturer` | Anonymous GET allowed via `[AllowAnonymous]` |
| `GET` | `/News/Detail/{id}` | `StaffOrLecturer` | Anonymous GET allowed via `[AllowAnonymous]` |
| `GET` | `/Staff/Dashboard` | `StaffOnly` | |
| `GET` | `/Staff/Profile` | `StaffOnly` | |
| `POST` | `/Staff/Profile/UpdateName` | `StaffOnly` | |
| `POST` | `/Staff/Profile/ChangePassword` | `StaffOnly` | |
| `GET` | `/Staff/RunAnalysis` | `StaffOnly` | |
| `POST` | `/Staff/RunAnalysis` | `StaffOnly` | JSON via AJAX |
| `GET` | `/Staff/Categories` | `StaffOnly` | |
| `GET` | `/Staff/Categories/CreatePartial` | `StaffOnly` | Partial view |
| `POST` | `/Staff/Categories/Create` | `StaffOnly` | JSON via AJAX |
| `GET` | `/Staff/Categories/EditPartial/{id}` | `StaffOnly` | Partial view |
| `POST` | `/Staff/Categories/Edit` | `StaffOnly` | JSON via AJAX |
| `POST` | `/Staff/Categories/ToggleActive/{id}` | `StaffOnly` | JSON via AJAX |
| `POST` | `/Staff/Categories/Delete/{id}` | `StaffOnly` | JSON via AJAX |
| `GET` | `/Staff/Tags` | `StaffOnly` | |
| `GET` | `/Staff/Tags/CreatePartial` | `StaffOnly` | Partial view |
| `POST` | `/Staff/Tags/Create` | `StaffOnly` | JSON via AJAX |
| `GET` | `/Staff/Tags/EditPartial/{id}` | `StaffOnly` | Partial view |
| `POST` | `/Staff/Tags/Edit` | `StaffOnly` | JSON via AJAX |
| `POST` | `/Staff/Tags/Delete/{id}` | `StaffOnly` | JSON via AJAX |
| `GET` | `/Admin/Accounts` | `AdminOnly` | |
| `GET` | `/Admin/Dashboard` | `AdminOnly` | |
| `GET` | `/Admin/Accounts/CreatePartial` | `AdminOnly` | Partial view |
| `POST` | `/Admin/Accounts/Create` | `AdminOnly` | JSON via AJAX |
| `GET` | `/Admin/Accounts/EditPartial/{id}` | `AdminOnly` | Partial view |
| `POST` | `/Admin/Accounts/Edit` | `AdminOnly` | JSON via AJAX |
| `POST` | `/Admin/Accounts/Delete/{id}` | `AdminOnly` | JSON via AJAX |
| `GET` | `/Admin/Statistics` | `AdminOnly` | |
| `POST` | `/Admin/Statistics` | `AdminOnly` | Form POST with date filter |

---

## 4 — TradingAgentService Call Chain

```
RunAnalysisAsync(tagId, categoryId, createdByAccountId)
│
├── Step 1 — Resolve Tag (scoped DbContext)
│   └─ context.Tags.FindAsync(tagId)
│       → throws PipelineException("DB_ERROR") if tag not found
│
├── Step 2 — Fetch News Headlines
│   └─ FetchNewsAsync(ticker)
│       → GET https://newsapi.org/v2/everything?q={ticker}&sortBy=publishedAt&pageSize=10
│       → deserialises NewsApiResponse
│       → throws PipelineException("NEWS_TIMEOUT")     on TaskCanceledException
│       → throws PipelineException("NEWS_API_ERROR")    on HTTP non-2xx or parse failure
│       → throws PipelineException("NO_NEWS")          if zero articles returned
│       → returns numbered "1. Title – Description\n2. …" string
│
├── Step 3 — Sentiment Agent (OpenAI)
│   └─ RunSentimentAgentAsync(ticker, headlines)
│       → renders SENTIMENT_AGENT_PROMPT_TEMPLATE with {ticker} and {headlines_numbered_list}
│       → CallOpenAiAsync(prompt)
│           → POST /v1/chat/completions  { model, messages:[{role:"user",content}], temperature:0.2, max_tokens:1000 }
│           → throws PipelineException("LLM_TIMEOUT")   on TaskCanceledException
│           → throws PipelineException("LLM_ERROR")     on HTTP non-2xx or parse failure
│           → throws PipelineException("LLM_ERROR")     if response content is empty
│       → returns raw text (plain reasoning paragraph)
│
├── Step 4 — Fundamental Agent (OpenAI)
│   └─ RunFundamentalAgentAsync(ticker, headlines, sentimentOutput)
│       → renders FUNDAMENTAL_AGENT_PROMPT_TEMPLATE
│       → CallOpenAiAsync(prompt)
│       → returns raw text (plain reasoning paragraph)
│
├── Step 5 — Portfolio Manager Agent (OpenAI)
│   └─ RunPortfolioManagerAsync(ticker, sentimentOutput, fundamentalOutput)
│       → renders PORTFOLIO_MANAGER_PROMPT_TEMPLATE
│       → CallOpenAiAsync(prompt)
│       → PreprocessJsonResponse(raw)   ← strips ```json…``` fences
│       → JsonSerializer.Deserialize<PortfolioManagerResponse>
│           → throws PipelineException("JSON_PARSE_ERROR") on malformed JSON
│       → ValidatePortfolioResponse(result)
│           → throws PipelineException("INVALID_DECISION") if any field is null/empty
│           → normalises Decision to uppercase
│           → throws PipelineException("INVALID_DECISION") if Decision not BUY/SELL/HOLD
│       → returns PortfolioManagerResponse
│
├── Step 6 — Persist Report (scoped DbContext + transaction)
│   └─ SaveReportAsync(portfolioResponse, tagId, categoryId, createdByAccountId)
│       → BEGIN TRANSACTION
│       → context.Tags.FindAsync(tagId)   ← re-verify
│       → context.NewsArticles.Add(article)    NewsTitle = $"[{decision}] {tagName} Automated Analysis"
│       → SaveChangesAsync()
│       → context.NewsTags.Add({ NewsArticleID, TagID })
│       → SaveChangesAsync()
│       → COMMIT TRANSACTION
│       → rollback on any exception
│       → throws PipelineException("DB_ERROR")
│
└── Return TradingAgentResult { Success=true, NewsArticleID, ErrorMessage=null }
```

### Error Handling Strategy

`RunAnalysisAsync` catches all `PipelineException` and unhandled `Exception` instances and
converts them to a `TradingAgentResult` with `Success = false` and an `ErrorMessage` string.
**The pipeline never throws to the MVC layer** — callers receive a structured result they can
display directly to the user.

### PipelineException Error Codes

| Code | Meaning |
|------|---------|
| `DB_ERROR` | Tag not found or DB write failure |
| `NEWS_TIMEOUT` | NewsAPI request cancelled (network or 10 s timeout) |
| `NEWS_API_ERROR` | NewsAPI returned non-2xx or JSON parse failure |
| `NO_NEWS` | NewsAPI returned zero articles for this ticker |
| `LLM_TIMEOUT` | OpenAI request cancelled (network or 10 s timeout) |
| `LLM_ERROR` | OpenAI returned non-2xx or JSON parse failure |
| `JSON_PARSE_ERROR` | Portfolio Manager output was not valid JSON |
| `INVALID_DECISION` | LLM output was valid JSON but missing/invalid `decision` field |

---

## Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| P1 — Tech Lead | | | |
| P2 — Backend Dev | | | |
| P3 — Full-stack Dev | | | |
| P4 — UI Dev | | | |
