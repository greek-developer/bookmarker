# AGENTS.md

## Build & run

```powershell
dotnet build .\grdev.bookmarker.sln
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

Default URLs: `http://localhost:5069` / `https://localhost:7144`

## Project layout

```
src\Bookmarker\
  Program.cs              # single GET / endpoint + startup init
  template.html           # HTML shell with {{content}} and {{footer}} placeholders
  Models\
    Bookmark.cs           # data models and BookmarkJsonConverter
    BookmarkerOptions.cs  # root config models (tabs + file lists)
  bookmarks\              # built-in (demo) bookmark data
    .bookmarker.json      # demo root config
    ariadne\
    rgs\
```

## Configuration

On first run the app writes two files to the user's home directory:

- `~/.bookmarker.json` — root config (tabs and file paths)
- `~/.bookmarker.welcome.json` — companion bookmark content file

If those files already exist they are not touched. The built-in `bookmarks/` data is only used as fallback when no user config is present.

File paths in the root config can be **absolute** (for user-home files) or **relative to the app content root** (for built-in files).

## Bookmark data format

Two levels of config:

1. **Root config** (`BookmarkerOptions`): lists tabs, each tab lists file paths
2. **Content files** (`BookmarksFileContent`): name + groups → sets → optional nested bookmarks

Bookmarks in `sets[].bookmarks[]` accept either `"Name=URL"` shorthand or a full `{"name": "", "url": ""}` object.

## Key conventions

- Server-side rendering only — all HTML is built in `Program.cs:Render()`
- No JavaScript; tabs use the CSS radio-button trick, sections use `<details>`
- `BookmarkJsonConverter` handles both string and object formats on read; on write it emits the string shorthand for plain bookmarks and a full object when nested bookmarks are present
- Missing files listed in a tab's `Files[]` are silently skipped
- JSON deserialization is case-insensitive and allows trailing commas
