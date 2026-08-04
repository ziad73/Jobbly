# PRD v1 Scope Changes — Implementation Plan

> ## ARCHIVED — REJECTED
>
> **Date:** August 2026
>
> **Status:** Considered and rejected in favor of smaller scope.
>
> **Reason:** The proposed changes (auth-gated search, all 10 providers at launch, email alerts in v1) would make Phase 1 the critical path and significantly inflate scope for a weekend-side-project context. The team chose to keep the original smaller scope: public search, incremental provider roll-out, alerts in v2, the 4-state application tracker, and Postgres full-text search first.
>
> This document is kept for historical reference only. **Do not use it as the source of truth** — see `PRD.md` and `TECHNICAL-DESIGN.md`.

**Generated from pre-design handoff review | August 2026**

---

## Summary of Changes

| # | Change | From | To |
|---|--------|------|-----|
| 1 | Auth gating | Public search + private features | **All search requires login** |
| 2 | Provider count at launch | 10 by Phase 5 (post-launch expansion) | **All 10 at public launch** |
| 3 | Email alerts | v2 (`alerts`, `notifications` tables) | **v1** (include tables + email digest) |
| 4 | Application tracker | 4 states: `saved` → `applied` → `in_progress` → `closed` | **2 states: `saved`, `applied`** |
| 5 | Search engine | Elasticsearch from Phase 0 | **Postgres full-text first, ES later** |
| 6 | Enrichment | Rule-based only | Unchanged |
| 7 | Company pages | Minimal | Unchanged |

---

## Detailed Section-by-Section Changes

### Part 1 — Product Requirements

#### §1 Executive Summary (lines 80-99)
- **Change**: Update differentiation table — "No registration wall for search" → "Unified search behind auth"
- **Change**: v1 Objective — add "email alerts" to shipped features

#### §3 Goals & Success Metrics (lines 132-161)
- **KPI Dashboard**: Add `Alert-to-visit conversion` target for v1 Month 3 (was v2)
- **KPI Dashboard**: Move `Resume upload rate` and `AI match satisfaction` to v2 only (no change)

#### §4 User Personas (lines 164-237)
- **Persona 1 (Active Seeker)**: Value by version — v1 adds "email alerts for saved searches"
- **Persona 2 (Passive Browser)**: Value by version — v1 adds "email alerts for saved searches"
- **Persona 3 (Career Switcher)**: No change

#### §5 Tech Stack (lines 239-317)
- **Infrastructure table**: Remove Elasticsearch 8 from v1; add note "Postgres full-text for v1; ES migration planned v1.5"
- **Project Structure**: No change (search implementation detail)

#### §6 System Architecture (lines 321-360)
- **Architecture diagram**: Remove Elasticsearch box from v1; add PostgreSQL full-text search annotation
- **Infrastructure list**: Remove ES; note Postgres search

#### §7 Data Pipeline (lines 364-432)
- **§7.3 Providers**: All 10 providers must be **production-ready before public launch** (not just Phase 5 target)
- **§7.4 Pipeline Stages**: No change to enrichment (rule-based only)
- **§7.5 Data Freshness SLA**: Applies to all 10 providers at launch

#### §8 v1 — Core Features (lines 434-579)
- **§8.1 Job Search & Discovery**: 
  - Add "Requires authentication" note
  - Search response caching still applies
- **§8.2 User Accounts & Profile**:
  - Change "Browsing is fully public" → "All search requires authentication"
  - Registration wall at homepage/search entry point
- **§8.3 Saved Searches**:
  - **Add email digest capability**: Users can choose "instant" or "daily digest" frequency
  - Alert evaluation runs on pipeline ingestion (new worker)
  - Email delivery via Resend (already in stack)
- **§8.4 Job Bookmarking & Application Tracker**:
  - **Reduce states**: `saved` | `applied` only
  - Remove `in_progress` and `closed` states
  - Remove follow-up date field (or make optional for v1.1)
  - Remove private notes (or keep as optional)
- **§8.5 Data Pipeline**:
  - Add alert evaluation worker to Hangfire jobs
  - Pipeline runs trigger alert matching for new jobs

#### §9 Future Roadmap (lines 581-675)
- **§9.2 Job Alert Notifications**: 
  - Change from `v2` → `v1` (with email digest)
  - Remove "in-app notification center" from v1 (keep for v2)
  - Slack/browser push stays v3
  - Schema tables `alerts`, `alert_matches`, `notifications` now v1
- **§9.3 Resume / CV Analyzer**: Stays v2
- **§9.4 Skill Gap Recommendations**: Stays v3
- **§9.5 Salary Benchmarking**: Stays v3

#### §10 User Flows (lines 677-743)
- **§10.1 First-Time Visitor**:
  - Homepage → Sign up/login → Search (no public search)
  - Remove "no account required" from search
- **§10.2 Returning User — Daily Search**:
  - Add "Check email alerts" step
- **§10.3 Application Tracking**:
  - Simplify to `Saved` → `Applied` transition only
- **§10.5 Job Alert Setup** (was v2, now v1):
  - Move to v1 section
  - Email digest frequency selection
  - In-app notification center → v2

#### §11 Design Principles (lines 746-768)
- No changes needed

#### §12 Information Architecture (lines 771-798)
- **v1 routes**: Add `/dashboard/alerts` (email alert management)
- **Future routes**: Remove `/dashboard/alerts` from v2; keep `/profile/resume`, `/dashboard/career`

#### §13 Non-Functional Requirements (lines 802-847)
- **§13.1 Performance**: Add "Alert evaluation + email dispatch ≤ 5 min from ingestion" (was v2)
- **§13.3 Reliability**: Add "Alert delivery" target for v1
- **§13.4 Security & Privacy**: Add email preference management

#### §14 Out of Scope (lines 850-866)
- **Move from Out of Scope → In Scope (v1)**:
  - Job alert notifications (email digest only)
- **Keep Out of Scope**:
  - AI job matching (v2)
  - Resume / CV analyzer (v2)
  - Skill gap recommendations (v3)
  - Salary benchmarking (v3)
  - Mobile app (v3+)
  - Slack/browser push (v3)
  - Employer features, direct apply, social, interview prep, community

#### §15 Risks & Mitigations (lines 869-879)
- **Add risk**: "Email alert fatigue / low relevance" — Mitigation: digest frequency options, relevance threshold, easy unsubscribe
- **Add risk**: "Auth-gated search reduces SEO/organic discovery" — Mitigation: public SEO landing pages with job previews, "Sign up to see more" pattern
- **Update risk**: "Elasticsearch operational complexity" → "Deferred to v1.5; Postgres full-text for v1"

#### §16 Open Questions (lines 882-893)
- **Update Q3**: "Elasticsearch vs Postgres full-text for v1" → Resolved: Postgres first
- **Add Q8**: "Alert frequency default — instant vs daily digest?"
- **Add Q9**: "SEO strategy for auth-gated search — public landing pages?"

#### Appendix A — Feature Roadmap Matrix (lines 896-915)
| Feature | v1 | v2 | v3 |
|---|---|---|---|
| Multi-provider job aggregation (10) | ✅ | | |
| Cross-provider deduplication | ✅ | | |
| Job search + filters | ✅ | | |
| **Auth-gated search** | ✅ | | |
| User accounts + auth | ✅ | | |
| Job bookmarking | ✅ | | |
| **Application tracker (2-state)** | ✅ | | |
| **Saved searches + email alerts** | ✅ | | |
| Rule-based enrichment | ✅ | | |
| AI-powered enrichment | | ✅ | |
| **In-app notification center** | | ✅ | |
| Resume / CV analyzer | | ✅ | |
| AI job matching scores | | ✅ | |
| Skill gap recommendations | | | ✅ |
| Salary benchmarking | | | ✅ |
| Mobile app | | | ✅ |

---

### Part 2 — Delivery Phases

#### Phase 0 - Foundation and Architecture (lines 943-980)
- **§Scope**: Change "Decide initial search approach" → "Use PostgreSQL full-text search (`tsvector` + `tsquery`) for v1; plan ES migration"
- **§Scope**: Remove Elasticsearch setup from Phase 0
- **§Deliverables**: Remove "Elasticsearch integration"
- **§Exit Criteria**: Add "Postgres full-text search working for core filters"

#### Phase 1 - Pipeline Backbone (lines 983-1032)
- **§Scope**: All 10 provider connectors must be **implemented and tested** (not just one)
- **§Scope**: Add alert evaluation worker scaffolding
- **§Deliverables**: "10 provider connectors running end-to-end on schedule"
- **§Notes**: Remove "Do not start with 10 providers" — now required for launch

#### Phase 2 - Search and Job Discovery MVP (lines 1035-1092)
- **§Scope**: Search endpoints require authentication
- **§Scope**: Use Postgres full-text search (not ES)
- **§Exit Criteria**: "Authenticated user can search and filter jobs from all 10 live providers"

#### Phase 3 - Accounts and Profile (lines 1095-1137)
- **§Scope**: Add email preferences table/fields for alert frequency
- **§Scope**: Registration wall at search entry point
- **§Deliverables**: "Public landing pages with SEO content; search requires login"

#### Phase 4 - Saved Jobs, Saved Searches, and Application Tracker (lines 1140-1183)
- **§Scope**: `saved_jobs` table — reduce `status` enum to `saved` | `applied`
- **§Scope**: Remove `follow_up_at` or make nullable with v1.1 target
- **§Scope**: `saved_searches` — add `frequency` field (`instant` | `daily`)
- **§Scope**: Add `alerts` and `notifications` tables (moved from v2)
- **§Scope**: Build alert evaluation worker (Hangfire job)
- **§Scope**: Email digest template + Resend integration
- **§Scope**: `/dashboard/alerts` UI for managing alerts
- **§Deliverables**: 
  - "Bookmark + applied flag flow"
  - "Saved search with email digest (instant or daily)"
  - "Alert evaluation on pipeline ingestion"
- **§Exit Criteria**: 
  - "User can save job, mark applied"
  - "User can save search with email frequency, receives digest"
  - "New pipeline jobs trigger alert evaluation"

#### Phase 5 - Expand Coverage and Harden v1 (lines 1186-1223)
- **§Goal**: Now "Harden v1" (all 10 providers already done in Phase 1)
- **§Scope**: Focus on:
  - Dedup accuracy tuning with real data
  - Enrichment keyword rule improvements
  - Caching, rate limiting, error handling
  - Observability hardening
  - **Load testing with all 10 providers**
  - **Alert email deliverability testing**
- **§Deliverables**: "Launch-readiness checklist including alert system"
- **§Exit Criteria**: "All 10 providers stable; search + alerts + tracker stable for real users"

#### Recommended Milestone Order (lines 1226-1233)
- No order change, but Phase 1 scope significantly larger

#### What Not To Build in v1 (lines 1237-1255)
- Remove "Job alert notifications" from this list
- Add "In-app notification center" to this list
- Add "4-state application kanban" to this list

#### Recommended First Execution Slice (lines 1258-1269)
- Update to reflect Postgres search + auth-gated:
  1. Set up database and v1 job tables
  2. Add one provider connector (prove pipeline)
  3. Run scheduled pipeline flow end-to-end
  4. Persist canonical jobs
  5. Set up auth (register/login)
  6. Expose `GET /api/jobs` (authenticated)
  7. Expose `GET /api/jobs/{id}` (authenticated)

---

### Part 3 — Database Schema & ERD

#### Entity Overview (lines 1286-1316)
- **Move from v2 → v1**:
  - `alerts` → `[v1]`
  - `alert_matches` → `[v1]`
  - `notifications` → `[v1]`
- **Update `saved_jobs`**: Status enum reduced to `saved` | `applied`
- **Update `saved_searches`**: Add `frequency` field

#### Key Relationships (lines 1319-1329)
- Add: `users ||--o{ alerts : "creates"`
- Add: `alerts ||--o{ alert_matches : "produces"`
- Add: `alert_matches }o--|| canonical_jobs : "triggers"`
- Add: `users ||--o{ notifications : "receives"`

#### v1 ERD — Core Tables Only (lines 1333-1490)
**Tables to add to v1 ERD**:
- `alerts` (with `frequency` field)
- `alert_matches`
- `notifications`

**Tables to modify**:
- `saved_jobs`: Change `status` to `enum('saved', 'applied')`; make `follow_up_at` nullable; add `applied_at`
- `saved_searches`: Add `frequency` enum(`instant`, `daily`)

**Remove from v1 ERD** (defer to v1.5+ migration):
- Elasticsearch-specific indexes/annotations

#### Full ERD — All Versions (lines 1494-1745)
- Update version tags for `alerts`, `alert_matches`, `notifications` from `[v2]` to `[v1]`
- Update `saved_jobs` status enum
- Update `saved_searches` with frequency

#### Table Version Reference (lines 1749-1770)
| Table | Version | Feature |
|---|---|---|
| `alerts` | **v1** | Job Alerts (email digest) |
| `alert_matches` | **v1** | Job Alerts |
| `notifications` | **v1** | Email Notifications |
| `saved_jobs` | v1 | Application Tracker (2-state) |
| `saved_searches` | v1 | Saved Searches + Alerts |

#### Schema Notes (lines 1773-1788)
- Add: "`saved_jobs.status` uses 2-value enum (`saved`, `applied`) per simplified tracker"
- Add: "`saved_searches.frequency` controls email digest cadence"
- Add: "`alerts.frequency` mirrors saved search frequency for alert evaluation"
- Add: "PostgreSQL `tsvector`/`tsquery` used for v1 full-text search; `pg_trgm` for trigram similarity"
- Add: "Elasticsearch migration planned for v1.5; schema includes `indexed_at` for sync tracking"
- Update: "`users` carries `refresh_token`..." — no change
- Update: "`canonical_jobs` includes `is_archived`..." — no change

---

### Part 4 — API Contract

#### API Style (lines 1797-1803)
- No change (JWT + refresh token already specified)

#### Endpoints by Feature (lines 1804-1869)

**Job Search & Discovery**:
- `GET /api/jobs` — **Add `Authorization` header required**
- `GET /api/jobs/{id}` — **Add `Authorization` header required**
- `GET /api/companies/{id}` — **Add `Authorization` header required**

**Auth** — No change

**User Profile** — No change

**Saved Searches**:
- `GET /api/saved-searches` — Response includes `frequency` field
- `POST /api/saved-searches` — Request accepts `frequency` (`instant` | `daily`)
- `PATCH /api/saved-searches/{id}` — Can update `frequency`

**Saved Jobs & Application Tracker**:
- `GET /api/saved-jobs` — Response `status` only `saved` | `applied`
- `POST /api/saved-jobs` — Creates with `status: 'saved'`
- `PATCH /api/saved-jobs/{id}` — Accepts `status: 'applied'` + optional `appliedAt`
- `DELETE /api/saved-jobs/{id}` — Unchanged

**New: Alerts & Notifications (moved from v2)**:
```
GET    /api/alerts                  # List user alerts (same as saved-searches with frequency)
POST   /api/alerts                  # Create alert (saved search + frequency)
PATCH  /api/alerts/{id}             # Update alert
DELETE /api/alerts/{id}             # Delete alert
GET    /api/notifications           # List notifications (email sent records)
```

#### Request and Response Models (lines 1870-1920)

**Saved Search** — Add `frequency` field:
```json
{
  "id": "string",
  "name": "string",
  "criteria": "object",
  "frequency": "instant | daily"
}
```

**Saved Job / Application** — Reduce status values:
```json
{
  "status": "saved | applied",
  "appliedAt": "datetime | null"
}
```

**New: Alert Model**:
```json
{
  "id": "string",
  "name": "string",
  "criteria": "object",
  "frequency": "instant | daily",
  "isActive": "boolean",
  "lastTriggeredAt": "datetime | null"
}
```

**New: Notification Model**:
```json
{
  "id": "string",
  "type": "alert_match",
  "channel": "email",
  "subject": "string",
  "body": "string",
  "isRead": "boolean",
  "sentAt": "datetime"
}
```

#### Filtering, Sorting & Pagination Rules (lines 1922-1928)
- No functional change; auth required

#### Error Handling (lines 1929-1940)
- Add `401` for unauthenticated search requests

#### Tooling Notes (lines 1941-1946)
- No change

---

## Migration Checklist

### Code Changes Required

- [ ] **Backend**: Remove ES client init from Phase 0; add Postgres full-text search module
- [ ] **Backend**: Implement 10 provider connectors in Phase 1 (not incremental)
- [ ] **Backend**: Add `alerts`, `alert_matches`, `notifications` entities + migrations (v1)
- [ ] **Backend**: Simplify `saved_jobs.status` enum + migration
- [ ] **Backend**: Add `frequency` to `saved_searches` + migration
- [ ] **Backend**: Alert evaluation Hangfire job (triggered on pipeline ingestion)
- [ ] **Backend**: Email digest worker + Resend templates
- [ ] **Backend**: Auth middleware on all search endpoints
- [ ] **Backend**: Public landing page controller (SEO) without auth
- [ ] **Frontend**: Remove public search pages; add login gate
- [ ] **Frontend**: Simplify tracker UI to 2-state (save/applied)
- [ ] **Frontend**: Add alert frequency selector in saved search create/edit
- [ ] **Frontend**: Add `/dashboard/alerts` page
- [ ] **Frontend**: Public landing pages with job previews + "Sign up" CTAs

### Documentation Updates

- [ ] Update PRD.md with all changes above
- [ ] Update README.md architecture diagram
- [ ] Update API OpenAPI spec (auto-generated from code)
- [ ] Update database migration scripts

### Testing Updates

- [ ] Integration tests for auth-gated search
- [ ] Integration tests for alert evaluation + email
- [ ] Load tests with 10 providers
- [ ] Email deliverability tests

---

## Open Decisions for Implementation

1. **Alert evaluation timing**: Run synchronously in pipeline `Index` stage, or async Hangfire job after?
2. **Email template design**: Plain text + HTML? Use MJML?
3. **SEO landing pages**: Static generated at build time, or SSR with cached job data?
4. **Postgres full-text config**: `tsvector` column + GIN index, or expression index? `pg_trgm` threshold?
5. **Migration strategy for ES v1.5**: Dual-write during transition, or cutover?

---

## Effort Impact Estimate

| Change | Phase Impact | Effort |
|---|---|---|
| Auth-gated search | Phase 0, 2, 3 | Medium (auth middleware, landing pages) |
| 10 providers at launch | Phase 1 | **High** (10 connectors vs 1 + incremental) |
| Email alerts in v1 | Phase 1, 4 | Medium-High (worker, templates, Resend, UI) |
| Simpler tracker | Phase 4 | Low (fewer states, less UI) |
| Postgres full-text | Phase 0, 2 | Medium (search module rewrite, no ES) |
| **Total** | | **Significant increase in Phase 1 scope** |

---

## Recommendation

**Phase 1 is now the critical path**. With 10 providers required at launch, consider:
- Parallelizing connector development across team members
- Using a shared connector base class to reduce boilerplate
- Defining a "connector contract test suite" each must pass
- Timeboxing each connector to 1-2 days max

The auth-gated search simplifies Phase 2/3 (no public/private code paths) but adds Phase 3 work for landing pages.

---

*Plan generated for pre-design handoff. Review with engineering lead before sprint planning.*