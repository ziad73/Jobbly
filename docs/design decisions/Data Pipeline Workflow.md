## `RunIngestionPipeline` Workflow

### Entry point

```
RunIngestionPipeline.ExecuteAsync(providerSlug, ct)
  └─→ returns PipelineRunResult?   (null = provider or connector missing)
```

### Flow diagram

```
┌──────────────────────────────────────────────────────────────┐
│ 1. RESOLVE                                                   │
│    Provider: Providers.SingleOrDefault(p.Slug == slug)        │
│    ├─ null  → return null  (endpoint maps to 404)            │
│    └─ !IsActive → return null                                │
│    Connector: connectors.FirstOrDefault(c.Slug == slug)      │
│    └─ null → return null                                     │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. START RUN                                                 │
│    run = PipelineRun.Start(provider.Id, provider.Slug)       │
│    PipelineRuns.Add(run)                                     │
│    (Status = Running, StartedAt = now)                       │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. FETCH              ◀── IJobConnector (e.g. Greenhouse)    │
│    rawJobs = await connector.FetchAsync(ct)                  │
│    run.RecordFetch(rawJobs.Count)                            │
│    └─ if throws → catch block (see §7)                       │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. PER-JOB LOOP                                              │
│    foreach (raw in rawJobs)                                  │
│    ├─ created = 0, updated = 0, deduplicated = 0             │
└──────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ 4a. WITHIN-     │ │ 4b. NORMALIZE    │ │ 4c. DEDUP       │
│     PROVIDER    │ │   (new only)     │ │   (new only)    │
│     EXIST?      │ │                  │ │                 │
│                 │ │ job =            │ │ ResolveAsync:   │
│ Jobs.Single     │ │ _normalizer      │ │  query Jobs     │
│   OrDefault     │ │ .Normalize       │ │  where Finger-  │
│  (ProviderId,   │ │  (raw,          │ │  print == job.  │
│   ExternalId)   │ │   provider.Id)  │ │  Fingerprint    │
│                 │ │                  │ │  AND ProviderId │
│ ├─ found:       │ │ Jobs.Add(job)   │ │  != job.        │
│ │ _normalizer   │ │                  │ │  ProviderId     │
│ │  .Update      │ └─────────────────┘ └─────────────────┘
│ │  (existing,   │          │                  │
│ │   raw)        │          │         ┌────────┴────────┐
│ │ _enrichment   │          │         ▼                 ▼
│ │  .Enrich      │          │ ┌──────────────┐ ┌──────────────────┐
│ │  (existing)   │          │ │ DUPLICATE    │ │ NEW              │
│ │ updated++     │          │ │              │ │                  │
│ │ continue      │          │ │ canonical =  │ │ canonical =      │
│ └─ not found:   │          │ │  CanonicalJobs│ │  CanonicalJob    │
│                 │          │ │  .Single     │ │  .Create(job)   │
└─────────────────┘          │ │  (id)        │ │                  │
                            │ │ job.Attach   │ │ CanonicalJobs    │
                            │ │  ToCanonical │ │  .Add(canonical) │
                            │ │  (id)        │ │                  │
                            │ │ canonical    │ │ job.Attach       │
                            │ │  .LinkSource │ │  ToCanonical     │
                            │ │  (1.0)       │ │  (canonical.Id)  │
                            │ │ deduplicated │ │ created++        │
                            │ │  ++          │ │                  │
                            │ └──────┬───────┘ └────────┬─────────┘
                            │        │                  │
                            │        └────────┬─────────┘
                            │                 ▼
                            │      ┌─────────────────────┐
                            │      │ 4d. ENRICH          │
                            │      │  _enrichment        │
                            │      │   .Enrich(job)      │
                            │      │  → Job.SetEnrichment│
                            │      │  PipelineStatus=    │
                            │      │  Enriched           │
                            │      └─────────────────────┘
                            │
                            └─ (back to top of loop)
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. FINALIZE (loop done)                                      │
│    run.Complete(created, updated, deduplicated)              │
│      → Status = Succeeded                                    │
│      → FinishedAt = now                                     │
│    provider.MarkSynced(now)                                  │
│      → LastSyncedAt = now                                    │
│      → LastError = null                                      │
│      → ConsecutiveFailures = 0                               │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│ 6. PERSIST                                                   │
│    await dbContext.SaveChangesAsync(ct)                      │
│    └─ all jobs + canonical_jobs + run + provider             │
│       in a single transaction                                │
└──────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌─────────────────────────────────────┐
        │  return ToResult(run)               │
        │   → PipelineRunResult(slug, Succeeded, …) │
        └─────────────────────────────────────┘
```

### Failure path (§7 — exception thrown anywhere in §3–§6)

```
┌──────────────────────────────────────────────────────────────┐
│ 7. CATCH  (Exception ex)                                     │
│    run.Fail(ex.Message)                                      │
│      → Status = Failed                                       │
│      → ErrorMessage = ex.Message                             │
│      → FinishedAt = now                                      │
│    provider.MarkFailed(ex.Message, now)                      │
│      → LastError = ex.Message                                │
│      → ConsecutiveFailures++                                 │
│      → LastSyncedAt = failedAtUtc                            │
│    await dbContext.SaveChangesAsync(ct)                      │
│      (may persist partially-tracked jobs)                    │
│    return ToResult(run)                                      │
│      → PipelineRunResult(slug, Failed, …, ErrorMessage)     │
└──────────────────────────────────────────────────────────────┘
```
