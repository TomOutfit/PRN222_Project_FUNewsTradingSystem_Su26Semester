# FUNewsTradingSystem — Work Breakdown

---

## Overview

| Person | Role | Tasks |
|--------|------|-------|
| P1 — Tech Lead | Solution skeleton, Auth/AuthZ, AI Pipeline, Real-time Hubs, shared backend infrastructure | 50 |
| P2 — Backend Dev | DB migrations, all Repositories + Services, Account Management, Admin Statistics | 44 |
| P3 — Full-stack Dev | Category/Tag CRUD (full stack), Report Viewer, Staff History | 38 |
| P4 — UI Dev | All shared JS/CSS, Run Analysis UI, Profile Management, Staff Dashboard, Real-time UI Sync, Documentation | 52 |

> **Testing policy:** each person writes and runs smoke tests for their own features before merging.
> A shared smoke test checklist lives in `TESTING.md` — P4 maintains the file, everyone fills it in.

| **Total** | | **184 tasks** |

---

## PERSON 1 — Tech Lead | 50 tasks

> Owns the entire skeleton, auth system, and AI pipeline. If P1's work breaks, nothing else runs.

### 🔧 Solution & Project Setup (10 tasks)
- [X] Create Visual Studio solution: `FUNewsTradingSystem.sln`
- [X] Create ASP.NET Core MVC project: `FUNewsTradingSystem_MVC` targeting .NET 10
- [X] Install NuGet packages: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Authentication.Cookies`, `Newtonsoft.Json`
- [X] Create project folder structure: `/Models`, `/ViewModels`, `/Repositories`, `/Services`, `/Controllers`, `/Views`, `/wwwroot/css`, `/wwwroot/js`, `/wwwroot/lib`
- [X] Add Bootstrap 5 and jQuery 3.x via libman.json or CDN in `_Layout.cshtml`
- [X] Add `jquery-validate` and `jquery-validate-unobtrusive` to the project
- [X] Configure `appsettings.json`:
  ```json
  {
    "ConnectionStrings": { "DefaultConnection": "..." },
    "AdminAccount": { "Email": "admin@FUNewsTradingSystem.org", "Password": "@@abc123@@", "Name": "System Admin" },
    "NewsApi": { "ApiKey": "...", "BaseUrl": "https://newsapi.org/v2/everything" },
    "OpenAI": { "ApiKey": "...", "BaseUrl": "https://api.openai.com/v1/chat/completions", "Model": "gpt-4o" }
  }
  ```
- [X] Configure `appsettings.Development.json` to override secrets for local development
- [X] Add `appsettings.json` to `.gitignore`
- [X] Configure `Program.cs`: DbContext, Cookie Auth middleware (`HttpOnly=true`, `SlidingExpiration=true`, `ExpireTimeSpan=60min`), Authorization middleware, default route (`controller=Account, action=Login`), static files middleware

### 💻 Entity Classes (6 tasks)
- [X] Create `SystemAccount.cs`: AccountID (PK), AccountName, AccountEmail, AccountRole, AccountPassword
- [X] Create `Category.cs`: CategoryID (PK), CategoryName, CategoryDescription, ParentCategoryID (FK self-ref, nullable), IsActive; navigation properties `ParentCategory` and `ChildCategories`
- [X] Create `Tag.cs`: TagID (PK), TagName, Note
- [X] Create `NewsArticle.cs`: all 11 columns; FK navigation to `Category`, `CreatedByAccount`, `UpdatedByAccount`, `NewsTagList`
- [X] Create `NewsTag.cs`: composite PK (NewsArticleID + TagID), navigation properties to `NewsArticle` and `Tag`
- [X] Create `FUNewsManagementContext.cs`: `DbSet<>` for all 5 entities; `OnModelCreating` — composite PK on `NewsTag`, self-ref FK on `Category`, `ON DELETE SET NULL` on `NewsArticle.CreatedByID`, `ON DELETE NO ACTION` on `NewsArticle.UpdatedByID`, unique index on `AccountEmail` and `TagName`; `HasData` seed — read Admin credentials from `IConfiguration`, hash with `IPasswordHasher<SystemAccount>`, insert `AccountRole=3`

### 🔐 Authentication — FR-1 (6 tasks)
- [X] Create `AccountController.cs` with `[AllowAnonymous]` on Login actions
- [X] `GET /Account/Login`: return Login view; if already authenticated, redirect by role
- [X] `POST /Account/Login`: validate ModelState → query `SystemAccount` by email → `IPasswordHasher.VerifyHashedPassword()` → on fail: ModelError "Invalid email or password." (no email-existence hint) → on success: create `ClaimsPrincipal` (claims: AccountID, AccountEmail, AccountRole) → `HttpContext.SignInAsync()` with `IsPersistent=false` → redirect Role=1→`/Staff/Dashboard`, Role=2→`/News/Index`, Role=3→`/Admin/Dashboard`
- [X] `POST /Account/Logout` with `[ValidateAntiForgeryToken]`: `HttpContext.SignOutAsync()` → redirect to Login
- [X] Create `LoginViewModel.cs`: Email (Required, EmailAddress), Password (Required, MinLength 8)
- [X] Create `Views/Account/Login.cshtml`: form with Email/Password inputs, `asp-for` tag helpers, `<span asp-validation-for>` on each field, `@Html.AntiForgeryToken()`, validation scripts partial, Bootstrap styling

### 🛡️ Authorization — FR-2 (7 tasks)
- [X] Create `/Filters/RoleAuthorizeAttribute.cs`: custom `IAuthorizationFilter` reading role from claims; return 403 or redirect to Login on violation
- [X] Configure policies in `Program.cs`: `"StaffOnly"` (Role=1), `"AdminOnly"` (Role=3), `"StaffOrLecturer"` (Role=1 or 2)
- [X] Apply `[Authorize(Policy = "StaffOnly")]` to all Staff controller actions (Category, Tag, RunAnalysis, History, Profile, Dashboard)
- [X] Apply `[Authorize(Policy = "AdminOnly")]` to all Admin controller actions (Accounts, Statistics)
- [X] Ensure `NewsController.Index` and `NewsController.Detail` have `[AllowAnonymous]`
- [X] Create `ClaimsPrincipalExtensions.GetAccountId()`: parses AccountID claim as `int` for use across controllers
- [X] Add role-based nav rendering to `_Layout.cshtml` using `User.IsInRole()`

### 🤖 AI Trading Pipeline — FR-3 (19 tasks)
- [X] Create `/Models/DTOs/NewsApiArticle.cs`: `title`, `description`, `publishedAt`, `source`
- [X] Create `/Models/DTOs/NewsApiResponse.cs`: `status`, `totalResults`, `articles: List<NewsApiArticle>`
- [X] Create `/Models/DTOs/PortfolioManagerResponse.cs`: `decision`, `title`, `headline`, `content`, `source`
- [X] Create `/Models/DTOs/OpenAiRequest.cs`: `model`, `messages`, `temperature`, `max_tokens`
- [X] Create `/Models/DTOs/OpenAiResponse.cs`: maps `choices[0].message.content`
- [X] Create `ITradingAgentService.cs`: single method `Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId)`
- [X] Create `TradingAgentResult.cs`: `bool Success`, `int? NewsArticleID`, `string ErrorMessage`
- [x] Define prompt constants as `static readonly string` in `TradingAgentService`: `SENTIMENT_AGENT_PROMPT_TEMPLATE`, `FUNDAMENTAL_AGENT_PROMPT_TEMPLATE`, `PORTFOLIO_MANAGER_PROMPT_TEMPLATE`
- [x] Implement `FetchNewsAsync(string tickerName)`: GET NewsAPI.org `q={ticker}&sortBy=publishedAt&pageSize=10`; extract top 10 as numbered list `"1. {title} – {description}"`; throw `PipelineException("NO_NEWS")` if 0 results
- [x] Implement `RunSentimentAgentAsync(string ticker, string headlines)`: POST to OpenAI with Sentiment prompt; extract `choices[0].message.content`; throw `PipelineException("LLM_TIMEOUT")` or `PipelineException("LLM_ERROR")` on failure
- [x] Implement `RunFundamentalAgentAsync(string ticker, string headlines, string sentimentOutput)`: same pattern as Sentiment step
- [x] Implement `RunPortfolioManagerAsync(...)`: POST to OpenAI; receive raw string; call `PreprocessJsonResponse()`; deserialize to `PortfolioManagerResponse`; throw `PipelineException("JSON_PARSE_ERROR")` on failure
- [x] Implement `PreprocessJsonResponse(string raw)`: strip leading ` ```json ` and trailing ` ``` ` if present; trim whitespace
- [x] Implement `ValidatePortfolioResponse(PortfolioManagerResponse r)`: `r.decision = r.decision.ToUpperInvariant()`; assert `decision ∈ {"BUY","SELL","HOLD"}`; assert all 5 fields non-empty; throw `PipelineException("INVALID_DECISION")` on failure
- [x] Implement `SaveReportAsync(...)`: open DB transaction → insert `NewsArticle` (`NewsTitle="[{decision}] {TagName} Automated Analysis"`, `CreatedByID`, `CreatedDate=DateTime.UtcNow`, `NewsStatus=1`) → insert `NewsTag` → commit; rollback + throw `PipelineException("DB_ERROR")` on failure
- [x] Implement `RunAnalysisAsync(...)`: orchestrate all steps; catch `PipelineException`; return `TradingAgentResult` with success/error state
- [x] Register in `Program.cs`: `AddSingleton<HttpClient>` with `Timeout=TimeSpan.FromSeconds(10)`; `AddSingleton<ITradingAgentService, TradingAgentService>()`
- [X] Create `RunAnalysisController.cs` with `[Authorize(Policy = "StaffOnly")]`: `GET /Staff/RunAnalysis` (populate Ticker + active Sector dropdowns, return view); `POST /Staff/RunAnalysis` async (call `RunAnalysisAsync()`, return JSON `{ success, newsArticleId, errorMessage }`)
- [X] Create `RunAnalysisViewModel.cs`: `SelectedTagId` (Required), `SelectedCategoryId` (Required), `AvailableTags: SelectList`, `AvailableCategories: SelectList`

### 📡 Real-time SignalR Hubs Configuration (2 tasks)
- [x] Configure SignalR services and map hubs (`/hubs/notifications`, `/hubs/presence`) in `Program.cs`
- [x] Implement backend notifications triggers inside Services (e.g., CategoryService, TagService) using `IHubContext` to broadcast CRUD actions to connected clients

### 🧪 P1 Self-test (smoke tests to run before handoff)
- [x] Verify Login success for all 3 roles with correct redirects
- [x] Verify Login failure shows inline error with no email-existence hint
- [x] Verify unauthorized route access redirects to Login
- [x] Verify pipeline runs end-to-end: trigger from UI → new `NewsArticle` row in DB with correct `NewsTitle` format and `CreatedByID`
- [x] Verify pipeline error scenarios return descriptive messages without crashing the app

---

## PERSON 2 — Backend Developer | 44 tasks

> Owns the full data layer. P3 cannot wire controllers until P2's service interfaces are defined — publish interface stubs by Day 2.

### 🗄️ EF Core Migrations & Database (5 tasks)
- [X] Run `dotnet ef migrations add InitialCreate` after P1 completes entity classes; review generated SQL (composite PK on `NewsTag`, `ON DELETE SET NULL` on `CreatedByID`, unique indexes)
- [X] Run `dotnet ef database update`; verify all tables and constraints in SSMS
- [X] Test seed end-to-end: drop DB → `dotnet ef database update` → log in as Admin → confirm access
- [X] Create `ServiceResult.cs`: `bool Success`, `string? ErrorMessage`, `int? EntityId` — shared return type for all service methods
- [X] Create seed migrations for demo Categories and Tags: Technology, Healthcare, Finance, Energy, Cryptocurrencies, Consumer Goods; AAPL, NVDA, MSFT, GOOGL, TSLA, BTC, ETH, AMZN

### 🗃️ Repository Layer (4 tasks)
- [X] Create `ISystemAccountRepository` + `SystemAccountRepository`: `GetByEmailAsync`, `GetAllAsync`, `GetByIdAsync`, `EmailExistsAsync(email, excludeId?)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- [X] Create `ICategoryRepository` + `CategoryRepository`: `GetAllAsync` (eager-load ParentCategory), `GetActiveAsync`, `GetTopLevelAsync`, `GetByIdAsync`, `IsReferencedByAnyArticleAsync`, `HasChildCategoriesReferencedByArticlesAsync`, `CreateAsync`, `UpdateAsync`, `ToggleActiveAsync`, `DeleteWithReparentChildrenAsync` (set children `ParentCategoryID=null` + delete parent in one transaction)
- [X] Create `ITagRepository` + `TagRepository`: `GetAllAsync`, `GetAllForDropdownAsync`, `GetByIdAsync`, `TagNameExistsAsync(name, excludeId?)`, `IsReferencedByAnyArticleAsync`, `CreateAsync` (normalize to uppercase), `UpdateAsync` (normalize), `DeleteAsync`
- [X] Create `INewsArticleRepository` + `NewsArticleRepository`: `GetActiveAsync` (eager-load Category + NewsTag.Tag), `GetByIdAsync` (all nav props), `GetByCreatorAsync(accountId)`, `GetByDateRangeAsync(startUtc, endUtc)` (eager-load Category + CreatedByAccount), `CreateWithTagAsync(article, tagId)` (transaction; return new NewsArticleID), `ToggleStatusAsync(newsArticleId, updatedByAccountId)` (flip NewsStatus, set UpdatedByID + ModifiedDate=UtcNow)

### ⚙️ Service Layer (5 tasks)
- [X] Create `IAccountService` + `AccountService`: `GetAllAccountsAsync()`, `GetAccountForEditAsync(id)`, `CreateAccountAsync(vm)` (hash password, check email uniqueness), `UpdateAccountAsync(vm)` (rehash only if new password provided), `DeleteAccountAsync(id, requestingAdminId)` (reject if self), `UpdateAccountNameAsync(id, name)`, `ChangePasswordAsync(id, currentPwd, newPwd)` (verify hash → rehash → save)
- [X] Create `ICategoryService` + `CategoryService`: `GetAllCategoriesAsync()`, `GetActiveCategorySelectListAsync()`, `GetTopLevelCategorySelectListAsync()`, `GetCategoryForEditAsync(id)`, `CreateCategoryAsync(vm)` (validate no self-ref), `UpdateCategoryAsync(vm)` (validate no self-ref), `ToggleActiveAsync(id)`, `DeleteCategoryAsync(id)` (check article + children references)
- [X] Create `ITagService` + `TagService`: `GetAllTagsAsync()`, `GetTagSelectListAsync()`, `GetTagForEditAsync(id)`, `CreateTagAsync(vm)` (uppercase + uniqueness), `UpdateTagAsync(vm)` (uppercase + uniqueness), `DeleteTagAsync(id)` (check NewsTag references)
- [X] Create `INewsArticleService` + `NewsArticleService`: `GetActiveReportsAsync()`, `GetReportDetailAsync(id)`, `GetReportsByCreatorAsync(accountId)`, `GetReportsByDateRangeAsync(startDate, endDate)` (UTC range, `CreatedByName` null→"Deleted User", sort descending), `ToggleStatusAsync(newsArticleId, updatedByAccountId)`
- [X] Create all DTOs: `AccountListItemDto` (ID, Name, Email, Role, RoleLabel), `CategoryListItemDto` (ID, Name, Description, ParentName, IsActive), `TagListItemDto` (ID, TagName, Note), `NewsArticleListItemDto` (ID, Title, Headline, CreatedDate, CategoryName, TagNames, NewsStatus, DecisionBadge), `NewsArticleDetailDto` (all list fields + NewsContent, NewsSource, CreatedByName), `NewsArticleStatDto` (Title, Headline, CreatedDate, CategoryName, CreatedByName)


### 👤 Account Management — FR-4 (backend + views) (17 tasks)
- [X] Create `AdminAccountController.cs` with `[Authorize(Policy = "AdminOnly")]`
- [X] `GET /Admin/Accounts`: call `GetAllAccountsAsync()`, return Index view
- [X] `GET /Admin/Accounts/CreatePartial`: return `_CreateAccountModal` partial with empty `CreateAccountViewModel`
- [X] `POST /Admin/Accounts/Create` (AJAX JSON): validate ModelState → `CreateAccountAsync()` → return `{ success }` or `{ success: false, errors }`
- [X] `GET /Admin/Accounts/EditPartial/{id}`: call `GetAccountForEditAsync(id)`, return `_EditAccountModal` partial
- [X] `POST /Admin/Accounts/Edit` (AJAX JSON): validate → `UpdateAccountAsync()` → return result JSON
- [X] `POST /Admin/Accounts/Delete/{id}` (AJAX JSON + `[ValidateAntiForgeryToken]`): `DeleteAccountAsync(id, currentAdminId)` → return result JSON
- [X] `GET /Admin/Dashboard`: landing page with links to Accounts and Statistics
- [X] Create `CreateAccountViewModel.cs`: AccountName (Required, 2–100), AccountEmail (Required, EmailAddress), AccountPassword (Required, MinLength 8), AccountRole (Required, Range 1–2)
- [X] Create `EditAccountViewModel.cs`: same fields + AccountId (Required); AccountPassword optional on Edit
- [X] Create `Views/Admin/Accounts/Index.cshtml`: table (ID | Name | Email | Role label | Edit | Delete); Delete hidden for Admin's own row; "Add Account" button; modal container divs
- [X] Create `Views/Admin/Accounts/_CreateAccountModal.cshtml`: form fields, validation spans, Save button
- [X] Create `Views/Admin/Accounts/_EditAccountModal.cshtml`: same + hidden AccountId field
- [X] Create `wwwroot/js/accounts.js`: `openCreateModal()` (AJAX GET partial → inject → show), `openEditModal(id)`, `submitCreateForm()` (AJAX POST → handle → refresh list), `submitEditForm()`, `deleteAccount(id, name)` (populate `_ConfirmDeleteModal` → confirm → AJAX POST → refresh), `refreshAccountTable()`

### 📊 Admin Statistical Report — FR-10 (7 tasks)
- [X] Create `AdminStatisticsController.cs` with `[Authorize(Policy = "AdminOnly")]`
- [X] `GET /Admin/Statistics`: return view with empty `StatisticsFilterViewModel` (no results on initial load)
- [X] `POST /Admin/Statistics`: validate dates → `GetReportsByDateRangeAsync()` → return view with results
- [X] Create `StatisticsFilterViewModel.cs`: StartDate (Required), EndDate (Required)
- [X] Create `StatisticsResultViewModel.cs`: Filter, Results (`List<NewsArticleStatDto>`), HasResults (bool)
- [X] Create `Views/Admin/Statistics/Index.cshtml`: date filter form (StartDate + EndDate date pickers, "Generate Report" button); client-side block if StartDate > EndDate ("Start date must be before or equal to end date."); results table (Title | Headline | Created Date UTC | Sector | Created By); "No reports found for the selected period." when empty
- [X] Server-side filter: `CreatedDate >= StartDate 00:00:00 UTC` AND `CreatedDate <= EndDate 23:59:59 UTC`; sort descending by CreatedDate

### 🧪 P2 Self-test (smoke tests to run before handoff)
- [X] Verify seed data (Categories + Tags) appears in DB and in pipeline dropdowns after fresh migration
- [X] Verify Account CRUD: create duplicate email → error; edit with blank password → existing password retained; delete own account → blocked
- [X] Verify Statistics filter: correct date range, descending sort, "Deleted User" for null CreatedByID
- [X] Verify all Repository methods return correct data by inspecting DB before and after each operation

---

## PERSON 3 — Full-stack Developer | 38 tasks

> Owns Category/Tag management (full stack) and the public-facing Report Viewer. Can scaffold views on Day 1 in parallel with P2.

### 🖼️ Layout & Shared Views (5 tasks)
- [x] Implement `Views/Shared/_Layout.cshtml`: Bootstrap 5 navbar; role-gated nav links via `@if (User.IsInRole("1"))` guards (Staff: Run Analysis, Categories, Tags, My Reports, Profile; Admin: Accounts, Statistics; Lecturer/Guest: Reports only); Logout POST form with AntiForgeryToken; `@RenderBody()`; `@RenderSection("Scripts", required: false)` at bottom
- [x] Create `Views/Shared/_ValidationScripts.cshtml` partial: `jquery-validate` + `jquery-validate-unobtrusive` script tags
- [x] Create `Views/Shared/_ConfirmDeleteModal.cshtml`: reusable Bootstrap modal; `data-entity-name` and `data-delete-url` attributes drive content; Cancel + Confirm buttons
- [x] Create `Views/Shared/Error.cshtml`: status code + friendly message display
- [x] Test nav visibility manually for all 4 roles after P1 completes auth

### 🗂️ Category Management — FR-5 (15 tasks)
- [x] Create `CategoryController.cs` with `[Authorize(Policy = "StaffOnly")]`
- [x] `GET /Staff/Categories`: `GetAllCategoriesAsync()` → return Index view
- [x] `GET /Staff/Categories/CreatePartial`: return `_CreateCategoryModal` partial with empty `CreateCategoryViewModel`
- [x] `POST /Staff/Categories/Create` (AJAX JSON): validate → `CreateCategoryAsync()` → return result JSON
- [x] `GET /Staff/Categories/EditPartial/{id}`: `GetCategoryForEditAsync(id)` → return `_EditCategoryModal` partial
- [x] `POST /Staff/Categories/Edit` (AJAX JSON): validate → `UpdateCategoryAsync()` → return result JSON
- [x] `POST /Staff/Categories/ToggleActive/{id}` (AJAX JSON): `ToggleActiveAsync(id)` → return `{ success, newIsActive }`
- [x] `POST /Staff/Categories/Delete/{id}` (AJAX JSON + AntiForgery): `DeleteCategoryAsync(id)` → return result JSON
- [x] Create `CreateCategoryViewModel.cs`: CategoryName (Required, Max 200), CategoryDescription (Max 500), ParentCategoryID (int?, optional), IsActive (bool, default true)
- [x] Create `EditCategoryViewModel.cs`: same + CategoryID (Required)
- [x] Create `Views/Staff/Categories/Index.cshtml`: table (ID | Name | Description | Parent | IsActive toggle | Edit | Delete); "Add Category" button; modal container divs
- [x] Create `Views/Staff/Categories/_CreateCategoryModal.cshtml`: CategoryName input, CategoryDescription textarea, ParentCategoryID dropdown ("None" default, top-level only), IsActive checkbox, validation spans
- [x] Create `Views/Staff/Categories/_EditCategoryModal.cshtml`: same + hidden CategoryID
- [x] Create `wwwroot/js/categories.js`: `openCreateModal()`, `openEditModal(id)`, `submitCreateForm()`, `submitEditForm()`, `toggleActive(id, checkbox)` (AJAX POST → on failure revert checkbox + show error toast), `deleteCategory(id, name)` (shared `confirmDelete()` → confirm → AJAX → refresh), `refreshCategoryTable()`

### 🏷️ Tag Management — FR-6 (10 tasks)
- [x] Create `TagController.cs` with `[Authorize(Policy = "StaffOnly")]`
- [x] `GET /Staff/Tags`: `GetAllTagsAsync()` → return Index view
- [x] `GET /Staff/Tags/CreatePartial`, `POST /Staff/Tags/Create` (AJAX JSON)
- [x] `GET /Staff/Tags/EditPartial/{id}`, `POST /Staff/Tags/Edit` (AJAX JSON)
- [x] `POST /Staff/Tags/Delete/{id}` (AJAX JSON + AntiForgery)
- [x] Create `CreateTagViewModel.cs`: TagName (Required, MaxLength 50), Note (MaxLength 500); `EditTagViewModel.cs`: same + TagID (Required)
- [x] Create `Views/Staff/Tags/Index.cshtml`: table (ID | TagName | Note | Edit | Delete); "Add Ticker" button; modal container divs
- [x] Create `Views/Staff/Tags/_CreateTagModal.cshtml` and `_EditTagModal.cshtml` partials with validation spans
- [x] Create `wwwroot/js/tags.js`: `openCreateModal()`, `openEditModal(id)`, `submitCreateForm()`, `submitEditForm()`, `deleteTag(id, name)`, `refreshTagTable()`

### 📰 Trading Report Viewer — FR-7 (5 tasks)
- [x] Create `NewsController.cs` with `[AllowAnonymous]`; `GET /News/Index` → `GetActiveReportsAsync()` → view; `GET /News/Detail/{id}` → `GetReportDetailAsync(id)` → if null return 404 → view
- [x] Create `Views/News/Index.cshtml`: title "Trading Analysis Reports"; per report: decision badge (`badge bg-success`=BUY, `badge bg-danger`=SELL, `badge bg-secondary`=HOLD), NewsTitle, Headline, CreatedDate (`YYYY-MM-DD HH:mm UTC`), CategoryName, Tag pills; "Read Analysis" link; "No reports available." when empty; no Create/Edit/Delete controls
- [x] Create `Views/News/Detail.cshtml`: decision badge at top; NewsTitle as heading; metadata row (Created on | Sector | Tickers); NewsContent as formatted paragraphs; NewsSource as small caption; Back → `/News/Index`; no management buttons
- [x] Verify: inactive reports (`NewsStatus=0`) are NOT visible on the public list
- [x] Verify: no management buttons rendered for Guest or Lecturer

### 📋 Staff Report History — FR-8 (4 tasks)
- [x] Add to `StaffController.cs`: `GET /Staff/MyReports` → `GetReportsByCreatorAsync(currentAccountId)` → view; `POST /Staff/MyReports/ToggleStatus/{id}` (AJAX JSON) → `ToggleStatusAsync(id, currentAccountId)` → return `{ success, newStatus }`
- [x] Create `Views/Staff/MyReports/Index.cshtml`: table (Title | Created Date | Sector | Tickers | Status badge | Toggle button | View Detail); "Archive" / "Restore" toggle label based on current status; "View Detail" → `/News/Detail/{id}`; empty state message
- [x] Verify: Staff A's list shows only Staff A's reports, not Staff B's
- [x] Verify: toggling to Inactive removes report from public `/News/Index`; still visible here

### 🧪 P3 Self-test (smoke tests to run before handoff)
- [x] Verify Category CRUD: create top-level + child; self-ref blocked; IsActive toggle instant; delete unreferenced succeeds; delete referenced blocked; inactive excluded from pipeline dropdown
- [x] Verify Tag CRUD: uppercase normalization; duplicate name blocked; delete linked tag blocked
- [x] Verify Report Viewer: only active reports show; detail page has all fields; Guest sees no management controls
- [x] Verify Staff History: filtered by creator; toggle updates DB `UpdatedByID` + `ModifiedDate`

---

## PERSON 4 — UI Developer | 52 tasks

> Owns all shared JS/CSS infrastructure, the Run Analysis UI, Profile Management, Staff Dashboard, documentation, and the shared smoke test tracker.

### 🎨 Shared CSS & UI Infrastructure (6 tasks)
- [X] Create `wwwroot/css/site.css`: decision badge color overrides (`bg-success`=BUY, `bg-danger`=SELL, `bg-secondary`=HOLD), modal z-index fixes, loading spinner sizing, navbar brand styling, form layout spacing, card hover effects for Dashboard
- [X] Create `wwwroot/js/modal-helpers.js`: `openModal(modalId)`, `closeModal(modalId)`, `submitModalForm(formId, listRefreshCallback)` (AJAX POST → success: close modal + call refresh; error: show inline error inside modal), `confirmDelete(entityName, deleteUrl, listRefreshCallback)` (populate `_ConfirmDeleteModal` → show → on confirm: AJAX POST → call refresh)
- [X] Create `wwwroot/js/toast-helpers.js`: `showSuccess(message)` (Bootstrap toast, green, auto-dismiss 3s), `showError(message)` (red, auto-dismiss 5s)
- [X] Create `wwwroot/js/validate-extensions.js`:
  - `dateRange` validator — blocks submit if StartDate > EndDate (used on Statistics filter)
  - `notSelf` validator — blocks submit if ParentCategoryID dropdown value == current CategoryID (used on Edit Category modal)
  - `passwordMatch` validator — blocks submit if ConfirmNewPassword ≠ NewPassword (used on Profile Change Password)
- [X] Wire `validate-extensions.js` onto the relevant forms via `data-val-*` attributes or explicit jQuery Validate calls
- [X] Test all shared JS helpers in isolation: open/close modal, confirm delete flow, success/error toast display

### 🚀 Run Analysis View & JS (4 tasks)
- [X] Create `Views/Staff/RunAnalysis.cshtml`: two `<select>` dropdowns (`SelectedTagId` and `SelectedCategoryId`), "Run Analysis" button (`id="btnRunAnalysis"`), hidden loading spinner (`<div id="loadingSpinner" class="d-none"><div class="spinner-border"></div> Analyzing...</div>`), result area (`<div id="resultArea" class="d-none">`)
- [X] Create `wwwroot/js/run-analysis.js`:
  - On `#btnRunAnalysis` click: disable button + show spinner + hide result area
  - `fetch('/Staff/RunAnalysis', { method: 'POST', headers: {...}, body: formData })`
  - On `result.success == true`: hide spinner, re-enable button, show `#resultArea` with green success alert + link `<a href="/News/Detail/{result.newsArticleId}">View Report →</a>`
  - On `result.success == false`: hide spinner, re-enable button, show `#resultArea` with red alert containing `result.errorMessage`
  - On network error: hide spinner, re-enable button, show "Unexpected network error. Please try again."
- [X] Verify: button is disabled and spinner visible during pipeline execution (cannot double-submit)
- [X] Verify: success case shows green alert with correct link to new report; error case shows red alert with the specific message from the pipeline

### 👤 Profile Management — FR-9 (12 tasks)
- [X] Add to `StaffController.cs`: `GET /Staff/Profile` → query current account from DB by `AccountID` claim → populate `ProfileViewModel` → return view
- [X] Add `POST /Staff/Profile/UpdateName` (AntiForgery): validate `UpdateNameViewModel` → call `_accountService.UpdateAccountNameAsync()` → TempData["NameSuccess"] = "Profile updated successfully." → redirect to GET
- [X] Add `POST /Staff/Profile/ChangePassword` (AntiForgery): validate `ChangePasswordViewModel` → call `_accountService.ChangePasswordAsync()` → on fail: ModelState error "Current password is incorrect." → on success: TempData["PwdSuccess"] = "Password changed successfully." → redirect to GET
- [X] Create `ProfileViewModel.cs`: AccountName (editable), AccountEmail (read-only), AccountRoleLabel ("Staff")
- [X] Create `UpdateNameViewModel.cs`: AccountName (Required, 2–100 chars)
- [X] Create `ChangePasswordViewModel.cs`: CurrentPassword (Required), NewPassword (Required, MinLength 8), ConfirmNewPassword (Required, `[Compare("NewPassword")]`)
- [X] Create `Views/Staff/Profile/Index.cshtml`:
  - Section 1 "Update Display Name": AccountName input, Save Name button (separate form, POST to UpdateName); success message from `TempData["NameSuccess"]`
  - Section 2 "Change Password": CurrentPassword, NewPassword, ConfirmNewPassword inputs; Save Password button (separate form, POST to ChangePassword); success message from `TempData["PwdSuccess"]`
  - AccountEmail and AccountRole as read-only `<p>` elements (not `<input>`)
  - Each section is a separate Bootstrap card
- [X] Verify: AccountEmail and AccountRole have no `<input>` — cannot be submitted
- [X] Verify: wrong current password → "Current password is incorrect." inline; password unchanged
- [X] Verify: mismatched new passwords → `[Compare]` validation error shown before submit
- [X] Verify: new password < 8 chars → MinLength validation error shown before submit
- [X] Verify: successful password change → log out → log in with new password → access granted

### 🏠 Staff Dashboard (4 tasks)
- [x] Add `GET /Staff/Dashboard` to `StaffController.cs`: call `GetReportsByCreatorAsync(currentAccountId)` for count; return view with AccountName claim + report count
- [x] Create `Views/Staff/Dashboard/Index.cshtml`: welcome heading with AccountName; Bootstrap card grid with: "Run Analysis" (link + icon + description), "Manage Categories" (link + icon), "Manage Tags" (link + icon), "My Reports" (link + icon + badge showing report count), "My Profile" (link + icon)
- [x] Verify: report count badge on "My Reports" card matches actual row count in DB
- [x] Verify: all 5 card links navigate to correct pages

### ⚡ Real-time UI Sync & Modal Optimization (7 tasks)
- [x] Build central Toast Notification system (`window.showCustomToast`) in `_Layout.cshtml` with CSS transitions, alert icons, and status-based classes (`type-create`, `type-update`, `type-delete`)
- [x] Configure SignalR client-side hub connections (`presenceConnection`, `notificationConnection`) in `_Layout.cshtml` and synchronize the URL endpoint path `/hubs/notifications`
- [x] Set Toast container `.custom-toast-container` `z-index` to `99999` to overlay correctly on all UI components including active modals
- [x] Implement non-disruptive SPA-like real-time data sync via `window.refreshPageContentRealtime()` using Fetch API and DOMParser to update `<main>` without full page reloads
- [x] Refactor all client scripts (`ajax-crud.js`, `myreports.js`, `categories.js`, `_ConfirmDeleteModal.cshtml`) to replace destructive `location.reload()` with real-time UI refresh
- [x] Solve modal backdrop overlay multiplication bugs ("Đang hơi tối nha") by implementing `syncModalsToBody()` to clean up duplicate modal elements in the DOM
- [x] Refactor JS controllers (`categories.js`, `tags.js`) to use `bootstrap.Modal.getOrCreateInstance` for correct modal instance lifecycle management

### 📝 Documentation & Demo Prep (14 tasks)
- [x] Write `README.md`: prerequisites (.NET 8 SDK, SQL Server LocalDB, Visual Studio 2022); setup steps (clone → fill `appsettings.json` → `dotnet ef database update` → Run); default credentials table; folder structure explanation; known limitations
- [x] Create `appsettings.json.example`: all required keys with placeholder values — safe to commit to repo
- [x] Write inline XML doc comments (`///`) on: `TradingAgentService.RunAnalysisAsync()` (each step), `TradingAgentService.PreprocessJsonResponse()` (why needed), all Repository interface methods (one-liner each), `FUNewsManagementContext.OnModelCreating()` (each config block)
- [X] Create `TESTING.md`: shared smoke test tracker — one table with Feature | Test Case | Owner | PASS/FAIL/NOTES; each person fills in their own rows after self-testing
- [X] Create `KNOWN_ISSUES.md`: honest list of any failing tests, partial implementations, or workarounds discovered during self-testing
- [X] Create 2 Admin Accounts and delete 1 to confirm self-deletion block works
- [ ] Run pipeline on 3 tickers (AAPL, NVDA, TSLA) across 3 different sectors to populate demo reports before grading
- [ ] Archive 1 report (set Inactive) so graders can verify visibility behavior
- [ ] Screenshot all key screens and save to `/docs/screenshots/`: Login, Staff Dashboard, Run Analysis (success state), Report List (with BUY/SELL/HOLD badges), Report Detail, Category Management (with one inactive), Account Management, Statistics results
- [X] Write `ARCHITECTURE_NOTES.md`: DI registrations, auth flow summary, route map, TradingAgentService call chain — P1 to review and sign off before submission
- [ ] Final pre-submission checklist: `appsettings.json` gitignored, `appsettings.json.example` committed, `README.md` complete, screenshots present, `TESTING.md` filled by all members, app launches cleanly from a fresh clone on a different machine
- [ ] Do a dry-run demo walkthrough: follow the grading rubric top to bottom, simulate a grader's session; note anything that breaks and report to the team

---

## Contingency Plan

| Who Leaves | Immediately Reassign To |
|------------|------------------------|
| P4 (UI Dev) | P3 takes modal-helpers.js, toast-helpers.js, and CSS; P1 takes Run Analysis view/JS (they own the controller anyway); P2 takes Statistics view; P3 takes Profile Management + Dashboard; P1 writes README; each person writes their own doc comments |
| P3 (Full-stack) | P2 takes Category/Tag controllers + service wiring; P4 takes all Category/Tag Views and JS (same modal pattern, just different entities); P4 takes Report Viewer views (read-only, no complex logic); P2 adds Staff History ToggleStatus endpoint; P4 builds the History view |
| P2 (Backend) | P1 takes all Repository implementations (critical — P3 is blocked without them); P1 takes Account Management controller; P4 takes Account Management views/JS; P1 takes Statistics controller; P4 takes Statistics view — **P2 must push all interface files by Day 2 even if implementations are stubs** |
| P1 (Lead) | **High risk.** P2 rebuilds entity classes + DbContext from PRD schema; P2 implements Cookie Auth from ASP.NET Core docs; P2 implements TradingAgentService from PRD prompt specs (expect 3–4 extra days); P3 takes `_Layout.cshtml` and Run Analysis view; `ARCHITECTURE_NOTES.md` written by P1 at end of Day 1 is the primary handoff document |
