# Bookmarker

Bookmarker is a small ASP.NET Core web app that renders a bookmark dashboard from JSON files. It serves a single HTML page at `/` and builds the page content from files listed in `src\Bookmarker\bookmarks\.bookmarker.json`.

## What it does

- Loads a bookmark page configuration from `src\Bookmarker\bookmarks\.bookmarker.json`
- Reads one or more bookmark data files per top-level page
- Renders the result into `src\Bookmarker\template.html`
- Serves the generated HTML from the root route: `GET /`

The UI uses:

- **tabs** for top-level pages such as `Ariadne` and `RGS`
- **collapsible sections** for each bookmark file
- **nested collapsible groups** when a group has a name
- **links on one line** for a main URL plus additional related URLs

## Requirements

- .NET SDK 10.0

## Run locally

From the repository root:

```powershell
dotnet build .\grdev.bookmarker.sln
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

The development launch profile runs on:

- `http://localhost:5069`
- `https://localhost:7144`

If you run with the default launch settings, open:

```text
http://localhost:5069/
```

## Project structure

```text
grdev.bookmarker.sln
src\Bookmarker\
  Program.cs
  template.html
  Models\
    Bookmark.cs
    BookmarkerOptions.cs
  bookmarks\
    .bookmarker.json
    ariadne\
    rgs\
```

## Configuration format

### 1. Root configuration

`src\Bookmarker\bookmarks\.bookmarker.json` defines the top-level tabs and the bookmark files that belong to each tab.

Example:

```json
{
  "Tabs": [
    {
      "Name": "Ariadne",
      "Files": [
        "bookmarks/ariadne/ariadne.bookmarks.json",
        "bookmarks/ariadne/ariadne.development.bookmarks.json",
        "bookmarks/ariadne/ariadne.staging.bookmarks.json"
      ]
    }
  ]
}
```

Properties:

- `Tabs[]` - top-level tabs
- `Tabs[].Name` - tab label
- `Tabs[].Files[]` - relative paths to bookmark content files

## 2. Bookmark content files

Each file listed in `Files[]` is deserialized into this shape (`BookmarksFileContent`):

```json
{
  "name": "Ariadne",
  "groups": [
    {
      "name": "Infrastructure",
      "sets": [
        {
          "name": "Logs",
          "url": "http://localhost:5000"
        },
        {
          "name": "Projects",
          "bookmarks": [
            "Project 1=http://localhost:5000/project1",
            "Project 2=http://localhost:5000/project2"
          ]
        }
      ]
    }
  ]
}
```

Properties:

- `name` - section title shown as the file header (`BookmarksFileContent.Name`)
- `groups[]` - logical groups within that section (`BookmarksGroup[]`)
- `groups[].name` - collapsible group title; if empty, the group content is shown without an extra wrapper
- `groups[].sets[]` - bookmark entries within the group (`BookmarkSet[]`)
- `sets[].name` - label text
- `sets[].url` - main clickable link; if omitted or empty, only the label is shown
- `sets[].bookmarks[]` - extra named links rendered after the main label; each entry is either a `"name=url"` string or an object with `name` and `url` properties

## Rendering behavior

The current implementation in `Program.cs` behaves as follows:

- The app only exposes `GET /`
- Missing files listed in `.bookmarker.json` are skipped
- JSON is read with case-insensitive property matching
- Trailing commas in JSON are allowed
- The first tab, first file section, and first named group are opened by default
- The footer shows the full path to the active root configuration file

## Notes

- `appsettings.json` is currently only used for standard ASP.NET Core logging settings
- The page is generated server-side from string replacement in `template.html`
- The bookmark data lives under `src\Bookmarker\bookmarks\`
- The sample configuration references `ariadne.development.bookmarks.json`, but the app tolerates that file being absent because non-existent files are filtered out
