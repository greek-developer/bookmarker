# Architecture

## Overview

Bookmarker is a minimal ASP.NET Core application (.NET 10, minimal API style). It has no database, no client-side framework, and no build pipeline. The entire application fits in a handful of files.

## Request lifecycle

```
Browser GET /
  └─ Program.cs: GET / handler
       ├─ cachedHtml is set? → return cached HTML
       └─ BuildHtml()
            ├─ Read template.html from disk
            ├─ Read ~/.bookmarker.json  → BookmarkerOptions
            ├─ For each tab, read each content file → BookmarksFileContent[]
            ├─ Render() — build HTML string via StringBuilder
            │    └─ Replace {{content}}, {{footer}}, {{timestamp}} in template
            └─ Cache and return HTML
```

`POST /refresh` nulls `cachedHtml` and redirects to `GET /`, forcing a re-read of all files on the next request.

## Project layout

```
src\Bookmarker\
  Program.cs              # startup, GET /, POST /refresh, /api/diagnostics/version, Render()
  Defaults.cs             # EnsureUserConfig() — first-run file creation
  ProductionVersion.cs    # reads ProductionVersion.json emitted at build time
  template.html           # HTML shell; {{content}}, {{footer}}, {{timestamp}}, {{version}}
  Models\
    Bookmark.cs           # data models + BookmarkJsonConverter + BookmarkSetJsonConverter
    BookmarkerOptions.cs  # BookmarkerOptions, BookmarkerTabsOptions
  bookmarks\              # empty — reserved for future bundled content
tests\Bookmarker.UnitTests\
  BookmarkJsonConverterTests.cs
  BookmarkerOptionsTests.cs
docs\
  ARCHITECTURE.md         # this file
  project-brief.md        # editorial brief for blog post / video
  ui\design-prompt.md     # the design brief the UI CSS was generated from
specs\                    # how the product behaves, one spec per domain
tasks\tasks.md            # running task checklist
AGENTS.md                 # the grdev standard — synced, never edited locally
BRIEF.md                  # project ground truth
README.md
```

## Data model

```
BookmarkerOptions
  Tabs: BookmarkerTabsOptions[]
    Name: string
    Files: string[]           ← absolute paths to content files

BookmarksFileContent          ← one file = one collapsible section
  Name: string
  Groups: BookmarksGroup[]
    Name: string              ← empty = no wrapper element rendered
    Sets: BookmarkSet[]
      Name: string
      Url: string
      Bookmarks: Bookmark[]   ← pipe-separated related links

Bookmark                      ← base type, has [JsonConverter]
  Name: string
  Url: string

BookmarkSet : Bookmark        ← also has [JsonConverter] (not inherited)
  Bookmarks: Bookmark[]
```

## JSON deserialization

`BookmarkJsonConverter` and `BookmarkSetJsonConverter` handle two input formats:

- **String shorthand**: `"Label=https://url"` — split on first `=`
- **Object format**: `{ "name": "...", "url": "...", "bookmarks": [...] }`

`[JsonConverter]` is not inherited in System.Text.Json, so `BookmarkSet` carries its own `[JsonConverter(typeof(BookmarkSetJsonConverter))]` attribute. `BookmarkSetJsonConverter` delegates to `BookmarkJsonConverter` with `typeToConvert = typeof(BookmarkSet)` so the correct concrete type is returned.

Deserialization options: case-insensitive property names, trailing commas allowed.

## HTML rendering

`Render()` in `Program.cs` builds the full page content via `StringBuilder` and injects it into `template.html` using three string replacements:

| Placeholder | Value |
|---|---|
| `{{content}}` | The generated tab/section/group/link HTML |
| `{{footer}}` | Lowercased path to `~/.bookmarker.json` |
| `{{timestamp}}` | `DateTime.Now` formatted as `yyyy/MM/dd HH:mm` |

The tab mechanism is CSS-only: a flat sequence of `<input type="radio"> <label> <div>` triplets inside `.tabs`. The selector `.tabs > input:checked + label + div` shows the active tab's content. No JavaScript is involved in tab switching.

Sections and groups use `<details open>` — all expanded by default on every page load.

## Template

`template.html` is a self-contained file (no external files beyond a Google Fonts CDN link for Fira Code). Styles are in a `<style>` block. A small inline `<script>` at the end of the body updates `document.title` to `bookmarker: <active tab name>` on tab switch and on load.

## First-run

`Defaults.EnsureUserConfig()` runs at startup before any request is served. If `~/.bookmarker.json` does not exist it writes:

- `~/.bookmarker.json` — root config referencing the companion files
- `~/.bookmarker.welcome.json`
- `~/.bookmarker.social.json`
- `~/.bookmarker.google.json`

If the file already exists the method returns immediately without touching anything.

## Caching

`cachedHtml` is a module-level `string?`. It is populated on the first `GET /` and cleared by `POST /refresh`. Thread safety is not a concern for a single-user local tool.
