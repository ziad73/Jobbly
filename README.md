# Jobbly

A technical job aggregation platform. Jobbly pulls listings from multiple job-board providers, deduplicates them, enriches them with tech-stack/seniority/salary metadata, and delivers a fast, developer-native search and application-tracking experience.

Built as a learning project — the goal is to ship a working v1 while getting real reps on Clean Architecture, background job pipelines, and Postgres full-text search.

> Full product spec: [`PRD.md`](./PRD.md) · Technical design (schema, API contract, delivery phases): `TECHNICAL-DESIGN.md`

---

## What it does

- **Search & discovery** — full-text, tech-aware search (`Node.js`, `.NET`, `k8s`) with filters for role, stack, seniority, location, remote, salary
- **Aggregation pipeline** — one connector per provider, deduplicated across sources, rule-based enrichment (tags, seniority, salary normalization)
- **Accounts** — email/password + Google OAuth, fully public browsing (no signup wall)
- **Saved searches** — persist filters, dashboard feed surfaces new matches
- **Application tracker** — `saved → applied → in_progress → closed`, private notes, follow-up dates

v1 scope deliberately excludes AI matching, alerts, and resume analysis — those are v2/v3, once the pipeline and retention are proven. See [PRD §6](./PRD.md#6-not-in-v1-out-of-scope).

---

## Tech stack

| Layer | Choice |
|---|---|
| Backend | .NET 10 / ASP.NET Core, Minimal APIs |
| Database | PostgreSQL (Npgsql + EF Core) |
| Search | Postgres full-text (`tsvector`/`tsquery`) — Elasticsearch later if scale demands it |
| Background jobs | Hangfire (ingestion pipeline scheduling, dashboard, retries) |
| Auth | JWT access + refresh tokens (httpOnly cookie), Google OAuth |
| Containerization | Docker / Docker Compose |
| CI/CD | GitHub Actions |

---

## Architecture

Clean Architecture — the ingestion pipeline is a set of Application use cases triggered by Hangfire, not a separate service.

```
Jobbly.sln
├── src/
│   ├── Jobbly.Domain/          # Entities: Job, JobSource, PipelineRun
│   ├── Jobbly.Application/     # Use cases + ports: IJobConnector, IJobNormalizer,
│   │                           #   IDeduplicationService, IEnrichmentService,
│   │                           #   RunIngestionPipeline
│   ├── Jobbly.Infrastructure/  # Connectors (Greenhouse, Lever), EF Core/Npgsql,
│   │                           #   Hangfire scheduling
│   └── Jobbly.Api/             # Minimal APIs, composition root
└── tests/
    ├── Jobbly.Application.Tests/
    └── Jobbly.Infrastructure.Tests/
```

**Pipeline flow** (Hangfire-triggered, runs per provider independently so one broken source never cascades):

```
Hangfire trigger → RunIngestionPipeline
  → connector.FetchAsync()        (per provider, isolated)
  → save to raw_listings          (staging)
  → normalize → canonical Job
  → deduplicate (link via JobSource, keep provenance)
  → enrich (tags, seniority, salary)
  → upsert → Postgres
  → record PipelineRun (counts, errors, duration)
```

Design decision: the pipeline is embedded in `Jobbly.Api` for v1 (not a separate service) — direct DB writes, one deployable, no distributed-consistency problem to solve prematurely. It's built behind ports (`IJobConnector`, `RunIngestionPipeline`) so it can be extracted later if there's a concrete reason to.

---

## Build order (v1)

Bottom-up — each phase produces something runnable before the next depends on it:

1. **Skeleton & plumbing** — solution structure, EF Core + Postgres migration, Hangfire wired with a dummy job, Docker Compose dev environment
2. **Canonical model + fake connector** — finalize `Job` entity, one in-memory fake `IJobConnector`, prove the pipeline end-to-end
3. **Real connector: Greenhouse** — `HttpClientFactory`, per-connector error handling/retries
4. **Normalization** — real provider-shape → canonical `Job` mapping, rule-based tech-stack tagging
5. **Deduplication** — rule-based matching against real data, provenance preserved via `JobSource`
6. **Enrichment** — seniority inference, salary normalization
7. **Second connector: Lever** — validates the architecture; adding a provider should mean *only* a new connector + DI registration
8. **Observability** — `PipelineRun` populated properly, Hangfire dashboard as the monitoring surface

---

## Getting started

```bash
git clone <repo-url>
cd jobbly
docker compose up
```

_(Setup instructions will be filled in as Phase 0 lands.)_

---
## License
TBD
