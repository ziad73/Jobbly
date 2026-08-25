PostgreSQL Full-Text Search (FTS) is Postgres's built-in search engine — it lets you do Google-style keyword matching over text columns without running a separate service like Elasticsearch.

## The core idea

Plain SQL `LIKE '%dotnet%'` is dumb matching: no word ranking, no stemming, full table scans. FTS instead works in two steps:

**1. Tokenize + stem text into a `tsvector`**
```sql
SELECT to_tsvector('english', 'Senior .NET Backend Engineer building Node.js services');
-- 'backend':3 'build':6 'engin':4 'net':2 'node.j':7 'senior':1 'servic':8
```
Notice `building` → `build` (stemming) and positions are kept for phrase queries. That's the `SearchVector` generated column we created on your `jobs` table — Postgres computes it automatically whenever title/company/description change.

**2. Query it with a `tsquery`**
```sql
SELECT * FROM jobs
WHERE "SearchVector" @@ to_tsquery('english', 'senior & backend & !(intern | junior)');
-- @@ = match operator; supports AND (&), OR (|), NOT (!), phrases ('.net engineer')
```

## Why it's fast: the GIN index

Without an index, every query scans all tsvectors. The GIN index we added (`IX_jobs_SearchVector`) is an inverted index — like a book's index term → rows — so lookups stay fast even at hundreds of thousands of listings.

## What you get out of the box

- **Relevance ranking**: `ts_rank()` scores matches by term frequency/proximity — that powers your `sort=relevance`
- **Language-aware stemming**: the `'english'` config means "engineering" matches "engineer"
- **Prefix matching**: `to_tsquery('dot:*')` matches dotnet, docker... useful for autocomplete
- **Highlighting**: `ts_headline()` returns snippets with `<b>` marks

## Why Jobbly chose it (PRD §9, Decision 5)

It lives inside the Postgres you already run — zero extra infra for v1. Elasticsearch/Meilisearch only enter the picture if/when scale demands it (open question §11.3). For a few hundred k listings with proper indexing, Postgres FTS comfortably meets your P95 < 500 ms target.

One caveat worth knowing: default stemming treats `.NET`, `C#`, `k8s`, `Node.js` poorly (punctuation gets mangled). Tech-aware tokenization — one of your PRD's differentiators — will eventually need either custom dictionaries/config or pre-processing tokens into a separate tags column (your jsonb `tech_stack` already helps there).

Want me to continue with step 4 (Options config classes)?
