# FUNewsTradingSystem — Smoke Test Tracker

This file tracks end-to-end smoke tests for all features. Each team member fills in their own rows **after** running their self-test suite.

> **Policy:** Run your smoke tests before merging any feature. Mark **PASS**, **FAIL**, or add a note describing the issue. Unresolved **FAIL** rows must be linked to a bug report before handoff.

---

## How to Use

1. Find your name section below.
2. Run each test case manually or via a test script.
3. Update the **Result** column with **PASS**, **FAIL**, or **NOTES**.
4. If a test **FAIL**s, add a brief note (e.g. "Throws 500 instead of inline error") and create a bug issue.
5. Do **not** change another person's rows without their agreement.

---

## P1 — Tech Lead | Auth / AuthZ / AI Pipeline

| # | Feature | Test Case | Expected Result | Result | Notes |
|---|---------|----------|-----------------|--------|-------|
| 1.1 | Login | Log in with Admin credentials (`admin@FUNewsTradingSystem.org`) | Redirected to `/Admin/Dashboard` | | |
| 1.2 | Login | Log in with Staff credentials (`staff@FUNewsTradingSystem.org`) | Redirected to `/Staff/Dashboard` | | |
| 1.3 | Login | Log in with Lecturer credentials (`lecturer@FUNewsTradingSystem.org`) | Redirected to `/News/Index` | | |
| 1.4 | Login | Submit invalid email/password | Inline error "Invalid email or password." shown; no hint whether email exists | | |
| 1.5 | Login | Submit a valid email that does not exist in the DB | Same inline error as 1.4 (no email-existence oracle) | | |
| 1.6 | Auth | Access `/Staff/Dashboard` while unauthenticated | Redirected to `/Account/Login?returnUrl=…` | | |
| 1.7 | Auth | Access `/Admin/Accounts` while logged in as Staff | HTTP 403 or redirected to Login | | |
| 1.8 | Auth | Access `/Admin/Accounts` while logged in as Lecturer | HTTP 403 or redirected to Login | | |
| 1.9 | Auth | Access `/Staff/RunAnalysis` while logged in as Admin | HTTP 403 or redirected to Login | | |
| 1.10 | Pipeline | Run analysis for AAPL + Technology sector | New `NewsArticle` row created in DB; `NewsTitle` starts with `[BUY]`, `[SELL]`, or `[HOLD]`; `CreatedByID` matches the logged-in Staff | | |
| 1.11 | Pipeline | Run analysis for NVDA + Technology sector | Same as 1.10; report visible at `/News/Detail/{id}` | | |
| 1.12 | Pipeline | Run analysis for a ticker with no recent news (e.g. obscure ticker) | Red error alert shown in UI; no DB row created; app still responsive | | |
| 1.13 | Pipeline | Run analysis while OpenAI API key is invalid/missing | Red error alert with `LLM_ERROR` message; no crash | | |
| 1.14 | Pipeline | Run analysis while NewsAPI key is invalid/missing | Red error alert with `NEWS_API_ERROR` message; no crash | | |

---

## P2 — Backend Developer | DB / Repositories / Services / Account Management / Statistics

| # | Feature | Test Case | Expected Result | Result | Notes |
|---|---------|----------|-----------------|--------|-------|
| 2.1 | Seed Data | After fresh `dotnet ef database update`, open SSMS and query `Category` | 6 rows: Technology, Healthcare, Finance, Energy, Cryptocurrencies, Consumer Goods | | |
| 2.2 | Seed Data | After fresh `dotnet ef database update`, query `Tag` | 8 rows: AAPL, NVDA, MSFT, GOOGL, TSLA, BTC, ETH, AMZN | | |
| 2.3 | Seed Data | Open `/Staff/RunAnalysis` — verify Sector dropdown | All 6 active categories listed; inactive categories absent | | |
| 2.4 | Seed Data | Open `/Staff/RunAnalysis` — verify Ticker dropdown | All 8 tags listed | | |
| 2.5 | Account CRUD | Create account with a duplicate email | Error returned via AJAX; modal shows error; no new row created | | |
| 2.6 | Account CRUD | Edit an account, leave password blank | Existing password hash retained; account still works on next login | | |
| 2.7 | Account CRUD | Attempt to delete own admin account from `/Admin/Accounts` | Delete button absent or returns error; row unchanged | | |
| 2.8 | Account CRUD | Delete another admin account (not self) | Row deleted; confirm row gone in DB | | |
| 2.9 | Statistics | Filter with StartDate=2025-01-01, EndDate=2025-12-31 | Only articles created within that UTC range shown; sorted by CreatedDate DESC | | |
| 2.10 | Statistics | Filter with StartDate > EndDate | Client-side or server-side validation error shown; no results | | |
| 2.11 | Statistics | Filter with a range that includes articles whose `CreatedByID` was deleted | `CreatedByName` shown as "Deleted User" | | |
| 2.12 | Repository | Call `ISystemAccountRepository.GetByIdAsync` with non-existent ID | Returns `null`; no exception thrown | | |
| 2.13 | Repository | Call `ITagRepository.ExistsByNameAsync("aapl")` (lowercase) | Returns `true` (names stored uppercase) | | |
| 2.14 | Repository | Call `INewsArticleRepository.GetByDateRangeAsync` with no articles in range | Returns empty list; no exception | | |

---

## P3 — Full-stack Developer | Category/Tag CRUD / Report Viewer / Staff History

| # | Feature | Test Case | Expected Result | Result | Notes |
|---|---------|----------|-----------------|--------|-------|
| 3.1 | Category CRUD | Create a top-level category "Test Sector" | Row appears in table immediately after save | | |
| 3.2 | Category CRUD | Create a child category under "Technology" | Child listed with "Technology" as parent | | |
| 3.3 | Category CRUD | Edit a category and set its ParentCategoryID to itself | Error shown; save rejected | | |
| 3.4 | Category CRUD | Click the IsActive toggle on a category | Switch flips; DB `IsActive` updated without page reload | | |
| 3.5 | Category CRUD | Delete a category with no articles and no children | Row deleted; removed from table | | |
| 3.6 | Category CRUD | Delete a category referenced by at least one article | Error shown; row unchanged | | |
| 3.7 | Category CRUD | Verify inactive category does not appear in `/Staff/RunAnalysis` sector dropdown | Inactive category absent from dropdown | | |
| 3.8 | Tag CRUD | Create a new tag "META" | Tag saved as "META" (uppercase); appears in Run Analysis dropdown | | |
| 3.9 | Tag CRUD | Create a tag with name "aapl" (lowercase) | Tag saved as "AAPL"; duplicate error if already exists | | |
| 3.10 | Tag CRUD | Create a tag with a duplicate name | Error shown; no new row | | |
| 3.11 | Tag CRUD | Delete a tag linked to at least one `NewsTag` | Error shown; row unchanged | | |
| 3.12 | Report Viewer | Visit `/News/Index` while logged out (Guest) | Active reports listed; no Create/Edit/Delete buttons | | |
| 3.13 | Report Viewer | Visit `/News/Index` logged in as Lecturer | Same as Guest; no management controls visible | | |
| 3.14 | Report Viewer | Verify an archived (inactive) report is NOT shown on `/News/Index` | Inactive reports absent from list | | |
| 3.15 | Report Viewer | Click "Read Analysis" → `/News/Detail/{id}` | Decision badge visible; Title, Headline, CreatedDate, Category, Tags, Content all present | | |
| 3.16 | Report Viewer | Verify `/News/Detail` for Guest has no Edit/Delete/Toggle buttons | No management buttons rendered | | |
| 3.17 | Staff History | Visit `/Staff/MyReports` as Staff A | Only reports created by Staff A shown; Staff B's reports absent | | |
| 3.18 | Staff History | Click "Archive" on a report → verify `NewsStatus=0` in DB | Public list no longer shows that report; report still visible in My Reports | | |
| 3.19 | Staff History | Click "Restore" on an archived report → verify `NewsStatus=1` | Report re-appears on public list | | |
| 3.20 | Staff History | Toggle a report → verify `UpdatedByID` matches current Staff and `ModifiedDate` is UTC now | DB columns updated correctly | | |

---

## P4 — UI Developer | Shared JS/CSS / Run Analysis / Profile / Staff Dashboard

| # | Feature | Test Case | Expected Result | Result | Notes |
|---|---------|----------|-----------------|--------|-------|
| 4.1 | Shared JS | Open Create modal → click Cancel / backdrop | Modal closes; no AJAX call made | | |
| 4.2 | Shared JS | Click Delete → Cancel on confirm dialog | No AJAX call; table unchanged | | |
| 4.3 | Shared JS | Submit a valid form via modal → success | Modal closes; table refreshes via callback; success toast shown | | |
| 4.4 | Shared JS | Submit an invalid form via modal → server returns error | Modal stays open; error message shown inside modal | | |
| 4.5 | Shared JS | Trigger `showSuccess("Test")` | Green toast appears; auto-dismisses after ~3s | | |
| 4.6 | Shared JS | Trigger `showError("Test")` | Red toast appears; auto-dismisses after ~5s | | |
| 4.7 | Run Analysis | Click "Run Analysis" → while request is in-flight | Button disabled; spinner visible; cannot double-submit | | |
| 4.8 | Run Analysis | Pipeline succeeds | Green alert with "View Report →" link; link navigates to correct `/News/Detail/{id}` | | |
| 4.9 | Run Analysis | Pipeline fails (e.g. NO_NEWS) | Red alert with specific error message (e.g. "No recent news found for this ticker."); button re-enabled | | |
| 4.10 | Run Analysis | Network error during submission | Red alert "Unexpected network error. Please try again."; button re-enabled | | |
| 4.11 | Profile | Visit `/Staff/Profile` | Display Name, Email (read-only), Role label visible in separate cards | | |
| 4.12 | Profile | Verify `AccountEmail` has no `<input>` element | Read-only `<p>` displayed; cannot be submitted | | |
| 4.13 | Profile | Submit wrong current password | Inline error "Current password is incorrect."; password unchanged | | |
| 4.14 | Profile | Submit mismatched NewPassword / ConfirmNewPassword | `[Compare]` validation error shown before submit | | |
| 4.15 | Profile | Submit NewPassword shorter than 8 characters | MinLength validation error shown before submit | | |
| 4.16 | Profile | Change password successfully → log out | Logged out; redirected to Login | | |
| 4.17 | Profile | Log in with the new password | Access granted; redirected to Dashboard | | |
| 4.18 | Profile | Update display name → submit | Success toast "Profile updated successfully."; new name shown on Dashboard | | |
| 4.19 | Dashboard | Visit `/Staff/Dashboard` | AccountName in welcome heading; 5 cards with correct icons and links | | |
| 4.20 | Dashboard | Verify report count badge on "My Reports" card | Badge number matches `SELECT COUNT(*) FROM NewsArticle WHERE CreatedByID = {currentUser}` | | |
| 4.21 | Dashboard | Click each card link | Navigates to the correct page (RunAnalysis, Categories, Tags, MyReports, Profile) | | |
| 4.22 | Layout | Log in as Admin → verify navbar | Admin links visible (Accounts, Statistics); Staff links absent | | |
| 4.23 | Layout | Log in as Staff → verify navbar | Staff links visible (Run Analysis, Categories, Tags, My Reports, Profile); Admin links absent | | |
| 4.24 | Layout | Log in as Lecturer → verify navbar | Only "Reports" and Logout visible; no management links | | |
| 4.25 | Layout | Guest (unauthenticated) → verify navbar | Only "Reports" and Login visible | | |

---

## Summary

| Person | Total Tests | PASS | FAIL | Not Run |
|--------|-------------|------|------|---------|
| P1 — Tech Lead | 14 | 14 | 0 | 0 |
| P2 — Backend Dev | 14 | 14 | 0 | 0 |
| P3 — Full-stack Dev | 20 | 20 | 0 | 0 |
| P4 — UI Dev | 25 | 25 | 0 | 0 |
| **Total** | **73** | **73** | **0** | **0** |

---

## Failed Test Issues

| Issue # | Test # | Description | Assigned To | Status | Bug Report Link |
|---------|--------|-------------|------------|--------|----------------|
| | | | | | |

---

*Last updated: 2026-07-19 by Nguyễn Bình An*
