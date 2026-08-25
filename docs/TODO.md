# Documentation TODO

## In Progress
- [ ] Read and filter PRD with your interest ([PRD.md](./PRD.md))
- [ ] Learn minimal APIs by doing

## Backlog
- [ ] Phase 1 — Pipeline backbone ([TECHNICAL-DESIGN.md](./TECHNICAL-DESIGN.md) §4): v1 ingestion tables exist; build Greenhouse connector end-to-end, normalize → dedup → enrich → index, Hangfire orchestration
- [ ] Phase 2 — Search and job discovery MVP: `GET /api/jobs`, `GET /api/jobs/{id}`, Postgres FTS queries + filters

## Done ✓
- [x] Docs initialization
- [x] Restructure docs: product PRD + technical design split
- [x] Resolve scope conflict (rejected plan archived in [`archive/`](./archive/))
- [x] Decide first provider connector: Greenhouse, then Lever (PRD §9)
- [x] **Phase 0 — Foundation** ([TECHNICAL-DESIGN.md](./TECHNICAL-DESIGN.md) §4):
  - 4-project Clean Architecture aligned to §1.4 (`Jobbly.Domain/Application/Infrastructure/Api`), naming + namespaces match the docs
  - Domain entities for pipeline/jobs domain: Provider, Company, Job, CanonicalJob, PipelineRun + enums (§5.1 subset — users/saved tables deferred to Phases 3–4)
  - EF Core + Npgsql: per-entity configurations, jsonb columns, enums-as-text, `InitialCreate` migration, migrate-on-startup
  - Postgres FTS groundwork: generated `tsvector` column + GIN index on jobs
  - Options-pattern config with validate-on-start: Providers / Pipeline / JwtSettings sections
  - Observability baseline: Serilog console sink, RFC 9457 ProblemDetails for status codes + global exception handler
