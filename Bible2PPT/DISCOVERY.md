# Current Findings

## Repository Snapshot
- Solution `Bible2PPT.sln` comprises a WinForms client (`Bible2PPT/`) plus service libraries for Bible data, build pipeline, templates, and indexes.
- Tests mirror these services via sibling `*.Tests/` projects, all targeting `net6.0-windows` with xUnit and coverlet instrumentation.
- `.editorconfig` enforces 4-space indentation (C#), UTF-8 BOM, and sorted `using` directives with `System.*` first; `dotnet format` aligns submissions.

## Bible Content Flow
- Runtime Bible data originates from web scrapers in `Bible2PPT.Services.BibleService/Sources/`:
  - `GoodtvBible` (`goodtvbible.goodtv.co.kr`), `GodpiaBible` (`bible.godpia.com`), and `GodpeopleBible` (`find.godpeople.com`) request HTML via static `HttpClient` instances.
  - Each source overrides `GetBiblesOnlineAsync`, `GetBooksOnlineAsync`, and `GetVersesOnlineAsync` to parse provider-specific markup (regex/EUC-KR decoding) before mapping to domain models.
- `BibleService` caches these results in SQLite (`bible-v3.db`) through EF Core (`BibleContext`). If cached rows exist, it skips network calls; otherwise it fetches online and persists.

## Build & Runtime Notes
- Main program wires services through dependency injection (`Program.cs`) and registers SQLite files:
  - `bindex-v3.db` for index lookups
  - `bible-v3.db` for fetched scripture content (config key `BibleContext`)
  - `build-v3.db` for slide generation state
- PowerPoint automation lives in `Bible2PPT.Services.BuildService`, consuming Office interop; templates reside under `Template/`.

## Porting Considerations (Python)
- Rewriting parity in Python implies reimplementing: desktop UI (e.g., PyQt), PowerPoint export (`python-pptx`/COM), EF Core data model (SQLAlchemy), plus every scraper with requests + parsing. Estimate: multi-month project for full feature coverage, new automated testing, and packaging pipelines.

## Offline-First Strategy
- Goal: ship with pre-populated scripture data so the app never depends on live HTTP.
- Approach:
  1. Build a one-off ingestion tool (existing sources) to generate a complete `bible-v3.db` offline bundle per release.
  2. Embed or distribute that DB with installers; on first run, copy from resources if missing.
  3. Modify `BibleService` to treat cache misses as errors (or load from offline bundle) so production never calls web sources; optionally introduce an `OfflineBibleSource` adapter around the bundled database.
  4. Maintain a separate “data refresh” workflow to regenerate the DB when provider text changes.
- Additional checks: redistribution licenses of source content, storage footprint optimization, encoding validation for non-UTF8 providers.

## Pending Questions / Follow-Up
- Legal clarity on bundling third-party Bible texts.
- Desired template customization UX improvements (preview, validation) once offline goals are met.
- Whether to provide periodic data updates or rely on user-triggered refreshes.
