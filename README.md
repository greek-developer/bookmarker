# Bookmarker

Bookmarker is a self-hosted start page. Run it once, point your browser at it, and use it all day to navigate to the tools, environments, and services you actually need. All your links live in plain JSON files in your home directory — no database, no account, no sync.

## Documentation

- [Architecture](specs/ARCHITECTURE.md)

- specs/[UI Design Brief](specs/ui/design-prompt.md)

## Requirements

- .NET SDK 10.0

## Run

```powershell
dotnet run --project .\src\Bookmarker\Bookmarker.csproj
```

Then open `http://localhost:5069/` in your browser.

## Install as a Windows Service

Download the latest `bookmarker-vX.X.X-win-x64.zip` from [Releases](https://github.com/greek-developer/bookmarker/releases), extract it, then run from an elevated PowerShell prompt:

```powershell
.\install-service.ps1
```

To remove the service:

```powershell
.\install-service.ps1 -Uninstall
```

## Changing the port

Edit `appsettings.json` before installing the service:

```json
{
  "Urls": "http://localhost:5069"
}
```

Change `5069` to any free port. If the service is already running, stop it, edit the file, and start it again.

## Configuring the config file path

By default Bookmarker looks for `.bookmarker.json` at `C:\Program Files\Bookmarker\.bookmarker.json`. This works correctly whether the app runs interactively or as a Windows Service, since the path does not depend on which user account the process runs under.

To store the config somewhere else, set the path explicitly in `appsettings.json`:

```json
{
  "BookmarkerConfigPath": "C:\\Users\\yourname\\.bookmarker.json"
}
```

Leave the value empty (or omit the key entirely) to use the default next-to-exe location.

## On first run the app creates these files in `C:\Program Files\Bookmarker\`:

| File | Purpose |
|---|---|
| `.bookmarker.json` | Root config — defines tabs and which files belong to each |
| `.bookmarker.welcome.json` | Example bookmark content file |
| `.bookmarker.social.json` | Example bookmark content file |
| `.bookmarker.google.json` | Example bookmark content file |

These files are never overwritten once created. Edit them freely.

## How to organise your bookmarks

### The two-level config

**`~/.bookmarker.json`** controls the top-level structure:

```json
{
  "Tabs": [
    {
      "Name": "Work",
      "Files": [
        "C:\\Users\\you\\.bookmarker.work.json",
        "C:\\Users\\you\\.bookmarker.work-staging.json"
      ]
    },
    {
      "Name": "Personal",
      "Files": [
        "C:\\Users\\you\\.bookmarker.personal.json"
      ]
    }
  ]
}
```

Each entry in `Files` is an absolute path to a bookmark content file. Missing files are silently skipped, so you can safely reference files you haven't created yet.

**Each content file** maps to one collapsible section inside a tab:

```json
{
  "name": "Infrastructure",
  "groups": [
    {
      "name": "Monitoring",
      "sets": [
        {
          "name": "Logs",
          "url": "https://logs.example.com",
          "bookmarks": [
            "Dev=https://logs.dev.example.com",
            "Staging=https://logs.staging.example.com"
          ]
        },
        "Dashboard=https://dashboard.example.com"
      ]
    }
  ]
}
```

### What each property does

| Property | What it controls |
|---|---|
| `name` (file level) | Section heading inside the tab |
| `groups[].name` | Collapsible group heading; omit for an ungrouped list |
| `sets[].name` | Link label |
| `sets[].url` | Primary link; omit to render as a plain text label |
| `sets[].bookmarks[]` | Extra links on the same row, shown after a pipe separator |

A bookmark entry in `bookmarks[]` can be written in two ways:

```json
"Label=https://url.com"
```
```json
{ "name": "Label", "url": "https://url.com" }
```

### How the JSON structure maps to the UI

```
Tab                 ← Tabs[].Name
  └─ Section        ← one content file (files[].name)
       └─ Group     ← groups[].name  (collapsible if named)
            └─ Row  ← sets[].name + url
                 └─ Related links  ← sets[].bookmarks[]
```

A tab can contain multiple files; each file renders as its own collapsible section inside that tab. This lets you split a large tab into logical files without creating a new tab.

## Refreshing

The page is cached in memory after the first load. If you edit a bookmark file and want to see the changes, click the **↻** button in the footer — it re-reads all files without restarting the app.
