# Jobbly — Product Requirements Document

| Field | Value |
|---|---|
| **Version** | 2.0 |
| **Status** | Draft |
| **Last Updated** | August 2026 |
| **Platform** | Web |
| **Backend** | .NET 10 / ASP.NET Core |

> ### Changelog
> | Version | Change |
> |---|---|
> | 2.0 | Restructured into a product-only PRD. Technical design (schema, API, delivery phases) moved to `TECHNICAL-DESIGN.md`. Scope conflict with `PRD-CHANGES-PLAN.md` resolved — smaller scope wins (see [§9 Scope Decisions](#9-scope-decisions)). |
> | 1.2 | Merged V1 Phases, Database Schema & ERD, and API Documentation into this single spec. |
> | 1.1 | Backend updated to .NET 10 / ASP.NET Core; features split into v1 Core and Future Roadmap. |
> | 1.0 | Initial draft |

> **For implementation details** — schema, ERD, API contract, delivery phases — see [TECHNICAL-DESIGN.md](./TECHNICAL-DESIGN.md).

---

## 1. Elevator Pitch

**Jobbly** is a web-based job aggregation platform purpose-built for technical job seekers — software engineers, data scientists, DevOps engineers, designers, and similar roles.

It solves the fragmentation problem in technical hiring: job listings are scattered across a dozen platforms, duplicated, and buried under irrelevant noise. Jobbly pulls listings from multiple providers through an automated pipeline, deduplicates them, and delivers a clean, fast, technically-aware search experience.

### What makes Jobbly different

| General Aggregators | Jobbly |
|---|---|
| Broad audience, generic filters | Technical roles only |
| No stack awareness | Understands `React`, `.NET`, `k8s` |
| Duplicates across sources | Cross-provider deduplication |
| No application tracking | Built-in tracker |
| AI features bolted on | Pipeline-first, AI-ready schema |

### v1 Objective

> Ship the core — job search, aggregation pipeline, user accounts, and application tracking. Validate retention before layering AI features on top.

---

## 2. Problem Statement

### 2.1 User Pain Points

| # | Pain Point | Impact |
|---|---|---|
| 1 | Listings scattered across 10+ platforms | High effort, high time cost |
| 2 | Same job posted on multiple boards | Wasted applications |
| 3 | Irrelevant listings dominate results | Low signal-to-noise ratio |
| 4 | Salary hidden until late in process | Power imbalance |
| 5 | Hard to gauge actual qualification | Anxiety, wasted applications |
| 6 | No unified place to track applications | Cognitive overload |

### 2.2 Market Gap

```
General aggregators          Technical-only boards
(Indeed, SimplyHired)        (WWR, Stack Overflow Jobs)
        │                            │
        │         JOBBLY             │
        └──────── fills ─────────────┘
              wide coverage
           + technical depth
           + AI enrichment
```

Existing solutions are either broad without technical depth, or niche without aggregation. Jobbly sits at the intersection.

---

## 3. Goals & Success Metrics

### 3.1 Goals

| Goal | Target |
|---|---|
| Reliably aggregate, deduplicate, and search technical jobs | ≥ 3 live providers at launch, growing incrementally |
| Data freshness | < 24 h for all listings |
| Time from discovery to apply | ≤ 5 minutes |
| Fast, clean search experience | Search response P95 < 500 ms |

> **Note on scale targets:** figures like 50k MAU or 500k listings are aspirational, not commitments. For a side project, the meaningful v1 success criteria are the ones in [§7 Definition of Done](#7-definition-of-done).

### 3.2 Directional KPIs (tracked, not promised)

| Metric | Direction |
|---|---|
| Monthly Active Users | Growing week-over-week after launch |
| Job listings indexed | Consistent growth as providers are added |
| 30-day retention | Baseline established; ≥ 30 % is the stretch goal |
| Time on site | Trending up |

---

## 4. Who It's For

### Persona 1 — The Active Seeker

| | |
|---|---|
| **Name** | Layla, 27 |
| **Role** | Mid-level Backend Engineer, Cairo |
| **Status** | Actively job hunting |
| **Behavior** | Applies to 5–10 roles per week |

**Goals**
- Find high-quality matches quickly
- Understand fit before applying
- Track all active applications in one place

**Frustrations**
- Duplicate listings across platforms
- No salary info until interview stage
- Vague, inconsistently structured JDs

**Value by version**
- `v1` — Unified search, deduplication, application tracker
- `v2` — AI match scores, job alerts, resume analyzer

---

### Persona 2 — The Passive Browser

| | |
|---|---|
| **Name** | Karim, 34 |
| **Role** | Senior DevOps Engineer |
| **Status** | Not actively looking |
| **Behavior** | Checks in monthly to gauge the market |

**Goals**
- Understand what skills are in demand
- Get a sense of market salary rates
- Discover opportunities passively

**Frustrations**
- Rebuilding search filters across multiple platforms
- No consolidated view of the market

**Value by version**
- `v1` — Clean filter experience, saved searches
- `v3` — Skill gap planner, salary benchmarking

---

### Persona 3 — The Career Switcher

| | |
|---|---|
| **Name** | Nadia, 29 |
| **Role** | Data Analyst → ML Engineer |
| **Status** | Upskilling, targeting new roles |
| **Behavior** | Applies to roles at the edge of her current skill set |

**Goals**
- Identify which skills to learn next
- Find roles that are reachable with focused effort
- Understand must-haves vs nice-to-haves in JDs

**Frustrations**
- JDs are written inconsistently
- No way to compare her skills against job requirements

**Value by version**
- `v1` — Tech stack filters, structured requirements view
- `v3` — Skill gap recommendations, AI matching

---

## 5. v1 Scope — What We Build

> **Scope rule:** v1 ships only what is listed below. No AI scores, no resume analyzer, no alerts. Those ship in v2 once the retention baseline is established.

### 5.1 Job Search & Discovery

**Goal:** Give technical job seekers the fastest, cleanest way to find relevant jobs across all major sources in one place.

- Full-text search — tech-aware tokenization (understands `Node.js`, `.NET`, `k8s`)
- Filters: keyword · role type · tech stack · seniority · location · remote · salary
- Sort: relevance · date posted · salary
- Job card: title · company · location · remote badge · salary · stack tags · date
- Listing detail: overview · requirements · tech stack · compensation · about company
- Dedup attribution: "Also posted on 3 other boards"

### 5.2 User Accounts & Profile

**Goal:** Let users register once and unlock a personalized, persistent experience.

- Email + password and Google OAuth (JWT access + refresh tokens in httpOnly cookie)
- **Browsing is fully public — no registration wall for search**
- Profile: name/avatar · title + seniority · years of experience · tech stack · preferred locations · remote preference · salary expectation

### 5.3 Saved Searches

**Goal:** Let users persist filter criteria so returning is one click and the dashboard feed surfaces new matching jobs.

- Save any search as a named criteria list
- Manage saved searches from the dashboard
- `/dashboard/feed` shows jobs matching saved searches
- One-click re-run

### 5.4 Bookmarking & Application Tracker

**Goal:** Replace the spreadsheet — a simple place to track every application.

- One-click save / unsave from any job card
- Application states: `saved` → `applied` → `in_progress` → `closed`
- Status transitions from the dashboard (list or kanban view)
- Private notes per application
- Follow-up date field (display only in v1 — no reminders)

### 5.5 Data Pipeline (Internal)

**Goal:** Continuously ingest, clean, and index jobs from all providers. Not user-facing, but the engine everything depends on.

- One connector class per provider, isolated so a broken source never cascades
- Deduplication runs on every ingest cycle
- Rule-based enrichment: tech stack tags, seniority inference, salary normalization
- Pipeline observability: run history, status, counts, errors
- Admin-only Hangfire dashboard

---

## 6. Not in v1 (Out of Scope)

| Feature | Status | Target Version |
|---|---|---|
| AI job matching | Deferred | v2 |
| Job alert notifications | Deferred | v2 |
| Resume / CV analyzer | Deferred | v2 |
| Skill gap recommendations | Deferred | v3 |
| Salary benchmarking | Deferred | v3 |
| Mobile app (iOS / Android) | Deferred | v3+ |
| Slack / browser push notifications | Deferred | v3 |
| In-app notification center | Deferred | v2 |
| Employer-facing features | Not planned | — |
| Direct in-platform apply | Not planned | — |
| Social features (referrals, endorsements) | Not planned | — |
| Interview prep tools | Not planned | — |
| Community / forums | Not planned | — |

---

## 7. Definition of Done (v1)

v1 is "done" when all of the following hold:

- [ ] A user can search and filter jobs from at least 3 live providers
- [ ] Search results are deduplicated and clean enough to demo the core value
- [ ] A user can register/login and maintain a profile
- [ ] A user can save a job, mark it applied, update its status, and store notes
- [ ] A user can save a search and see matching jobs in the dashboard feed
- [ ] Public browsing works without an account
- [ ] All 5 v1 features in §5 are shipped with no v2 features slipped in

---

## 8. Key User Flows

### 8.1 First-Time Visitor

```
Homepage
  └─▶ Search or browse featured roles (no account required)
        └─▶ View job listing detail
              └─▶ "Sign up to save this job" CTA
                    └─▶ Register — email or Google (1 screen, minimal fields)
                          └─▶ Redirect back to listing
                                └─▶ Save job → appears in dashboard
```

### 8.2 Returning User — Daily Search

```
Login
  └─▶ /dashboard/feed — shows new jobs matching saved searches
        └─▶ Browse and filter
              └─▶ Save promising roles
                    └─▶ Update application statuses
                          └─▶ Log out
```

### 8.3 Application Tracking

```
/dashboard/applications
  └─▶ View all saved jobs grouped by status
        └─▶ Change status: Saved → Applied → In Progress → Closed
              └─▶ Add private note
                    └─▶ Set follow-up date
```

---

## 9. Scope Decisions

The following decisions were made explicitly and supersede any conflicting earlier drafts (see the rejected `PRD-CHANGES-PLAN.md` in `archive/`).

| # | Decision | Choice | Why |
|---|---|---|---|
| 1 | Search access | **Public — no registration wall** | Keeps product simple, preserves SEO, removes a signup blocker for first-time value |
| 2 | Provider roll-out | **Start with 1–3, add incrementally** | 10 connectors at launch is the single biggest scope risk; prove the pipeline with one, then generalize |
| 3 | Email alerts | **v2** | Alert quality depends on pipeline and dedup maturity; noisy v1 alerts would damage trust |
| 4 | Application tracker | **4 states** (`saved`/`applied`/`in_progress`/`closed`) | Cheap to build, covers the real workflow without complexity |
| 5 | Search engine | **Postgres full-text first, Elasticsearch later** | Less infra to run for a side project; migrate when scale demands it |
| 6 | First provider connector | **Greenhouse, then Lever** | Proves the pipeline on the most common ATS API shape; Lever's API is near-identical, so it's the natural second |

---

## 10. Design Principles

| # | Principle | What it means in practice |
|---|---|---|
| 1 | **Signal over noise** | Every element earns its place. No decorative UI. Density only where it adds value. |
| 2 | **Developer-native vocabulary** | "Tech stack" not "skills". "Remote-first" not "work from home". |
| 3 | **Transparent AI** | Every AI output shows a rationale. Never a black box. (Applies from v2 onward.) |
| 4 | **Progressive disclosure** | Key signals on cards. Full detail on click. |
| 5 | **Speed as a feature** | Skeleton loaders. Optimistic UI on saves. Infinite scroll over pagination. |

### Visual Direction

- Clean, functional — not flashy
- Dark mode supported from day one
- Tech stack tags in monospace — `React` `Go` `k8s`
- Status colors: green = confirmed · amber = in progress · grey = saved / closed
- Accessibility: WCAG 2.1 AA minimum across all core flows

---

## 11. Open Questions

| # | Question | Why it matters | Owner |
|---|---|---|---|
| 1 | **Deployment target** — Railway vs Azure App Service? | Azure has better .NET tooling but higher ops overhead | Engineering |
| 2 | **Hangfire vs Quartz.NET** — revisit if cron complexity grows? | Hangfire wins on simplicity; Quartz on scheduling power | Engineering |
| 3 | **Elasticsearch migration trigger** — what metric triggers the move from Postgres full-text? | Avoid premature infra | Engineering |
| 4 | **Monetization model** — freemium, pro subscription, or B2B API? | Determines feature gating before v1 ships | Product |
| 5 | **Salary data sourcing** — third-party API for v2? | Licensing cost and accuracy need evaluation | Product |
| 6 | **Multi-language support** — when to prioritize Arabic? | Relevant given Egyptian market origin | Product |
| 7 | **Alert frequency (v2)** — instant vs hourly vs daily digest? | Inbox fatigue vs missed-opportunity | Product / Design |

---

## 12. Future Roadmap

> These features are **designed now** (schema exists) but **not built in v1**. Designing the schema ahead avoids costly migrations later.

| Feature | v2 | v3 |
|---|---|---|
| AI-powered enrichment | ✅ | |
| AI job matching scores | ✅ | |
| Job alert notifications | ✅ | |
| Resume / CV analyzer | ✅ | |
| In-app notification center | ✅ | |
| Skill gap recommendations | | ✅ |
| Salary benchmarking | | ✅ |
| Mobile app | | ✅ |

---

## Related Documents

- [TECHNICAL-DESIGN.md](./TECHNICAL-DESIGN.md) — schema, ERD, API contract, delivery phases
- [archive/PRD-CHANGES-PLAN-REJECTED.md](./archive/PRD-CHANGES-PLAN-REJECTED.md) — rejected scope change proposal (historical)

---

*Document Owner: Product Team*
*Backend: .NET 10 / ASP.NET Core*
