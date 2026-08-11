# BRIEF.md — Bookmarker

Project ground truth. Read this at the start of every session.

## Overview

Bookmarker is a self-hosted start page you run locally and point your browser at. Bookmarks
live in plain JSON files — one per project or context — and you compose your personal page by
picking which files to show. It targets developers who spend the day jumping between internal
tools, dashboards, staging environments and documentation, and want a single keyboard-accessible
place to go.

The core idea is that **a bookmark file is just a data file**. A `.bookmarker.*.json` file can
live in a project repository, be versioned and shared with the team, and be swapped in or out as
focus moves between projects. There is no database, no account, no sync, and no browser extension.

It runs interactively for development and as a Windows Service in normal use.

## Build & run

```powershell
dotnet build .\grdev.bookmarker.slnx
```

```powershell
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

```powershell
dotnet test .\grdev.bookmarker.slnx
```

Requires the **.NET SDK 10.0**. No environment variables, no local services, no external
dependencies to start first.

Default URLs: `http://localhost:5069` / `https://localhost:7144`.

**After code changes, restart the process.** After a `template.html`-only change, POST to
`/refresh` (or click the ↻ button in the footer) to bust the cache without restarting — the
template is re-read from disk on every cache miss.

## Layout

Deviations from the standard layout, and what the project-specific folders hold:

| Path | Holds |
|---|---|
| `docs/project-brief.md` | Promotional/editorial brief written for a blog post and video — not technical documentation |
| `docs/ui/design-prompt.md` | The written design brief the `template.html` CSS was generated from. Treat it as the source of the visual direction |
| `src/Bookmarker/bookmarks/` | Empty — reserved for future bundled content |
| `src/Bookmarker/template.html` | The entire UI: one self-contained HTML file with inline styles |

## Stack

| Technology | Role | Notable |
|---|---|---|
| ASP.NET Core minimal API (.NET 10) | Web server and renderer | No controllers, no view engine, no EF. The whole HTTP layer is a handful of endpoints in `Program.cs` |
| `System.Text.Json` | JSON parsing | A custom dual-format converter accepts both `"Name=https://url"` shorthand and full object syntax |
| `Microsoft.Extensions.Hosting.WindowsServices` | Windows Service hosting | One package and one `builder.Host.UseWindowsService()` call |
| `template.html` | UI shell | Read from disk on every cache miss, so UI changes are live without a restart |
| CSS-only tabs | Tab navigation | The radio-button trick — no JavaScript for tab switching |
| Fira Code (Google Fonts CDN) | Typography | Monospace throughout; the design direction is terminal/TUI |

Stack rules that apply to this project only:

- **Server-side rendering only.** All HTML is built in `Render()` in `Program.cs`. Avoid adding
  client-side logic beyond the minimal inline script that updates `document.title`.
- **No JavaScript for layout or navigation.** Tabs are CSS-only; sections use `<details>`.
- **External CSS via CDN is acceptable**, but `template.html` stays self-contained — no separate
  stylesheets or scripts on disk.
- **Missing bookmark files are silently skipped.** This is deliberate: it lets users reference
  files they intend to create later.

## Contributing

Where each concern lives:

| What you want to change | File |
|---|---|
| Request handling, rendering logic | `src\Bookmarker\Program.cs` |
| First-run default file content | `src\Bookmarker\Defaults.cs` |
| Page HTML structure and styles | `src\Bookmarker\template.html` |
| Data models and JSON converters | `src\Bookmarker\Models\Bookmark.cs` |
| Root config model | `src\Bookmarker\Models\BookmarkerOptions.cs` |
| Production version emission | `src\Bookmarker\Bookmarker.csproj` (`WriteProductionVersion` target) |

**Changing the UI.** `template.html` is a drop-in replacement — it must contain exactly
`{{content}}`, `{{footer}}`, `{{timestamp}}` and `{{version}}` as placeholders, and the `.tabs`
wrapper div is required.

**Adding a new default bookmark file.** Add a private static method returning
`BookmarksFileContent` in `Defaults.cs`, register the file path and add it to the appropriate tab
in `EnsureUserConfig()`, then write the corresponding JSON file to the config directory for your
local dev environment — `EnsureUserConfig` only runs when the root config is absent.

**Changing the data model.** `BookmarksFileContent`, `BookmarksGroup`, `BookmarkSet` and
`Bookmark` are all deserialized from user files, as are `BookmarkerOptions` /
`BookmarkerTabsOptions`. Additive changes (new optional properties) are safe; renaming or removing
a property is a breaking change for existing user configs.

## Tests

| Project | Covers |
|---|---|
| `tests/Bookmarker.UnitTests` | Pure logic — the dual-format JSON converters and config deserialization |

There are no integration or E2E layers. The app is a single-user local tool with no I/O worth
mocking beyond file reads.

## Never

- **Write files to the app content root at runtime.** All user data goes to the configured config
  directory (by default `C:\Program Files\Bookmarker\`), never next to the running assembly's
  content root.
- **Remove or rename the `{{content}}`, `{{footer}}`, `{{timestamp}}` or `{{version}}` placeholders**
  in `template.html`. Rendering is three string replacements — a renamed placeholder fails silently
  and ships a page with literal braces in it.
- **Break the CSS-only tab mechanism** (`.tabs > input:checked + label + div`). The backend emits
  `radio → label → div` triplets that must stay in that DOM order for the adjacent-sibling selector
  to work; the flexbox `order` property is what makes the labels appear in a row.
- **Tighten JSON deserialization.** Case-insensitive property names and trailing commas are allowed
  by design — users hand-edit these files.
- **Add `[JsonConverter]` to a `Bookmark`/`BookmarkSet` subclass via inheritance.** The attribute is
  not inherited in System.Text.Json; each subclass must carry it explicitly.

## Decisions

### 2026-08-11

- Adopted the grdev Agentic standard: `AGENTS.md` is now the synced upstream template and is never
  edited locally; everything project-specific lives in this file.
- `CONTRIBUTING.md` was folded into this file's `## Contributing` section and deleted.
- `ARCHITECTURE.md` moved from `specs/` to `docs/`; `specs/` now holds product behavior specs only.
- `PROJECT_BRIEF.md` (blog/video promo material) moved to `docs/project-brief.md` to free the
  `BRIEF.md` name for this file.
- Build output moved from `artifacts/` to `release/`.
- The build emits `ProductionVersion.json`; the site footer and `GET /api/diagnostics/version`
  expose it.
- Solution migrated from `.sln` to `.slnx`.
- `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` enabled in `Directory.Build.props`.

### Earlier

- The default config path is `C:\Program Files\Bookmarker\` rather than `%USERPROFILE%`. A Windows
  Service runs as `SYSTEM` or `LOCAL SERVICE`, whose `%USERPROFILE%` is not the developer's home
  directory, so the config would be invisible. Breaking change made at version 0.5.0.
- Config is two-level: the root file defines tabs and lists absolute paths to content files;
  content lives in separate files so a project can own its own bookmark file.
- The HTML lives in `template.html` on disk rather than an embedded string or a Razor view, so the
  UI can change and the cache can be busted without recompiling.
- The rendered HTML is cached in a module-level `string?`; `null` means stale. No cache keys, no
  expiry, no locking — it is a single-user local tool.
