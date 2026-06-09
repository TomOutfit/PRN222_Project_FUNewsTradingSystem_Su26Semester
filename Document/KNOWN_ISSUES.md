# FUNewsTradingSystem — Known Issues

> Honest list of any known limitations, partial implementations, or workarounds discovered during development and testing.

---

## Last Updated: [Date]

---

## API & External Dependencies

### 1. NewsAPI Free Tier Limitations
**Severity:** Medium
**Status:** Known limitation (out of scope for v1.0)

- NewsAPI free tier only returns articles from the last 30 days.
- Requests from certain cloud providers (e.g., AWS, some hosting platforms) may be rate-limited or blocked.
- **Workaround:** Use a paid NewsAPI plan for production deployments, or use a local development environment for testing.

---

### 2. OpenAI API Rate Limits
**Severity:** Low
**Status:** Known limitation

- OpenAI API may throttle requests if multiple pipeline runs are triggered in quick succession.
- **Workaround:** The AI Pipeline has a 10-second timeout per request. If a call times out, an error message is displayed to the user.

---

### 3. LLM Output Variability
**Severity:** Low
**Status:** By design

- The AI pipeline's decisions (BUY/SELL/HOLD) depend on the LLM's interpretation of fetched headlines.
- Outputs may vary between runs for the same ticker due to the probabilistic nature of LLMs.
- **Workaround:** The pipeline uses `temperature=0.3` for relatively deterministic output. Set lower temperature for more consistent results (trade-off: creativity vs. consistency).

---

## Implementation Notes

### 4. No Pagination on List Views
**Severity:** Low
**Status:** Deferred to v1.2

- All list views (Reports, Categories, Tags, Accounts) load all records in a single query.
- **Workaround:** None in v1.0. Pagination will be added in v1.2.

---

### 5. No Search/Filter on Public Report List
**Severity:** Low
**Status:** Deferred to v1.2

- The public report listing page does not support filtering by ticker, sector, or decision type.
- **Workaround:** None in v1.0. Search/filter will be added in v1.2.

---

### 6. Staff Cannot Edit Another Staff's Reports
**Severity:** Low
**Status:** By design (v1.0 scope)

- Only the original creator can toggle `NewsStatus` on their own reports.
- Cross-staff editing is not implemented in v1.0.
- **Workaround:** This is the intended behavior per PRD requirements.

---

### 7. No Real-time Session Invalidation
**Severity:** Low
**Status:** By design (v1.0 scope)

- If an Admin deletes a user's account while that user is logged in, the session remains active until the cookie expires or the user logs out.
- **Workaround:** Deleted users will be denied access on their next protected resource request.

---

### 8. Category Hierarchy Limited to 2 Levels
**Severity:** Low
**Status:** By design (per PRD Assumption A22)

- The UI enforces a maximum of 2 category levels (top-level + one level of children).
- ParentCategoryID dropdown shows only top-level categories.
- **Workaround:** Create a flat category structure or use deep hierarchies through direct DB manipulation (not recommended).

---

## Deployment Notes

### 9. appsettings.json Not Committed
**Severity:** N/A
**Status:** By design

- The `appsettings.json` file contains sensitive API keys and connection strings.
- It is added to `.gitignore` and must be configured manually for each deployment.
- **Workaround:** Use `appsettings.json.example` as a template and fill in real values.

---

### 10. Docker Deployment Requires Additional Configuration
**Severity:** Medium
**Status:** Known

- The included Dockerfile builds successfully but requires environment variables for database connection and API keys.
- **Workaround:** Set `ConnectionStrings__DefaultConnection`, `NewsApi__ApiKey`, and `OpenAI__ApiKey` as environment variables or in the cloud platform's dashboard.

---

## Testing Notes

### 11. External API Tests Require Valid API Keys
**Severity:** Medium
**Status:** Known

- Full end-to-end pipeline tests (FR-3) require valid NewsAPI and OpenAI API keys.
- Tests will fail if keys are missing, expired, or have insufficient quota.
- **Workaround:** Obtain valid API keys from NewsAPI.org and OpenAI before running pipeline tests.

---

### 12. DB Seed Depends on Admin Credentials in appsettings.json
**Severity:** Low
**Status:** By design

- The Admin account is seeded from `appsettings.json` at first run.
- If credentials in `appsettings.json` are changed after the initial seed, the Admin account retains the old password hash.
- **Workaround:** Drop the database and re-run `dotnet ef database update` to re-seed with new credentials.

---

## Resolved Issues

*(Move issues here once resolved)*

| Issue | Date Resolved | Resolution |
|-------|---------------|------------|
| - | - | - |

---

*This document should be updated before every major release or team sync.*
