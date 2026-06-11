# Contributing

## Build & run

```powershell
dotnet build .\grdev.bookmarker.sln
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

Default URLs: `http://localhost:5069` / `https://localhost:7144`

## Before you start

Read [ARCHITECTURE.md](specs/ARCHITECTURE.md) for a full description of the project structure, data model, rendering pipeline, and key design decisions.

## Where things live

| What you want to change | File |
|---|---|
| Request handling, rendering logic | `src\Bookmarker\Program.cs` |
| First-run default file content | `src\Bookmarker\Defaults.cs` |
| Page HTML structure and styles | `src\Bookmarker\template.html` |
| Data models and JSON converters | `src\Bookmarker\Models\Bookmark.cs` |
| Root config model | `src\Bookmarker\Models\BookmarkerOptions.cs` |

## Key conventions

- **Server-side rendering only.** All HTML is built in `Render()` inside `Program.cs`. Avoid adding client-side logic beyond the minimal inline script that updates `document.title`.
- **No JavaScript for layout or navigation.** Tabs are CSS-only (radio button trick). Sections use `<details>`. Keep it that way.
- **`[JsonConverter]` is not inherited** in System.Text.Json. If you add a class that derives from `Bookmark` or `BookmarkSet`, add the attribute explicitly on the new class.
- **All user data lives in `%USERPROFILE%`.** The app must never write to its own content root at runtime.
- **`cachedHtml` is cleared by `POST /refresh`.** Any change that affects rendered output must be testable by hitting the refresh endpoint without restarting the app. Template changes (`.html` edits) are re-read on refresh. Code changes require a restart.
- **Missing files are silently skipped.** Do not change this behaviour — it allows users to reference files they intend to create later.
- **Trailing commas and case-insensitive property names are always allowed** in JSON input. Do not tighten deserialization options.

## Changing the UI (template.html)

`template.html` is a drop-in replacement — it must contain exactly `{{content}}`, `{{footer}}`, and `{{timestamp}}` as placeholders. The `.tabs` wrapper div is required. The CSS radio-button tab selector must be preserved:

```css
.tabs > input[type="radio"]:checked + label + div { display: block; }
```

External CSS is acceptable (CDN links). Avoid JavaScript beyond the existing title-sync script. Keep the file self-contained — no separate stylesheets or scripts on disk.

See `specs/ui/design-prompt.md` for the original UI design brief.

## Adding new default bookmark files

1. Add a private static method returning `BookmarksFileContent` in `Defaults.cs`.
2. Register the new file path and add it to the appropriate tab in `EnsureUserConfig()`.
3. Write the corresponding JSON file to `%USERPROFILE%` for your local dev environment (since `EnsureUserConfig` only runs when `~/.bookmarker.json` is absent).

## Changing the data model

- `BookmarksFileContent`, `BookmarksGroup`, `BookmarkSet`, `Bookmark` are all deserialized from user files. Additive changes (new optional properties) are safe. Renaming or removing properties is a breaking change for existing user configs.
- `BookmarkerOptions` / `BookmarkerTabsOptions` are deserialized from `~/.bookmarker.json`. Same rule applies.
