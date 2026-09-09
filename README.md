# Jobbly

A technical job aggregation platform. Jobbly pulls listings from multiple job-board providers, deduplicates them, enriches them with tech-stack/seniority/salary metadata, and delivers a fast, developer-native search and application-tracking experience.

Built as a learning project — the goal is to ship a working v1 while getting real reps on Clean Architecture, background job pipelines, and Postgres full-text search.

> Product spec: [`PRD.md`](./docs/PRD.md) · Technical design (schema, API contract, delivery phases): [`TECHNICAL-DESIGN.md`](./docs/TECHNICAL-DESIGN.md)

---

## What it does

- **Search & discovery** — full-text, tech-aware search (`Node.js`, `.NET`, `k8s`) with filters for role, stack, seniority, location, remote, salary
- **Aggregation pipeline** — one connector per provider, deduplicated across sources, rule-based enrichment (tags, seniority, salary normalization)
- **Accounts** — email/password + Google OAuth, fully public browsing (no signup wall)
- **Saved searches** — persist filters, dashboard feed surfaces new matches
- **Application tracker** — `saved → applied → in_progress → closed`, private notes, follow-up dates

v1 scope deliberately excludes AI matching, alerts, and resume analysis — those are v2/v3, once the pipeline and retention are proven. See [PRD §6](./docs/PRD.md#6-not-in-v1-out-of-scope).

---

## Tech stack

| Layer | Choice |
|---|---|
| Backend | .NET 10 / ASP.NET Core, Minimal APIs |
| Database | PostgreSQL 16 (EF Core + Npgsql) |
| Search | Postgres full-text (`tsvector` generated column + GIN index) — Elasticsearch later if scale demands it |
| Background jobs | Hangfire (ingestion pipeline scheduling, dashboard, retries) |
| Auth | JWT access (15 min, `Authorization: Bearer`) + opaque refresh tokens (7 days, hashed at rest, rotated on refresh); Google OAuth *(deferred)*; roles `User` + `Admin` |
| Containerization | Docker / Docker Compose |

---

## Architecture

Clean Architecture, four projects. The ingestion pipeline is Application use cases (ports + orchestrator) implemented by Infrastructure adapters — not a separate service.

```
Jobbly.slnx
└── src/
    ├── Jobbly.Domain/          # Entities (Provider, Company, Job, CanonicalJob,
    │                           #   PipelineRun) + enums — zero dependencies
    ├── Jobbly.Application/     # Use cases + ports: IJobConnector, IJobNormalizer,
    │                           #   IDeduplicationService, IEnrichmentService,
    │                           #   RunIngestionPipeline, IJobblyDbContext
    ├── Jobbly.Infrastructure/  # Connectors (Greenhouse, Lever), EF Core/Npgsql,
    │                           #   Hangfire scheduling, options config
    └── Jobbly.Api/             # Minimal APIs, middleware, composition root
```

**Dependency rule:** `Application` never references `Infrastructure` — Infrastructure implements Application's interfaces; `Api` is the only place they meet (DI wiring in `Program.cs`).

**Pipeline flow** (Hangfire-triggered, runs per provider independently so one broken source never cascades):

```
Hangfire trigger → RunIngestionPipeline
  → IJobConnector.FetchAsync()    (per provider, isolated, Polly retry + circuit breaker)
  → normalize                     (provider payload → canonical Job entity)
  → deduplicate                   (exact fingerprint, then pg_trgm ≥0.95 same-company fuzzy pass → link to CanonicalJob)
  → enrich                        (tech tags incl. dotted/multi-word forms, 12-level seniority inference,
                                   remote incl. provider hints, requirements/nice-to-haves section extraction)
  → index                         (Postgres FTS tsvector is maintained by a generated column)
  → record PipelineRun            (counts, errors, retries)
```

Design decision: the pipeline runs inside `Jobbly.Api` for v1 (one deployable, direct DB writes). It's built behind ports so it can be extracted later if there's a concrete reason to.

---

## Delivery status

Following the phases in [TECHNICAL-DESIGN §4](./docs/TECHNICAL-DESIGN.md#4-delivery-phases):

- [x] **Phase 0 — Foundation**: project structure, domain entities, EF Core + migrations, Postgres FTS groundwork, validated options config, Serilog + ProblemDetails error handling, Docker Compose dev/prod environments
- [x] **Phase 1 — Pipeline backbone**: Greenhouse connector end-to-end (fetch → normalize → dedup → enrich → persist), Hangfire recurring runs, verified against the live Stripe board (594 jobs) via manual trigger; Lever (HighLevel board) and Ashby (Linear board) connectors added in Phase 5, each provider isolated behind `IJobConnector` with its own named HttpClient + resilience pipeline
- [x] **Phase 2 — Search & discovery MVP**: `GET /api/jobs` (full-text q, tags, location, seniority, remote, salary filters; relevance/date/salary sort; paging over deduplicated canonicals) + `GET /api/jobs/{id}` detail; public, no login wall
- [x] **Phase 3 — Accounts & profile**: Identity (email/password), JWT access + refresh (rotation, reuse detection), `User`/`Admin` roles, `/api/users/me*` authenticated-only, `/api/pipeline/trigger` + `/hangfire` admin-gated
- [x] **Phase 4 — Saved jobs/searches & application tracker**: `/api/saved-jobs` (save/list/patch/delete, status flow, duplicate → 409), `/api/saved-searches` (CRUD + `/matches` feed reusing the search pipeline)
- [ ] **Phase 5 — Expand coverage & harden**

---

## Getting started

### Prerequisites

- .NET SDK 10 (for local dev without Docker)
- Docker + Docker Compose

### Run with Docker Compose (recommended)

```bash
git clone <repo-url>
cd jobbly

# configure secrets (JWT key, DB credentials, ports)
cp .env.example .env   # then edit values

# development - hot reload via dotnet watch, Scalar UI enabled
docker compose -f compose.yml -f compose.dev.yml up --build

# or production target
docker compose up --build
```

Once running:

| URL | What |
|---|---|
| `http://localhost:${API_PORT}/scalar/v1` | Scalar API UI (dev only) — each endpoint carries a name, summary, and description in the spec |
| `http://localhost:${API_PORT}/openapi/v1.yaml` | OpenAPI spec (dev only) |
| `localhost:${POSTGRES_PORT}` | Postgres (host-side access) |

Migrations apply automatically on startup (`DatabaseInitializer`). The API waits for the DB healthcheck before starting.

**Manually run an ingestion pass** (instead of waiting for the Hangfire schedule) — requires the **Admin** role:

```bash
curl -X POST http://localhost:${API_PORT}/api/pipeline/trigger/greenhouse \
  -H "Authorization: Bearer <admin-access-token>"
# second provider (separate board, isolated run):
curl -X POST http://localhost:${API_PORT}/api/pipeline/trigger/lever \
  -H "Authorization: Bearer <admin-access-token>"
# third provider (Ashby board, same pattern):
curl -X POST http://localhost:${API_PORT}/api/pipeline/trigger/ashby \
  -H "Authorization: Bearer <admin-access-token>"
```

Returns the run summary (jobs fetched/created/updated/deduplicated, status) as JSON; `404` if the provider slug has no active connector, `401` without a token, `403` for non-admins.

**Search and job discovery:**

`GET /api/jobs` returns one listing per deduplicated canonical job, with optional filters:

| Query param | Type | Notes |
|---|---|---|
| `q` | string | Full-text search over title/company/description (Postgres tsvector) |
| `tags` | string[] | Repeatable (`?tags=python&tags=kafka`) — matches the enriched tech stack |
| `location` | string | Case-insensitive match on job location |
| `seniority` | enum | `0`=Unknown, `1`=Internship, `2`=EntryLevel, `3`=Junior, `4`=MidLevel, `5`=Senior, `6`=Staff, `7`=Lead, `8`=Principal, `9`=Manager, `10`=Director, `11`=Executive |
| `remote` | enum | `1` (Remote), `2` (Hybrid), `3` (OnSite) |
| `salaryMin` / `salaryMax` / `salaryCurrency` | int / string | Salary filtering |
| `sort` | enum | `Relevance` (default), `Date`, `Salary` |
| `page` / `pageSize` | int | Paging (`page` ≥ 1, `pageSize` 1–100) |
| — (all) | — | Invalid values (bad enums, out-of-range paging, malformed email) return `400` with ProblemDetails |

`GET /api/jobs/{canonicalId}` returns the full detail for one job (overview, requirements, salary range, source URL); `404` if not found or archived.

```bash
curl "http://localhost:${API_PORT}/api/jobs?q=.net&location=london&pageSize=20"
curl "http://localhost:${API_PORT}/api/jobs/01a06299-e407-7b5d-aab4-203d3c587d65"
```

**Accounts & auth (ASP.NET Core Identity + JWT):**

| Endpoint | Body | Notes |
|---|---|---|
| `POST /api/auth/register` | `{email, password, fullName}` | Creates the user **and** their 1:1 profile; returns a token pair |
| `POST /api/auth/login` | `{email, password}` | Returns a token pair |
| `POST /api/auth/refresh` | `{refreshToken}` | Rotates the refresh token (old one is revoked) and returns a new pair |
| `POST /api/auth/logout` | `{refreshToken}` | Revokes that refresh token |
| `GET /api/users/me` | — | Current profile; requires a bearer token (caller resolved from its `sub` claim) |
| `PUT /api/users/me/profile` | profile fields | Partial update; enum fields take numeric values; requires a bearer token |
| `PUT /api/users/me/skills` | `{skills:[…]}` | Replaces the whole skill set (deduped); requires a bearer token |

**Tracker — saved jobs & searches (all require a bearer token):**

| Endpoint | Body | Notes |
|---|---|---|
| `GET /api/saved-jobs?page=&pageSize=` | — | Tracked jobs, newest first, with a compact job snapshot |
| `POST /api/saved-jobs` | `{canonicalJobId}` | Starts tracking (`Saved`); `404` unknown/archived, `409` already saved |
| `PATCH /api/saved-jobs/{id}` | `{status?, notes?, followUpAt?}` | Status is numeric (`0`=Saved, `1`=Applied, `2`=InProgress, `3`=Closed); first `Applied` records the timestamp |
| `DELETE /api/saved-jobs/{id}` | — | Stops tracking (idempotent) |
| `GET /api/saved-searches` | — | Saved searches with stored criteria |
| `POST /api/saved-searches` | `{name, criteria}` | `criteria` mirrors the `GET /api/jobs` filters |
| `PATCH /api/saved-searches/{id}` | `{name?, criteria?}` | Partial update |
| `DELETE /api/saved-searches/{id}` | — | Idempotent |
| `GET /api/saved-searches/{id}/matches?page=&pageSize=` | — | Dashboard feed: runs the stored filters through the search pipeline |

Every auth response looks like `{accessToken, refreshToken, expiresInSeconds, user}` — the access token is a signed JWT (15 min, HS256) validated against `JwtSettings`; send it as `Authorization: Bearer …`. Refresh tokens are opaque, stored **hashed** (SHA-256) in the `refresh_tokens` table, and rotated on each refresh. Replaying a revoked refresh token is treated as a stolen session and revokes **all** of that user's active tokens.

```bash
curl -X POST "http://localhost:${API_PORT}/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"Str0ng!Pass","fullName":"You"}'
# then: curl -X POST .../api/auth/refresh -d "{\"refreshToken\":\"…\"}"
```

Auth uses ASP.NET Core Identity (`AspNetUsers` etc.) backed by the same Postgres DB; `user_profiles`, `user_skills` and `refresh_tokens` are tables of their own. `JwtSettings:Key` must be a dev secret (≥32 chars — a generated one is baked into `appsettings.json` for local demo).

**Roles & authorization:**

- Two roles, seeded on startup: **`User`** (assigned automatically at registration) and **`Admin`** (assigned manually).
- `/api/users/me*` require any authenticated user (`401` without a token). Job search (`/api/jobs`) and the auth endpoints stay public.
- The **ingestion trigger** and the **Hangfire dashboard** (`/hangfire`, dev only) require the `Admin` role (`403` otherwise).
- Role claims ride in the JWT as `role` and are matched via `TokenValidationParameters.RoleClaimType` (no inbound claim remapping). The OpenAPI spec marks secured operations with a `bearerAuth` security requirement so Scalar prompts for a token.

**Operations (Phase 5):**

- **Health**: `GET /health` (liveness) and `GET /health/ready` (Postgres + Hangfire storage) — public, for load balancers. No extra packages; checks run on the EF `DbContext`.
- **Pipeline alerts**: hourly Hangfire watchdog (`pipeline-health-monitor`) warns on providers with consecutive failures and on providers with no run inside 2× their interval. Console sink for now.
- **Search cache**: `GET /api/jobs` responses are output-cached 90s per query (in-memory, shared — the endpoint is public with no per-user content) and evicted on every successful ingestion via `ICacheInvalidator` (port in Application, OutputCache impl at the composition root), so scheduled and manual runs both stay fresh.
- **Tests**: `tests/Jobbly.Tests` (xUnit) — enrichment rules, fingerprint normalization, tracker transitions, validation attributes, connector mapping (stub `HttpClient`), health-monitor behavior (fake `IJobblyDbContext`, no DB). Run with `dotnet test`.

### Local dev without Docker

```bash
docker compose up -d db        # database only
dotnet build Jobbly.slnx
dotnet watch run --project src/Jobbly.Api
```

Connection strings and settings live in `src/Jobbly.Api/appsettings.json` (overridable per environment / env vars). All option sections (`Providers`, `Pipeline`, `JwtSettings`) are validated at startup — misconfiguration fails fast.

### Gotchas worth knowing

- Running the compiled DLL directly sets the **content root to your current directory** — launch from the app's output folder or set `ASPNETCORE_CONTENTROOT`, otherwise config won't load
- Incremental builds don't always recopy edited `appsettings*.json` into `bin/` — rebuild after config edits if changes seem ignored

---

## License

TBD
