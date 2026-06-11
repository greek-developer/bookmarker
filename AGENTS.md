# AGENTS.md

This file is for AI agents working on this repository. Read the documents below before making any changes.

## Required reading

- **[ARCHITECTURE.md](specs/ARCHITECTURE.md)** — project layout, data model, request lifecycle, rendering pipeline, caching, and all key design decisions. Read this before touching any `.cs` file or `template.html`.
- **[CONTRIBUTING.md](CONTRIBUTING.md)** — conventions, rules, and a map of where each concern lives. Read this before writing or editing code.

## Build & verify

```powershell
dotnet build .\grdev.bookmarker.sln
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

After code changes, restart the process. After `template.html`-only changes, POST to `/refresh` to bust the cache without restarting.

## Things agents must not do

- Write files to the app content root at runtime (all user data goes to `%USERPROFILE%`)
- Remove or rename the `{{content}}`, `{{footer}}`, or `{{timestamp}}` placeholders in `template.html`
- Break the CSS-only tab mechanism (`.tabs > input:checked + label + div`)
- Tighten JSON deserialization (case-insensitive and trailing-comma-tolerant by design)
- Add `[JsonConverter]` to a `Bookmark`/`BookmarkSet` subclass via inheritance — it must be explicit
