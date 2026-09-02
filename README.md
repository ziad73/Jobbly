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
| Auth | JWT access + refresh tokens (httpOnly cookie), Google OAuth *(Phase 3)* |
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
  → deduplicate                   (fingerprint match → link to CanonicalJob)
  → enrich                        (tech tags, seniority inference, salary normalization)
  → index                         (Postgres FTS tsvector is maintained by a generated column)
  → record PipelineRun            (counts, errors, retries)
```

Design decision: the pipeline runs inside `Jobbly.Api` for v1 (one deployable, direct DB writes). It's built behind ports so it can be extracted later if there's a concrete reason to.

---

## Delivery status

Following the phases in [TECHNICAL-DESIGN §4](./docs/TECHNICAL-DESIGN.md#4-delivery-phases):

- [x] **Phase 0 — Foundation**: project structure, domain entities, EF Core + migrations, Postgres FTS groundwork, validated options config, Serilog + ProblemDetails error handling, Docker Compose dev/prod environments
- [x] **Phase 1 — Pipeline backbone**: Greenhouse connector end-to-end (fetch → normalize → dedup → enrich → persist), Hangfire recurring runs, verified against the live Stripe board (594 jobs) via manual trigger
- [ ] **Phase 2 — Search & discovery MVP**: `GET /api/jobs`, filters, sorting
- [ ] **Phase 3 — Accounts & profile**
- [ ] **Phase 4 — Saved jobs/searches & application tracker**
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
| `http://localhost:${API_PORT}/scalar/v1` | Scalar API UI (dev only) |
| `http://localhost:${API_PORT}/openapi/v1.yaml` | OpenAPI spec (dev only) |
| `localhost:${POSTGRES_PORT}` | Postgres (host-side access) |

Migrations apply automatically on startup (`DatabaseInitializer`). The API waits for the DB healthcheck before starting.

**Manually run an ingestion pass** (instead of waiting for the Hangfire schedule):

```bash
curl -X POST http://localhost:${API_PORT}/api/pipeline/trigger/greenhouse
```

Returns the run summary (jobs fetched/created/updated/deduplicated, status) as JSON; `404` if the provider slug has no active connector.

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
