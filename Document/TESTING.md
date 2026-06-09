# FUNewsTradingSystem — Smoke Test Tracker

> Each team member fills in their own rows after self-testing their features.
> Update PASS/FAIL/NOTES columns as testing progresses.

---

## How to Use

1. Run each test case manually or via automated tests.
2. Mark PASS, FAIL, or add notes.
3. Update this file before any merge or team sync.

---

## Test Cases

| Feature | Test Case | Owner | Status | Notes |
|---------|-----------|-------|--------|-------|
| **FR-1: Authentication** | Login with correct Admin credentials → redirect to Admin Dashboard | P1 | | |
| **FR-1: Authentication** | Login with correct Staff credentials → redirect to Staff Dashboard | P1 | | |
| **FR-1: Authentication** | Login with correct Lecturer credentials → redirect to Report List | P1 | | |
| **FR-1: Authentication** | Login with wrong password → inline error "Invalid email or password." | P1 | | |
| **FR-1: Authentication** | Login with non-existent email → inline error (no email existence hint) | P1 | | |
| **FR-1: Authentication** | Click Logout → session cleared, redirected to Login | P1 | | |
| **FR-1: Authentication** | Access protected route without login → redirect to Login | P1 | | |
| **FR-2: Authorization** | Guest accessing `/Staff/Categories` → 403 or Login redirect | P1 | | |
| **FR-2: Authorization** | Staff accessing `/Admin/Accounts` → 403 or Login redirect | P1 | | |
| **FR-2: Authorization** | Admin accessing `/Staff/RunAnalysis` → 403 or Login redirect | P1 | | |
| **FR-3: AI Pipeline** | Run pipeline end-to-end → new NewsArticle row in DB | P1 | | |
| **FR-3: AI Pipeline** | Run pipeline → NewsTitle format is "[BUY/SELL/HOLD] {TagName} Automated Analysis" | P1 | | |
| **FR-3: AI Pipeline** | Run pipeline with no news → error message displayed | P1 | | |
| **FR-3: AI Pipeline** | Run pipeline → CreatedByID = current Staff AccountID | P1 | | |
| **FR-4: Account Management** | Admin creates account with duplicate email → error inline | P2 | | |
| **FR-4: Account Management** | Admin edits account with blank password → existing password retained | P2 | | |
| **FR-4: Account Management** | Admin deletes own account → blocked (no Delete button or error) | P2 | | |
| **FR-4: Account Management** | Admin creates account → appears in list immediately | P2 | | |
| **FR-4: Account Management** | Create form: Role dropdown only shows Staff (1) and Lecturer (2) | P2 | | |
| **FR-5: Category Management** | Create top-level category → appears in list | P3 | | |
| **FR-5: Category Management** | Create child category → Parent displayed correctly | P3 | | |
| **FR-5: Category Management** | Set category as its own parent → blocked | P3 | | |
| **FR-5: Category Management** | Toggle IsActive → UI updates instantly | P3 | | |
| **FR-5: Category Management** | Delete unreferenced category → succeeds | P3 | | |
| **FR-5: Category Management** | Delete category linked to articles → error "Cannot delete: this sector is linked to existing reports." | P3 | | |
| **FR-5: Category Management** | Delete category with children (no linked articles) → children re-parented, parent deleted | P3 | | |
| **FR-5: Category Management** | Inactive category excluded from pipeline dropdown | P3 | | |
| **FR-6: Tag Management** | Create tag → stored uppercase, appears in list | P3 | | |
| **FR-6: Tag Management** | Create tag with duplicate name → error inline | P3 | | |
| **FR-6: Tag Management** | Delete tag linked to reports → error "Cannot delete: this ticker is linked to existing reports." | P3 | | |
| **FR-6: Tag Management** | Delete unreferenced tag → succeeds | P3 | | |
| **FR-7: Report Viewer** | Guest accesses `/Report/Index` → sees active reports | P3 | | |
| **FR-7: Report Viewer** | Inactive reports NOT visible on public list | P3 | | |
| **FR-7: Report Viewer** | Report list sorted descending by CreatedDate | P3 | | |
| **FR-7: Report Viewer** | Decision badges: BUY=green, SELL=red, HOLD=gray | P3 | | |
| **FR-7: Report Viewer** | Detail page has all fields (Title, Headline, Content, Source, etc.) | P3 | | |
| **FR-7: Report Viewer** | Guest/Lecturer sees NO management buttons | P3 | | |
| **FR-8: Staff Report History** | Staff A's history shows ONLY Staff A's reports | P3 | | |
| **FR-8: Staff Report History** | Toggle report to Inactive → removed from public list | P3 | | |
| **FR-8: Staff Report History** | Toggle updates UpdatedByID + ModifiedDate in DB | P3 | | |
| **FR-9: Profile Management** | AccountEmail and AccountRole have no input fields | P4 | | |
| **FR-9: Profile Management** | Wrong current password → "Current password is incorrect." | P4 | | |
| **FR-9: Profile Management** | Mismatched new passwords → Compare validation error | P4 | | |
| **FR-9: Profile Management** | New password < 8 chars → MinLength validation error | P4 | | |
| **FR-9: Profile Management** | Successful password change → log out → log in with new password | P4 | | |
| **FR-10: Admin Statistics** | Page loads empty (no results initially) | P2 | | |
| **FR-10: Admin Statistics** | StartDate > EndDate → client-side error | P2 | | |
| **FR-10: Admin Statistics** | Filter by date range → correct results | P2 | | |
| **FR-10: Admin Statistics** | Results sorted descending by CreatedDate | P2 | | |
| **FR-10: Admin Statistics** | Null CreatedByID → "Deleted User" displayed | P2 | | |
| **FR-10: Admin Statistics** | Empty result → "No reports found for the selected period." | P2 | | |
| **FR-11: Global UI** | All Create/Update via modal — no standalone pages | P3/P4 | | |
| **FR-11: Global UI** | All Delete requires confirmation dialog | P3/P4 | | |
| **FR-11: Global UI** | Client-side validation runs before AJAX submit | P3/P4 | | |
| **FR-11: Global UI** | Date inputs use HTML5 date picker | P2/P3 | | |
| **FR-11: Global UI** | All forms have AntiForgeryToken | P1 | | |
| **Shared JS/CSS** | Modal open/close works correctly | P4 | | |
| **Shared JS/CSS** | Toast success/error display correctly | P4 | | |
| **Shared JS/CSS** | Decision badges colored correctly across all pages | P4 | | |
| **Run Analysis UI** | Button disabled during pipeline execution | P4 | | |
| **Run Analysis UI** | Success shows green alert + link to report | P4 | | |
| **Run Analysis UI** | Error shows red alert with specific message | P4 | | |
| **Staff Dashboard** | Report count badge matches DB row count | P4 | | |
| **Staff Dashboard** | All 5 card links navigate to correct pages | P4 | | |

---

## Summary

| Owner | Total Tests | PASS | FAIL | Notes |
|-------|------------|------|------|-------|
| P1 | ~10 | | | |
| P2 | ~10 | | | |
| P3 | ~20 | | | |
| P4 | ~12 | | | |
| **Total** | **~52** | | | |

---

*Last Updated: [Date]*
