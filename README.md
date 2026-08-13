# Bookmarker

Bookmarker is a self-hosted start page. Run it once, point your browser at it, and use it all day to navigate to the tools, environments, and services you actually need.

The core idea is that **bookmark files are just JSON files — they live in your projects, not in your browser**. Every project or repository you work on can carry its own `.bookmarker.*.json` file alongside the code, tracked in git like any other file. You then compose your personal Bookmarker page by picking which files to include from whichever projects you are currently working on. Switch projects, update the list, refresh — your start page reflects exactly what you need right now, nothing more.

No database, no account, no sync, no browser extension.

## Related

- [Blog post](https://greekdeveloper.com/posts/2026/bookmarker/) — write-up on the motivation and design

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — how the app is built
- [UI Design Brief](docs/ui/design-prompt.md) — the brief the interface was designed from
- [BRIEF.md](BRIEF.md) — building and running it yourself

## Install as a Windows Service

Download the latest `bookmarker-vX.X.X-win-x64.zip` from [Releases](https://github.com/greek-developer/bookmarker/releases), extract it, then run from an elevated PowerShell prompt:

```powershell
.\install-on-program-files.ps1
```

This copies the files to `C:\Program Files\Bookmarker\` and registers a Windows Service set to start automatically.

To remove the service:

```powershell
sc.exe stop Bookmarker
sc.exe delete Bookmarker
```

## Changing the port

Edit `appsettings.json` before installing the service:

```json
{
  "Urls": "http://*:5069"
}
```

Change `5069` to any free port. The `*` binds to all interfaces, which is required when running as a Windows Service. If the service is already running, stop it, edit the file, and start it again.

## Configuring the config file path

By default Bookmarker looks for `.bookmarker.json` at `C:\Program Files\Bookmarker\.bookmarker.json`. This works correctly whether the app runs interactively or as a Windows Service, since the path does not depend on which user account the process runs under.

To store the config somewhere else — a dotfiles repository, say, or a folder you can edit without administrator rights — create `appsettings.local.json` next to `Bookmarker.exe` and set the path there:

```json
{
  "BookmarkerConfigPath": "D:\\dotfiles\\.bookmarker.json"
}
```

`appsettings.local.json` is optional, is loaded after `appsettings.json`, and wins where the two disagree. Unlike `appsettings.json` it is not part of the app, so **reinstalling or upgrading leaves it untouched** — settings you want to keep across updates belong here. Restart the service after changing it.

The same key still works in `appsettings.json` itself, but an update overwrites that file.

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

## Versioning

Versions are computed by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
from [`version.json`](version.json) plus the git height — there is no hardcoded version anywhere.
`version.json` holds the `major.minor`; the patch is the number of commits since that value last
changed, so **every commit bumps the patch automatically**.

Install the CLI once:

```powershell
dotnet tool install --global nbgv
```

### Viewing the version

```powershell
nbgv get-version                    # full summary for HEAD
nbgv get-version -v SimpleVersion   # just x.y.z, for scripts
nbgv get-version -f json            # everything, as JSON
```

### Setting the version

The patch bumps on its own with every commit. To change the major or minor, hand-edit the
`version` field in `version.json` and commit it — the patch count restarts from there:

```json
"version": "1.3"
```

Do **not** run `nbgv set-version`. It rewrites `version.json` from scratch and silently drops the
`publicReleaseRefSpec` and `cloudBuild` settings this repo relies on. Never add a `<Version>`
element to `Directory.Build.props` or a `.csproj` either — it would override the computed version.

### Releases

A build from `release/production` is a public release and gets a clean version (`1.3.4`). Every
other branch is a prerelease and gets a commit-id suffix (`1.3.4-g1a2b3c4`). Pushing to
`release/production` triggers the [release workflow](.github/workflows/release.yml).
