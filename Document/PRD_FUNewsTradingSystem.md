# Product Requirements Document: FUNewsTradingSystem (FNTS)

**Project Name:** FUNewsTradingSystem (Automated Trading Agents by News)
**Document Version:** 2.0
**Target Framework:** ASP.NET Core MVC (.NET 8)
**Architecture:** 3-Layer Architecture (Presentation → Service → Repository)
**Last Updated:** 2025

---

## Product Overview

**Product Vision:**
FUNewsTradingSystem (FNTS) is an ASP.NET Core MVC web application that helps financial institutions and educational organizations manage market news and automatically analyze stock trends through a multi-agent AI pipeline. It generates actionable trading recommendations (BUY / SELL / HOLD) from real-world news without any manual data entry.

**Target Users:**
- **Primary:** Staff (System Operator) — the role that operates the AI pipeline and manages reference data.
- **Secondary:** Lecturer (Investor) — reads AI-generated analysis reports; Admin — manages the system and user accounts; Guest — unauthenticated visitor who browses public reports.

**Business Objectives:**
1. Fully automate the news-to-trading-decision workflow using a three-step LLM agent pipeline, eliminating manual analysis effort.
2. Provide a structured, role-gated platform for managing financial reference data (sectors, tickers) and generated reports.
3. Satisfy the academic requirement of strict 3-Layer Architecture (no Controller-to-DB direct calls) and the Singleton design pattern.
4. Integrate an external News API and an LLM API within a resilient, fully asynchronous pipeline.

**Success Metrics:**
- The AI pipeline completes and auto-saves a report to the database within ≤ 30 seconds of Staff clicking "Run Analysis."
- Pipeline error rate (timeout, JSON parse failure, API error) is < 5% across all executions.
- 100% of Create and Update operations are delivered via modal popups — no standalone Create/Update pages exist.
- 100% of protected controller actions are secured with correct role-based authorization filters.
- The Admin statistical report correctly filters and returns results in descending order by `CreatedDate` for any valid date range.

---

## User Personas

### Persona 1: Admin (System Administrator)
- **Demographics:** IT staff or senior manager; highly proficient with web systems; no financial domain knowledge required.
- **Goals:**
  - Manage all system accounts (Create, Read, Update, Delete).
  - Generate statistical reports of AI-created articles within a custom date range, sorted descending by creation date.
- **Pain Points:**
  - No time-range filtering tool for monitoring system activity.
  - The default Admin account must be pre-seeded — manual bootstrapping is not acceptable.
- **User Journey:**
  1. Open the application → the Login page appears by default.
  2. Log in with `admin@FUNewsTradingSystem.org` / `@@abc123@@` (credentials sourced from `appsettings.json`).
  3. Navigate to Account Management → Create / Read / Update / Delete user accounts via modal popups.
  4. Navigate to Statistical Report → select StartDate and EndDate → view the filtered report list sorted descending.
  5. Log out.

---

### Persona 2: Staff (System Operator)
- **Demographics:** Financial analyst or technical operator; basic understanding of stock markets; comfortable with web UIs.
- **Goals:**
  - Manage Market Sectors (Category) and Stock Tickers (Tag).
  - Trigger the AI pipeline to auto-generate a trading analysis report for a selected ticker and sector.
  - Review the history of reports they personally generated.
  - Manage their own profile (name and password).
- **Pain Points:**
  - Manual news collection and analysis is time-consuming (30–60 minutes per ticker).
  - No clear feedback when the AI pipeline fails silently.
- **User Journey:**
  1. Log in with an account where `AccountRole = 1`.
  2. Navigate to Category Management → CRUD Market Sectors via modal popups.
  3. Navigate to Tag Management → CRUD Stock Tickers via modal popups.
  4. Navigate to "Run Analysis" → select a Ticker (Tag) and Sector (Category) → click "Run Analysis."
  5. System fetches news → runs 3 LLM agents → parses JSON → saves `NewsArticle` to DB.
  6. Receive a success notification with a link to the new report, or a descriptive error message on failure.
  7. Navigate to Report History to browse all reports they have created.
  8. Log out.

---

### Persona 3: Lecturer (Investor)
- **Demographics:** University lecturer or individual investor; understands financial markets; no administrative privileges.
- **Goals:**
  - Browse all active trading analysis reports.
  - Read detailed AI insights: Sentiment view, Fundamental view, Risk warnings, and the final BUY/SELL/HOLD decision.
- **Pain Points:**
  - No consolidated tool to view aggregated AI analysis without doing their own research.
- **User Journey:**
  1. Log in with an account where `AccountRole = 2`.
  2. View the list of active Trading Analysis Reports.
  3. Click on a report → read the full AI-generated detail page.
  4. Log out.

---

### Persona 4: Guest (Unauthenticated Visitor)
- **Demographics:** Any person accessing the system without an account.
- **Goals:**
  - View the list of active Trading Analysis Reports and the AI's top-level predictions.
- **Pain Points:**
  - Cannot access inactive reports or any management functionality.
- **User Journey:**
  1. Access the application's public report listing page (no login required).
  2. Browse reports where `NewsStatus = active`.
  3. Click a report to view its detail page.
  4. Cannot perform any Create, Update, or Delete action; no management navigation is rendered.

---

## Feature Requirements

| Feature | Description | User Story | Priority | Acceptance Criteria | Dependencies |
|---------|-------------|------------|----------|---------------------|--------------|
| **FR-1: Authentication** | Email + Password login. The Login page is the application's default landing page. | As a user, I want to log in with my email and password so I can access features appropriate to my role. | **Must** | 1. Application root URL (`/`) redirects unauthenticated users to the Login page. 2. Successful login redirects: Role=1 → Staff Dashboard; Role=2 → Report List; Role=3 → Admin Dashboard. 3. Failed login displays inline: "Invalid email or password." No redirect occurs. 4. No hint is given about whether the email exists in the system. 5. A Logout option is available on all authenticated pages and destroys the session cookie immediately upon click. 6. Accessing any protected route while unauthenticated redirects to the Login page. | `SystemAccount` DB entity; Admin credentials in `appsettings.json` |
| **FR-2: Authorization (RBAC)** | Role-based access control enforced via authorization filters on every protected controller action. | As the system, I want to restrict every controller action by role so that unauthorized access is impossible. | **Must** | 1. Guest: read-only access to active reports — no login required. 2. Lecturer (Role=2): read-only access to active reports and their full detail pages. 3. Staff (Role=1): Category CRUD, Tag CRUD, Run Analysis, Report History, Profile Management. 4. Admin (Role=3): Account CRUD, Statistical Report. 5. Any role violation returns HTTP 403 or redirects to Login — no partial data is exposed. 6. Admin cannot access the Run Analysis page. Staff cannot access Account Management or Statistical Report. | FR-1 complete |
| **FR-3: AI Trading Pipeline** | Staff selects Ticker + Category → system calls News API → 3-step LLM agent sequence → auto-saves report to DB. | As a Staff member, I want to trigger an automated AI analysis for a stock ticker so that a trading report is generated and saved without manual input. | **Must** | 1. UI provides: Ticker dropdown (from `Tag` table), Sector dropdown (from active `Category` records), "Run Analysis" button. 2. On submit, a loading spinner appears and the button is disabled to prevent duplicate submissions. 3. News API is called asynchronously. If 0 headlines returned → abort; display "No news found for this ticker." 4. System sends headlines through 3 sequential LLM calls: Sentiment Agent → Fundamental Agent → Portfolio Manager. 5. Portfolio Manager must return a valid JSON with exactly these fields: `decision` ("BUY", "SELL", or "HOLD"), `title`, `headline`, `content`, `source`. 6. If `decision` is not one of the three valid values → abort; display "AI returned an invalid decision. Please try again." 7. On success, one `NewsArticle` and one `NewsTag` record are inserted within a single DB transaction. 8. `NewsTitle` = `"[{decision}] {TagName} Automated Analysis"`. `CreatedByID` = current Staff's `AccountID`. `NewsStatus` = 1 (active). 9. Success: display "Analysis report generated successfully." with a link to the new report. 10. Any failure (API timeout, parse error, DB error) → display a specific error message; no partial DB commit. | FR-2; API keys in `appsettings.json`; `TradingAgentService` as Singleton |
| **FR-4: Account Management (Admin)** | Admin performs full CRUD on `SystemAccount`. Admin cannot delete their own account or create another Admin. | As an Admin, I want to create, view, update, and delete user accounts so I can control who has access to the system. | **Must** | 1. Account list displays: AccountID, AccountName, AccountEmail, AccountRole label ("Staff" / "Lecturer"). 2. Create and Update are performed via modal popup — no standalone page. 3. Create form: AccountName (required, 2–100 chars), AccountEmail (required, unique, valid email format), AccountPassword (required, ≥ 8 chars), AccountRole (dropdown: 1=Staff or 2=Lecturer only — Role=3 is not selectable). 4. Update: pre-populates current values; leaving the Password field blank retains the existing hash. 5. Delete: confirmation dialog required. Admin's own row shows no Delete button (tooltip: "You cannot delete your own account."). 6. All inputs validate client-side and server-side; errors display inline next to the relevant field. | FR-2 |
| **FR-5: Category Management (Staff)** | Staff performs full CRUD on `Category`. Deletion blocked if Category is referenced by any `NewsArticle`. | As a Staff member, I want to manage market sectors so that trading reports can be accurately categorized. | **Must** | 1. List displays: CategoryID, CategoryName, CategoryDescription, Parent Category name (or "—"), IsActive toggle. 2. Create and Update via modal popup. Fields: CategoryName (required), CategoryDescription (optional), ParentCategoryID (dropdown of top-level categories, optional), IsActive (checkbox, defaults checked). 3. A category cannot be set as its own parent. 4. IsActive is toggled directly from the list row via AJAX — no modal required for this action. 5. Delete: confirmation dialog. If Category is referenced by ≥ 1 `NewsArticle` → reject: "Cannot delete: this sector is linked to existing reports." If it has child categories with no linked reports → child `ParentCategoryID` set to null, then parent deleted (single transaction). | FR-2; `Category` DB entity |
| **FR-6: Tag Management (Staff)** | Staff performs full CRUD on `Tag`. Deletion blocked if Tag is referenced by any `NewsTag` record. | As a Staff member, I want to manage stock tickers so I can link them to analysis reports accurately. | **Must** | 1. List displays: TagID, TagName, Note. 2. Create and Update via modal popup. Fields: TagName (required, unique, max 50 chars, stored uppercase), Note (optional, max 500 chars). 3. Delete: confirmation dialog. If Tag is referenced by ≥ 1 `NewsTag` record → reject: "Cannot delete: this ticker is linked to existing reports." | FR-2; `Tag` DB entity |
| **FR-7: Trading Report Viewer** | Guests and Lecturers browse and read active Trading Analysis Reports. | As a Guest or Lecturer, I want to view active trading reports and AI predictions so I can make informed decisions. | **Must** | 1. Report list accessible without login. 2. Shows only `NewsArticle` where `NewsStatus = 1`, sorted descending by `CreatedDate`. 3. Each list item displays: NewsTitle (with color-coded decision badge: green=BUY, red=SELL, gray=HOLD), Headline, CreatedDate (`YYYY-MM-DD HH:mm UTC`), Category name, associated Ticker tags. 4. Detail page shows: NewsTitle, Headline, CreatedDate, CategoryName, all Tags, full NewsContent, NewsSource. 5. No management controls (Create/Edit/Delete buttons) are rendered for Guest or Lecturer. | FR-2; `NewsArticle`, `NewsTag`, `Category`, `Tag` entities |
| **FR-8: Staff Report History** | Staff views all reports they personally generated and can toggle report visibility. | As a Staff member, I want to see all reports I have created and control their visibility so I can manage my analysis output. | **Should** | 1. Filtered to `NewsArticle` where `CreatedByID = current Staff AccountID`. 2. List displays: NewsTitle, CreatedDate, CategoryName, Tags, NewsStatus (Active / Inactive badge). 3. Staff can toggle `NewsStatus` (active ↔ inactive) for any of their own reports via a button on the list row (AJAX, no page reload). 4. On toggle: `UpdatedByID` = current Staff's `AccountID`, `ModifiedDate` = `DateTime.UtcNow`. 5. Staff can click any report to view its full detail page. | FR-2; FR-3 |
| **FR-9: Profile Management (Staff)** | Staff views and updates their own account name and password. | As a Staff member, I want to update my display name and password so my account information stays accurate and secure. | **Should** | 1. Page shows: AccountName (editable), AccountEmail (read-only), AccountRole (read-only, label "Staff"). 2. Name update and password change are separate form submissions. 3. Password change requires: current password (verified against stored hash), new password (≥ 8 chars), confirm new password (must match new password). 4. If current password does not match → display "Current password is incorrect." 5. Successful save displays "Profile updated successfully." 6. AccountEmail and AccountRole cannot be changed from this page. | FR-2 |
| **FR-10: Admin Statistical Report** | Admin filters AI-generated articles by date range; results sorted descending by creation date. | As an Admin, I want to filter all AI-generated articles by date range so I can monitor system usage over time. | **Must** | 1. Page loads empty (no results) — results appear only after the filter form is submitted. 2. Inputs: StartDate and EndDate (both required, date picker). 3. Validation: StartDate ≤ EndDate; if violated → "Start date must be before or equal to end date." 4. Filter: `CreatedDate >= StartDate 00:00:00 UTC` AND `CreatedDate <= EndDate 23:59:59 UTC`. 5. Results sorted descending by `CreatedDate`. 6. Each row: NewsTitle, Headline, CreatedDate, CategoryName, Created By (`AccountName`, or "Deleted User" if `CreatedByID` is null). 7. No results → "No reports found for the selected period." | FR-2; `NewsArticle`, `SystemAccount` entities |
| **FR-11: Global UI Constraints** | All Create/Update use modals. All Delete requires confirmation. Full client-side and server-side validation everywhere. | As a user, I want a consistent UI where edits happen in-context and deletions are always confirmed before executing. | **Must** | 1. No standalone Create or Update pages exist for any entity. 2. Modals close automatically and the list refreshes (AJAX / partial view) after a successful Create, Update, or Delete — no full page reload. 3. Every Delete triggers a confirmation dialog before any DB operation. 4. Client-side validation runs before the AJAX call. Server-side validation is always enforced regardless of client-side result. 5. Validation errors display inline, adjacent to the failing field. 6. All date inputs use an HTML5 date picker — free-text date entry is not accepted. 7. Anti-CSRF tokens (`AntiForgeryToken`) are included on all state-changing forms. | All CRUD FRs |

---

## User Flows

### Flow 1: User Login
1. User opens the application → **Login** page renders (default route `/`).
2. User enters **Email** and **Password** → clicks **"Login"**.
3. System queries `SystemAccount` by `AccountEmail`, verifies password against stored PBKDF2 hash.
   - **Success:** Session cookie created (AccountID, AccountRole; sliding 60-min expiry) → redirect by role:
     - Role=1 → Staff Dashboard | Role=2 → Report List | Role=3 → Admin Dashboard
   - **Failure:** Inline error "Invalid email or password." No redirect. No disclosure of email existence.
4. The default Admin account is seeded at application startup if no `AccountRole = 3` record exists in the DB.

---

### Flow 2: Staff Triggers the AI Trading Pipeline
1. Staff logs in → navigates to **"Run Analysis"** page.
2. Selects **Ticker** (Tag dropdown) and **Sector** (active Category dropdown) → clicks **"Run Analysis"**.
3. Button disables; loading spinner appears.
4. `TradingAgentService` executes asynchronously:

   **Step 1 — Fetch News**
   Calls NewsAPI.org `GET /v2/everything?q={TagName}&sortBy=publishedAt&pageSize=10`. If 0 results → abort with "No news found for this ticker."

   **Step 2 — Sentiment Agent**
   POSTs headlines (title + description, numbered list) to OpenAI Chat Completions with the Sentiment Agent prompt. Receives a plain-text sentiment paragraph.

   **Step 3 — Fundamental Agent**
   POSTs headlines + sentiment output to OpenAI with the Fundamental Agent prompt. Receives a plain-text fundamental analysis paragraph.

   **Step 4 — Portfolio Manager**
   POSTs sentiment + fundamental outputs to OpenAI with the Portfolio Manager prompt. LLM returns a strict JSON string. System strips any markdown code fences, then deserializes. Validates: `decision` ∈ {"BUY","SELL","HOLD"} (case-normalized); all five fields non-empty.

5. **DB Write (single transaction):**
   - Insert `NewsArticle` with auto-generated `NewsTitle = "[{decision}] {TagName} Automated Analysis"`, `CreatedByID` = Staff's AccountID, `CreatedDate = DateTime.UtcNow`, `NewsStatus = 1`.
   - Insert `NewsTag (NewsArticleID, TagID)`.
   - Transaction rolled back on any DB failure.

6. Success → spinner hides, button re-enables, notification: "Analysis report generated successfully." + link to new report.
   - Any failure → specific error message displayed; no DB change.

---

### Flow 3: Admin Manages User Accounts (CRUD)
1. Admin logs in → **Account Management**.
2. **Read:** Table — AccountID, AccountName, AccountEmail, AccountRole label, Edit, Delete buttons.
3. **Create:** "Add Account" → modal → fill AccountName, AccountEmail, AccountPassword, AccountRole (Staff/Lecturer only) → submit → validate → save → modal closes → list refreshes. Duplicate email → inline "This email is already registered."
4. **Update:** "Edit" on row → modal pre-filled → edit → submit → validate → save → modal closes → list refreshes. Blank password field = retain existing hash.
5. **Delete:** "Delete" on row → confirmation dialog → confirm → delete → list refreshes. Admin's own row: Delete button hidden.

---

### Flow 4: Staff Manages Categories (CRUD)
1. Staff logs in → **Category Management**.
2. **Read:** Table — CategoryID, CategoryName, CategoryDescription, Parent name, IsActive toggle.
3. **Create / Update:** Modal popup → validate → save.
4. **IsActive Toggle:** Clicking toggle on row → AJAX request → flip `IsActive` → UI updates inline. No modal.
5. **Delete:** Confirmation dialog → server checks `NewsArticle` references.
   - Referenced → reject: "Cannot delete: this sector is linked to existing reports."
   - Unreferenced with children → set children's `ParentCategoryID = null`, then delete parent (single transaction).
   - Unreferenced, no children → delete directly.

---

### Flow 5: Guest / Lecturer Views Reports
1. User accesses report listing page (no login needed for Guest).
2. System queries `NewsArticle WHERE NewsStatus = 1` joined with `Category`, `NewsTag`, `Tag`; sorted descending by `CreatedDate`.
3. List renders with color-coded decision badges (BUY=green, SELL=red, HOLD=gray).
4. User clicks a report → detail page: NewsTitle, Headline, CreatedDate, CategoryName, Tags, NewsContent, NewsSource.
5. No Create/Edit/Delete controls rendered for Guest or Lecturer.

---

### Flow 6: Admin Views Statistical Report
1. Admin navigates to **Statistical Report** — page loads empty.
2. Enters **StartDate** and **EndDate** → clicks **"Generate Report"**.
3. Client validates StartDate ≤ EndDate before submitting.
4. Server filters `NewsArticle WHERE CreatedDate BETWEEN StartDate 00:00:00 AND EndDate 23:59:59 (UTC)`, joins `SystemAccount` for creator name, sorts descending.
5. Results table renders on the same page. Empty result → "No reports found for the selected period."

---

## Non-Functional Requirements

### Performance
- **Page Load Time:** All list and detail pages load in ≤ 3 seconds under normal conditions (single machine, SQL Server LocalDB).
- **Pipeline Completion:** Full pipeline (News API + 3 LLM calls + DB write) completes in ≤ 30 seconds. Each individual external API call has a **10-second timeout**.
- **Concurrent Users:** Supports at least 20 concurrent authenticated users without degradation.
- **Async Enforcement:** Every external API call and DB operation must use `async/await`. Blocking `.Result` or `.Wait()` calls are forbidden.

### Security
- **Authentication:** Cookie-based session. Cookie is `HttpOnly` and `Secure`. **Sliding expiration: 60 minutes.**
- **Authorization:** Every non-public controller action is decorated with a role authorization filter. UI-hiding alone is insufficient.
- **Password Storage:** Passwords are hashed with **ASP.NET Core `IPasswordHasher<T>` (PBKDF2)**. Plain-text passwords are never stored.
- **Secret Management:** Connection string, Admin credentials, News API key, and LLM API key are stored only in `appsettings.json`. Never hard-coded in C# source files.
- **CSRF Protection:** `@Html.AntiForgeryToken()` on all state-changing forms; `[ValidateAntiForgeryToken]` on all POST actions.
- **SQL Injection Prevention:** All DB operations use EF Core parameterized queries. Raw SQL is forbidden.

### Compatibility
- **Devices:** Desktop and laptop (Windows/macOS). Mobile is not a requirement.
- **Browsers:** Google Chrome (latest stable), Microsoft Edge (latest stable). Internet Explorer is not supported.
- **Minimum Screen Resolution:** 1280 × 720 px.

### Accessibility
- All form inputs have associated `<label>` elements.
- Error messages are conveyed as text (not color alone).
- Action buttons have descriptive text (e.g., "Save Category," not just "Save").

---

## Technical Specifications
### Project Folder Structure
```
{GroupName}_{ClassCode}_A01.sln                     # Solution goc
└── {StudentName}MVC                                  # ASP.NET Core MVC Web Project
    ├── Controllers                                   # [Layer 3: Presentation]
    │   ├── AccountController.cs                      # Dang nhap, Dang xuat, Quan ly Profile (FR-1, FR-9)
    │   ├── AdminController.cs                        # Quan ly Users (Admin), Bao cao thong ke (FR-4, FR-10)
    │   ├── CategoryController.cs                     # Quan ly Sector / Category (FR-5)
    │   ├── TagController.cs                          # Quan ly Ticker / Tag (FR-6)
    │   ├── ReportController.cs                       # Xem bao cao public, Lich su cua Staff (FR-7, FR-8)
    │   └── AnalysisController.cs                     # Giao dien chay AI Trading Pipeline (FR-3)
    │
    ├── Services                                      # [Layer 2: Service / Business Logic]
    │   ├── Interfaces
    │   │   ├── ISystemAccountService.cs
    │   │   ├── ICategoryService.cs
    │   │   ├── ITagService.cs
    │   │   ├── INewsArticleService.cs
    │   │   └── ITradingAgentService.cs               # Chua logic goi News API & OpenAI (Singleton)
    │   └── Implements
    │       ├── SystemAccountService.cs
    │       ├── CategoryService.cs
    │       ├── TagService.cs
    │       ├── NewsArticleService.cs
    │       └── TradingAgentService.cs                # Xu ly HttpClient, LLM Prompts, JSON parsing
    │
    ├── Repositories                                  # [Layer 1: Repository / Data Access]
    │   ├── Interfaces
    │   │   ├── ISystemAccountRepository.cs
    │   │   ├── ICategoryRepository.cs
    │   │   ├── ITagRepository.cs
    │   │   ├── INewsArticleRepository.cs
    │   │   └── INewsTagRepository.cs
    │   └── Implements
    │       ├── SystemAccountRepository.cs
    │       ├── CategoryRepository.cs
    │       ├── TagRepository.cs
    │       ├── NewsArticleRepository.cs
    │       └── NewsTagRepository.cs
    │
    ├── Models                                        # Entity Framework Core Classes (Database Models)
    │   ├── FUNewsManagementContext.cs                # DbContext (Ket noi EF Core, cau hinh Fluent API / Seed data)
    │   ├── SystemAccount.cs
    │   ├── Category.cs
    │   ├── Tag.cs
    │   ├── NewsArticle.cs
    │   └── NewsTag.cs
    │
    ├── ViewModels                                    # Data Transfer Objects cho Views (Tranh lo Entities)
    │   ├── Auth
    │   │   ├── LoginViewModel.cs
    │   │   └── ProfileUpdateViewModel.cs
    │   ├── Admin
    │   │   ├── AccountFormViewModel.cs               # Dung cho Create/Update Account Modal
    │   │   └── StatisticalReportViewModel.cs
    │   ├── Category
    │   │   └── CategoryFormViewModel.cs              # Dung cho Category Modal
    │   ├── Tag
    │   │   └── TagFormViewModel.cs                   # Dung cho Tag Modal
    │   └── Report
    │       ├── RunAnalysisViewModel.cs               # Chua danh sach dropdown Ticker & Sector
    │       └── ReportDetailViewModel.cs              # Chua data tong hop cho trang doc bao cao
    │
    ├── Views                                         # Razor Views
    │   ├── Account
    │   │   ├── Login.cshtml                          # Default landing page
    │   │   └── Profile.cshtml                        # Doi ten va mat khau
    │   ├── Admin
    │   │   ├── Index.cshtml                          # Quan ly Account list
    │   │   ├── _AccountModal.cshtml                  # Partial View: Create/Update Account (Modal)
    │   │   └── StatisticalReport.cshtml              # Bao cao loc theo Date Range
    │   ├── Category
    │   │   ├── Index.cshtml                          # Quan ly Category list
    │   │   └── _CategoryModal.cshtml                 # Partial View: Create/Update Category (Modal)
    │   ├── Tag
    │   │   ├── Index.cshtml                          # Quan ly Tag list
    │   │   └── _TagModal.cshtml                      # Partial View: Create/Update Tag (Modal)
    │   ├── Report
    │   │   ├── Index.cshtml                          # Danh sach bao cao Public (Cho Guest / Lecturer)
    │   │   ├── Details.cshtml                        # Chi tiet bai bao cao & AI Decision
    │   │   └── History.cshtml                        # Lich su bao cao ca nhan cua Staff (Co toggle IsActive)
    │   ├── Analysis
    │   │   └── Index.cshtml                          # Giao dien Run Analysis (chon Ticker, Sector & goi AI)
    │   └── Shared
    │       ├── _Layout.cshtml                        # Giao dien goc (chua Header, Navigation phan quyen)
    │       ├── _ValidationScriptsPartial.cshtml      # Script cho Client-side validation
    │       └── _Alerts.cshtml                        # Partial View cho Toast / Inline notifications
    │
    ├── wwwroot                                       # Static files
    │   ├── css
    │   │   └── site.css                              # Custom styles (Mau badge BUY/SELL/HOLD, Modal z-index...)
    │   ├── js
    │   │   ├── site.js                               # Khoi tao Modals, xu ly AJAX toggles
    │   │   ├── ajax-crud.js                          # Script chuyen xu ly Form submit qua AJAX cho Modals
    │   │   └── pipeline-spinner.js                   # Xu ly Loading Spinner va vo hieu hoa nut khi chay AI
    │   └── lib                                   # Bootstrap 5, jQuery, jquery-validate, v.v.
    │
    ├── Migrations                                    # EF Core Migrations (Tu dong sinh ra)
    │
    ├── appsettings.json                              # Chua ConnectionString, Admin Seed, News API Key, OpenAI Key
    ├── appsettings.Development.json
    └── Program.cs                                    # Dang ky Dependency Injection (Singleton, Scoped), cau hinh Auth Cookie.
```

### Frontend
- **Stack:** ASP.NET Core Razor Views, HTML5, CSS3, Bootstrap 5, jQuery 3.x.
- **Modals:** Bootstrap 5 Modal component. Forms submit via jQuery AJAX. On success, modal closes and list updates without full page reload.
- **Validation:** `jquery-validate` + `jquery-validate-unobtrusive` (client-side); DataAnnotations on ViewModels (server-side).
- **Date Inputs:** HTML5 `<input type="date">`. No free-text date entry accepted.

### Backend
- **Stack:** ASP.NET Core MVC, .NET 8, C# 12.
- **3-Layer Architecture (strictly enforced):**
  - **Presentation:** Controllers + Razor Views. Controllers call only Service interfaces.
  - **Service:** Business logic, pipeline orchestration, external API calls. Services call only Repository interfaces.
  - **Repository / Data Access:** The only layer that interacts with `DbContext`. No business logic here.
- **Dependency Injection (`Program.cs`):**
  - `AddSingleton<ITradingAgentService, TradingAgentService>()` — reuses `HttpClient`.
  - All other Services and Repositories: `AddScoped<>()`.
- **External API Integration (in `TradingAgentService`):**
  - News API: HTTP GET, JSON. Top 10 most recent articles per request (title + description only).
  - LLM API: HTTP POST, JSON. Three sequential calls per pipeline run.
  - All calls use `HttpClient` with `Timeout = TimeSpan.FromSeconds(10)`.
- **Error Handling:** All external calls wrapped in `try/catch`. Errors logged via `ILogger` and a user-friendly message returned to the controller.
- **ViewModels:** Dedicated ViewModel classes are used for all Controller ↔ View data transfer. DB entity classes are never exposed directly to Views.

### LLM Prompt Specifications

**Prompt 1 — Sentiment Agent:**
```
You are a financial Sentiment Analyst. Given the following recent news headlines about {ticker}:

{headlines_numbered_list}

Analyze the prevailing market sentiment. Classify it as Positive, Negative, or Neutral.
Provide a concise reasoning paragraph of 2–3 sentences.
Respond with only the analysis text — no JSON, no headers, no labels.
```

**Prompt 2 — Fundamental Agent:**
```
You are a Financial Fundamental Analyst. Given the following recent news headlines about {ticker}:

{headlines_numbered_list}

And the following Sentiment Analysis:
{sentiment_output}

Evaluate the core business and fundamental impact of this news on {ticker}.
Consider revenue implications, competitive positioning, and long-term outlook.
Provide a concise analysis of 2–3 sentences.
Respond with only the analysis text — no JSON, no headers, no labels.
```

**Prompt 3 — Portfolio Manager (strict JSON output):**
```
You are a Portfolio Manager. Based on the analyses below for {ticker}, produce a final trading decision.

Sentiment Analysis:
{sentiment_output}

Fundamental Analysis:
{fundamental_output}

Respond ONLY with a single valid JSON object. Do not include any text before or after the JSON.
Do not use markdown code fences. The JSON must conform exactly to this schema:
{
  "decision": "BUY" | "SELL" | "HOLD",
  "title": "A concise title that includes the decision and ticker symbol",
  "headline": "One sentence summarizing the core reasoning for the decision",
  "content": "A structured paragraph covering: (1) Sentiment view, (2) Fundamental view, (3) Key risk warnings",
  "source": "Description of the data sources and AI model used"
}
```

### Database

**Database Name:** `FUNewsManagement`
**Technology:** SQL Server 2019+ / LocalDB (development)
**ORM:** Entity Framework Core 8, Code-First with Migrations

**Entity: SystemAccount**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AccountID | int | PK, Identity(1,1) | Unique identifier |
| AccountName | nvarchar(100) | NOT NULL | User's full display name |
| AccountEmail | nvarchar(200) | NOT NULL, UNIQUE | Login email address |
| AccountRole | int | NOT NULL | 1=Staff, 2=Lecturer, 3=Admin |
| AccountPassword | nvarchar(500) | NOT NULL | PBKDF2-hashed password |

**Entity: Category**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CategoryID | int | PK, Identity(1,1) | Unique identifier |
| CategoryName | nvarchar(200) | NOT NULL | Market sector name (e.g., "Technology") |
| CategoryDescription | nvarchar(500) | NULL | Brief sector description |
| ParentCategoryID | int | FK → Category(CategoryID), NULL | Self-reference for hierarchical sectors |
| IsActive | bit | NOT NULL, DEFAULT 1 | Enables/disables sector for new analyses |

**Entity: Tag**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| TagID | int | PK, Identity(1,1) | Unique identifier |
| TagName | nvarchar(50) | NOT NULL, UNIQUE | Ticker symbol, stored uppercase (e.g., "AAPL") |
| Note | nvarchar(500) | NULL | Optional metadata |

**Entity: NewsArticle**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| NewsArticleID | int | PK, Identity(1,1) | Unique identifier |
| NewsTitle | nvarchar(500) | NOT NULL | Auto-generated: "[BUY/SELL/HOLD] {Ticker} Automated Analysis" |
| Headline | nvarchar(1000) | NOT NULL | One-sentence AI summary |
| CreatedDate | datetime | NOT NULL | UTC timestamp of pipeline execution |
| NewsContent | nvarchar(MAX) | NOT NULL | Full structured AI output |
| NewsSource | nvarchar(500) | NULL | Source metadata (API + LLM model name) |
| CategoryID | int | FK → Category(CategoryID), NOT NULL | Associated market sector |
| NewsStatus | bit | NOT NULL, DEFAULT 1 | 1=Active (public), 0=Inactive (archived) |
| CreatedByID | int | FK → SystemAccount(AccountID), NULL (ON DELETE SET NULL) | Staff who triggered the pipeline |
| UpdatedByID | int | FK → SystemAccount(AccountID), NULL | Staff who last modified the record |
| ModifiedDate | datetime | NULL | UTC timestamp of last modification |

**Entity: NewsTag** (junction table)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| NewsArticleID | int | PK, FK → NewsArticle(NewsArticleID) | Report reference |
| TagID | int | PK, FK → Tag(TagID) | Ticker reference |

### Infrastructure
- **Hosting:** Local IIS Express (development only). No cloud deployment required for this academic version.
- **Solution Naming:**
  - Solution: `{StudentName}_{ClassCode}_A01.sln`
  - Web project: `{StudentName}MVC`
- **Configuration:** All secrets in `appsettings.json`. File must not be committed to any public repository.

---

## Analytics & Monitoring

**Key Metrics:**
- AI reports generated per day (total `NewsArticle` inserts).
- Pipeline error rate: failed runs / total triggered runs.
- Decision distribution: % BUY vs. % SELL vs. % HOLD.

**Events to Log (via `ILogger`):**
- Login success / failure: email attempted, timestamp.
- Pipeline triggered: Staff AccountID, TagName, CategoryName, timestamp.
- Pipeline success: NewsArticleID, total duration (ms).
- Pipeline failure: error type (NewsAPI timeout / LLM timeout / JSON parse error / DB error), timestamp.
- CRUD events: entity type, operation, actor AccountID, entity ID, timestamp.

**Dashboards:** Admin views the date-range statistical report (FR-10). No real-time streaming dashboard required.

**Alerting:** No automated alerting required. All pipeline errors are communicated to the triggering Staff member directly via UI message.

---

## Release Planning

### MVP (v1.0) — Semester Submission
**Features:** FR-1 (Auth), FR-2 (RBAC), FR-3 (AI Pipeline), FR-4 (Account CRUD), FR-5 (Category CRUD), FR-6 (Tag CRUD), FR-7 (Report Viewer), FR-10 (Admin Stats), FR-11 (Global UI Constraints)

**Timeline:** Per the academic submission deadline set by the lecturer.

**Success Criteria:** All acceptance criteria for v1.0 features pass during grading demo. Examiner confirms no 3-layer violations. All modal, AJAX, and validation behaviors work correctly.

### v1.1 — Post-Semester
- FR-8: Staff Report History with status toggling.
- FR-9: Profile Management (name and password change).

### v1.2 — Extended Features
- Export statistical report to Excel (.xlsx) or PDF.
- Pagination (10 items/page) on all list views.
- Search/filter on the public report list by ticker or sector.

### v2.0 — Advanced Features
- Real-time pipeline step indicator via SignalR.
- Batch ticker analysis using `Task.WhenAll`.
- Full pipeline audit log (Admin view).
- Email notification to Lecturers for new reports on followed tickers.

---

## Open Questions & Assumptions

All questions below were identified as important unresolved ambiguities in the original specification. Each is immediately followed by the assumption adopted for this PRD. These assumptions drive every implementation decision documented above.

---

### Authentication & Session Management

**Q1: How long should an authenticated session last before requiring re-login?**
> **Assumption A1:** Session uses **sliding expiration of 60 minutes**. Each authenticated request resets the timer. After 60 minutes of inactivity the session cookie expires, and the user is redirected to Login on their next request.

**Q2: Should the application support a "Remember Me" persistent login across browser restarts?**
> **Assumption A2:** No. All sessions are non-persistent — the session cookie is a browser-session cookie only. Closing the browser or 60 minutes of inactivity invalidates the session. "Remember Me" is deferred to v2.0.

**Q3: What happens when an active user's account is deleted by the Admin while they are logged in?**
> **Assumption A3:** Deletion takes effect immediately in the DB. On the deleted user's next request to a protected resource, the authorization check fails and they are redirected to Login. No real-time session invalidation mechanism is implemented in v1.0.

**Q4: Is a Logout feature required, and what HTTP verb should it use?**
> **Assumption A4:** Yes, Logout is required. It is implemented as a **POST** request to `/Account/Logout` (protected with `[ValidateAntiForgeryToken]`). Using GET for logout would allow CSRF-driven forced logout. On success, the session cookie is cleared and the user is redirected to Login.

---

### Account & Role Management

**Q5: Can an Admin create another Admin account (AccountRole = 3) through the UI?**
> **Assumption A5:** No. The Account Management form exposes only two role options: Staff (1) and Lecturer (2). The only Admin account is the one seeded at startup from `appsettings.json`. This prevents privilege escalation through the UI.

**Q6: Can an Admin update an existing account's role (e.g., promote Staff to Lecturer or vice versa)?**
> **Assumption A6:** Yes. The Update modal allows changing AccountRole between 1 and 2. Historical `NewsArticle` records linked to that account (via `CreatedByID`) are unaffected — no cascade update occurs.

**Q7: Can an Admin edit their own account name or password?**
> **Assumption A7:** Yes. The Admin's own row in the Account list includes the Edit button. Admin can update `AccountName` and `AccountPassword` via the Update modal. The Delete button for their own row is hidden.

**Q8: What are the minimum and maximum lengths for AccountName?**
> **Assumption A8:** `AccountName` must be between **2 and 100 characters**. Names shorter than 2 characters fail validation with: "Name must be at least 2 characters long."

**Q9: What is the minimum password length and are there complexity requirements?**
> **Assumption A9:** Minimum **8 characters**. No additional complexity rules (uppercase, special character, etc.) are enforced in v1.0, to keep the academic implementation simple.

---

### AI Pipeline Behavior

**Q10: Which specific News API provider and endpoint should be used?**
> **Assumption A10:** **NewsAPI.org**, endpoint `GET https://newsapi.org/v2/everything`. Query parameters: `q={TagName}`, `sortBy=publishedAt`, `pageSize=10`. API key stored in `appsettings.json` under `NewsApi:ApiKey`.

**Q11: Which LLM provider and model should be used?**
> **Assumption A11:** **OpenAI API**, model `gpt-4o`, endpoint `POST https://api.openai.com/v1/chat/completions`. Parameters: `temperature=0.3` (for deterministic output), `max_tokens=800`. API key stored in `appsettings.json` under `OpenAI:ApiKey`.

**Q12: What news content from each article is passed to the LLM?**
> **Assumption A12:** Only the `title` and `description` fields from each NewsAPI article are used (not the full article body). This limits token usage and avoids paywall-blocked content. Headlines are formatted as a numbered list in the prompt.

**Q13: Are duplicate pipeline runs for the same Ticker + Category allowed?**
> **Assumption A13:** Yes, duplicates are **intentionally allowed**. Each pipeline run creates a new, independent `NewsArticle` record. There is no uniqueness constraint on (TagID, CategoryID). Different runs at different times reflect changing market conditions, which is the intended behavior.

**Q14: What if the LLM returns JSON wrapped in markdown code fences (e.g., ` ```json ... ``` `) despite the prompt forbidding it?**
> **Assumption A14:** `TradingAgentService` applies a pre-processing step that strips any leading ` ```json ` or ` ``` ` wrappers from the response string before deserialization. If the cleaned string still fails to parse as valid JSON, the pipeline aborts with "Pipeline failed: AI response format error."

**Q15: What if the LLM returns a valid `decision` field but in lowercase (e.g., "buy" instead of "BUY")?**
> **Assumption A15:** The service applies `ToUpperInvariant()` to the `decision` field before validation. "buy", "Buy", and "BUY" all normalize to "BUY" and pass validation.

**Q16: Should the system retry failed LLM or News API calls automatically?**
> **Assumption A16:** No automatic retry in v1.0. Any failure immediately aborts the pipeline and displays a specific error message. Retry with exponential backoff is deferred to v1.2.

**Q17: Can multiple Staff members run the pipeline simultaneously without conflicts?**
> **Assumption A17:** Yes. `TradingAgentService` is a Singleton but `HttpClient` is thread-safe for concurrent requests. `DbContext` is Scoped, so each HTTP request has its own isolated DB context instance. No concurrency issues arise from simultaneous pipeline runs.

**Q18: Is the pipeline execution blocking the HTTP request thread while waiting for API responses?**
> **Assumption A18:** No. The pipeline is fully `async/await` — the controller `await`s the service method, which `await`s each `HttpClient.SendAsync()` call. The HTTP request thread is released to the thread pool during all I/O waits.

---

### Data Model & Business Rules

**Q19: What happens to `NewsArticle` records when the `Category` or `Tag` they reference is soft-deactivated (`IsActive = false`)?**
> **Assumption A19:** Setting `IsActive = false` on a Category does **not** affect existing `NewsArticle` records. Those reports remain visible if their own `NewsStatus = active`. Only the pipeline UI is affected: inactive Categories are excluded from the Sector dropdown on "Run Analysis" so they cannot be selected for new analyses.

**Q20: What happens to `NewsArticle` records when the Staff account that created them is deleted?**
> **Assumption A20:** `CreatedByID` is configured as `ON DELETE SET NULL` in the DB. After account deletion, `NewsArticle.CreatedByID` becomes null. The Admin Statistical Report displays "Deleted User" in the "Created By" column for such records. Reports themselves remain intact and publicly visible.

**Q21: What timezone should all timestamps use?**
> **Assumption A21:** All timestamps (`CreatedDate`, `ModifiedDate`) are stored and displayed in **UTC** using `DateTime.UtcNow`. All date/time values are displayed in the format `YYYY-MM-DD HH:mm UTC` throughout the UI. No timezone conversion is performed in v1.0.

**Q22: What is the maximum supported depth of the Category hierarchy?**
> **Assumption A22:** The DB schema supports unlimited depth technically. However, the UI enforces a **maximum of two levels**: top-level categories and one level of children. The ParentCategoryID dropdown in the Category modal shows only top-level categories (those with `ParentCategoryID = null`). A category that already has a parent cannot itself be selected as a parent for another category.

**Q23: Can `UpdatedByID`/`ModifiedDate` be updated by a Staff member other than the original creator?**
> **Assumption A23:** In v1.0, only the **original creator** can toggle `NewsStatus` on their own reports (via FR-8). Therefore, `UpdatedByID` will always equal `CreatedByID` in v1.0. Cross-staff editing is a v2.0 feature.

**Q24: Is there a one-to-one or one-to-many relationship between a `NewsArticle` and `Tag`?**
> **Assumption A24:** One-to-many via the `NewsTag` junction table — a single report can reference multiple tickers. However, in the v1.0 pipeline UI, Staff selects exactly **one Ticker per run**, so each generated report is linked to exactly one Tag record in `NewsTag`. Multi-ticker selection is deferred to v2.0.

---

### UI / UX Behavior

**Q25: Should the public report listing page support pagination?**
> **Assumption A25:** No pagination in v1.0. All active reports are loaded in a single query, sorted descending by `CreatedDate`. Pagination (10 items/page) is deferred to v1.2. This is acceptable for an academic context with a small dataset.

**Q26: Should the report listing page support search or filter (by ticker, sector, or decision type)?**
> **Assumption A26:** No search or filter in v1.0. All active reports are displayed. Search and filter are deferred to v1.2.

**Q27: What color codes are used for BUY / SELL / HOLD decision badges?**
> **Assumption A27:** Bootstrap 5 badge classes: `badge bg-success` (green) for BUY, `badge bg-danger` (red) for SELL, `badge bg-secondary` (gray) for HOLD. These colors apply consistently across the public report list, Staff Report History, and Admin Statistical Report.

**Q28: Should the Admin Statistical Report page show results on initial load, or only after form submission?**
> **Assumption A28:** The page loads **empty** — no results are shown until the Admin submits the date filter form. Both StartDate and EndDate fields start blank. No default date range is pre-populated.

**Q29: What is the default sort order and sort column for all list pages?**
> **Assumption A29:** All list pages (Account list, Category list, Tag list, Report list, Staff History, Admin Stats) default to **descending order by the entity's creation/primary timestamp**. For Account and Category/Tag lists (which have no timestamp), default sort is **ascending by ID**. No user-controlled sort toggling is required in v1.0.

**Q30: What feedback is shown to the user when an AJAX operation (modal save, IsActive toggle) is in progress?**
> **Assumption A30:** During any AJAX operation, the triggering button is disabled and shows a spinner icon. On success, the modal closes (if applicable) and the list updates. On failure, the button re-enables and an error message is shown inline (inside the modal) or as a toast notification (for toggle operations).

---

### Architecture & Code Quality

**Q31: Which specific classes must implement the Singleton pattern?**
> **Assumption A31:** The Singleton pattern is explicitly required for **`TradingAgentService`** and its underlying **`HttpClient`** instance. These are registered via `AddSingleton<ITradingAgentService, TradingAgentService>()` in `Program.cs`. All other services (`CategoryService`, `TagService`, `AccountService`, `NewsArticleService`) and all Repositories use **Scoped** lifetime.

**Q32: Should there be a separate DAO layer beneath the Repository, or does Repository = DAO?**
> **Assumption A32:** In this project, **Repository = DAO**. No additional DAO abstraction layer exists. Each entity has one corresponding Repository class and one interface (e.g., `INewsArticleRepository` / `NewsArticleRepository`). The term "Repository/DAO" in the original spec refers to the same layer.

**Q33: Should ViewModels (DTOs) be used between Controllers and Views, or can EF entity classes be passed directly?**
> **Assumption A33:** Dedicated **ViewModel classes** are used for all Controller ↔ View data transfer. EF entity classes are never directly bound to Views or exposed as action parameters in POST requests. This prevents over-posting and enforces a clean separation between the data model and the presentation layer.

**Q34: Is EF Core Code-First or Database-First required?**
> **Assumption A34:** **Code-First with EF Core Migrations** is used. Schema is created via `dotnet ef migrations add InitialCreate` and applied via `dotnet ef database update`. The `DbContext.OnModelCreating` seed method creates the Admin account on first run if none exists.

**Q35: Should the project include seed data beyond the Admin account (e.g., default Categories or Tags)?**
> **Assumption A35:** Only the **Admin account** is seeded at startup. No default Categories or Tags are seeded — Staff is responsible for creating these through the UI after login. This keeps the system flexible for different academic demonstration scenarios.

---

## Appendix

### Competitive Analysis
- **Bloomberg Terminal:** Strengths — extremely rich real-time data, deep professional analytics. Weaknesses — prohibitively expensive, no automated LLM pipeline, closed ecosystem. FNTS differentiates by providing an accessible, auto-generating AI pipeline for educational contexts.
- **Yahoo Finance (web):** Strengths — free, widely used, comprehensive news and price data. Weaknesses — no automated AI analysis layer, no internal role-based management system. FNTS adds a structured LLM decision layer and RBAC on top of similar news data.

### User Research Findings
- **Finding 1:** Staff members spend 30–60 minutes per ticker manually reading news and formulating opinions. The FNTS pipeline reduces this to under 1 minute by automating sentiment, fundamental, and portfolio synthesis.
- **Finding 2:** Users need the BUY/SELL/HOLD decision immediately visible in the report title and visually highlighted — they should not need to read the full report to understand the AI's conclusion. This drove the `NewsTitle` format `"[BUY] NVDA Automated Analysis"` and the color-coded badge design.

### AI Conversation Insights

**Identified Edge Cases:**
- LLM returns JSON with extra fields not in the schema → ignore extra fields; validate and map only the five required fields.
- News API returns headlines in a non-English language → LLM prompts do not specify a language; LLM handles multilingual input and responds in English by default.
- LLM returns `decision` in lowercase → normalized via `ToUpperInvariant()` before validation (covered in A15).
- Concurrent Staff pipeline runs → no race condition: `HttpClient` is thread-safe as Singleton; `DbContext` is Scoped per request (covered in A17).
- Deleting a parent Category with unreferenced children → children re-parented to null in a single transaction before parent deletion (covered in FR-5 AC-5).
- DB save fails after all 3 LLM calls succeed → full transaction rollback; no partial record persisted; Staff sees a DB error message (covered in FR-3 AC-10).

**AI-Suggested Improvements (deferred):**
- Add `ConfidenceScore` (0–100) to Portfolio Manager JSON output to indicate decision strength.
- Batch ticker analysis using `Task.WhenAll` for running multiple tickers in parallel (v2.0).
- Full pipeline execution audit log accessible to Admin (v2.0).
- Email notification to Lecturers when a new report for a ticker they follow is published (v2.0).

### Glossary

| Term | Definition |
|------|-----------|
| **BUY / SELL / HOLD** | The three possible trading decisions output by the Portfolio Manager agent. BUY = recommend purchasing; SELL = recommend selling; HOLD = maintain current position. |
| **Ticker (Tag)** | A stock market symbol identifying a publicly traded instrument (e.g., AAPL = Apple Inc., NVDA = NVIDIA). Stored in the `Tag` table. |
| **Market Sector (Category)** | A broad classification of an economic industry (e.g., Technology, Healthcare). Stored in the `Category` table. |
| **Pipeline** | The sequential automated workflow: Fetch News → Sentiment Agent → Fundamental Agent → Portfolio Manager → Auto-Save to DB. |
| **LLM (Large Language Model)** | A large-scale AI language model (e.g., OpenAI GPT-4o) used to process news and generate structured analysis and decisions. |
| **3-Layer Architecture** | A software pattern with three distinct layers: Presentation (Controller + View), Service (Business Logic), Repository (Data Access). Each layer communicates only with the adjacent layer below it. |
| **Singleton Pattern** | A design pattern ensuring a class has only one instance for the application's lifetime. Used for `TradingAgentService` and `HttpClient` to avoid socket exhaustion. |
| **RBAC (Role-Based Access Control)** | An authorization model where permissions are assigned to roles. FNTS uses four roles: Guest, Lecturer (2), Staff (1), Admin (3). |
| **ViewModel** | A class designed for data transfer between a Controller and a View, containing only the fields needed for that specific page — not the full DB entity. |
| **Modal Popup** | A Bootstrap dialog that overlays the current page for Create/Update forms without navigating away from the list. |
| **async/await** | C# keywords for asynchronous programming. Allows I/O-bound operations (HTTP calls, DB queries) to run without blocking the calling thread. |
| **Sliding Expiration** | A session timeout strategy where the timer resets on each user request, keeping active users logged in while expiring idle sessions. |
| **PBKDF2** | Password-Based Key Derivation Function 2 — a cryptographic hash algorithm used by ASP.NET Core's `IPasswordHasher<T>` to securely store passwords with a random salt. |
| **Code-First (EF Core)** | An EF Core workflow where the DB schema is generated from C# entity class definitions using migrations, rather than designing the DB first and scaffolding classes from it. |
| **ON DELETE SET NULL** | A referential integrity rule: when a referenced parent record is deleted, the FK column in child records is set to null instead of blocking the delete or cascading it. Used for `NewsArticle.CreatedByID`. |
