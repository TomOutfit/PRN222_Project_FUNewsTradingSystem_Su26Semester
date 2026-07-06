# FUNewsTradingSystem — Known Issues

This file records every defect, partial implementation, and intentional workaround discovered during development and self-testing. It is the single source of truth for known deviations from the spec.

> **Rule:** New issues must be logged here before they are discussed in stand-ups or bug triage. Resolve or move to a backlog item within 48 hours of discovery.

---

## How to Log an Issue

```
| ID   | Title | Severity | Owner | Status | Detail | Workaround |
|------|-------|----------|-------|--------|--------|------------|
| KI-# | …     | …        | …     | …      | …      | …          |
```

**Severity**
- **Critical** — Feature is broken for all users; blocks deployment.
- **Major** — Feature is broken for a subset of users or has significant data impact.
- **Minor** — Cosmetic or non-blocking UX issue.
- **Wishlist** — Not a bug; an improvement or stretch goal.

**Status**
- **Open** — Confirmed, not yet started.
- **In Progress** — Being actively worked on.
- **Deferred** — Acknowledged but deprioritised; links to backlog item.
- **Won't Fix** — Known and accepted; links to decision rationale.
- **Resolved** — Fixed and verified; links to commit/PR.

---

## 1. Critical Issues

*Issues that block deployment or cause data loss/corruption.*

| ID | Title | Severity | Owner | Status | Detail | Workaround |
|----|-------|----------|-------|--------|--------|------------|
| | | | | | | |

---

## 2. Major Issues

*Significant feature breakage or incorrect behaviour for a subset of users.*

| ID | Title | Severity | Owner | Status | Detail | Workaround |
|----|-------|----------|-------|--------|--------|------------|
| | | | | | | |

---

## 3. Minor Issues

*Cosmetic, non-blocking, or edge-case issues.*

| ID | Title | Severity | Owner | Status | Detail | Workaround |
|----|-------|----------|-------|--------|--------|------------|
| | | | | | | |

---

## 4. Partial / Intentionally Deferred Implementations

*Features that are stubbed, have known limitations, or were scoped down from the original spec.*

| ID | Title | Scope | Owner | Status | Notes |
|----|-------|-------|-------|--------|-------|
| | | | | | |

---

## 5. Third-party API Limitations & Workarounds

*Known constraints from external services (NewsAPI, OpenAI, LocalDB, etc.).*

| ID | Title | Provider | Owner | Status | Notes |
|----|-------|----------|-------|--------|-------|
| | | | | | |

---

## 6. Resolved Issues

*Issues that were discovered and fixed during development or testing.*

| ID | Title | Severity | Resolved By | Resolved On | Fix Summary |
|----|-------|----------|-------------|-------------|-------------|
| | | | | | |

---

*Last updated: [DATE] by [YOUR NAME]*
