# Diagnostics

How a running Bookmarker reports which build it is.

## Why

Bookmarker is installed as a Windows Service and then largely forgotten. The question that
actually gets asked is "is the machine running the build I think it is?" — so the running app
carries its own identity rather than relying on the installer's memory.

## The production version file

The build emits `ProductionVersion.json` next to the application. It is generated, never
hand-edited, and captures the exact commit the build was cut from:

```json
{
  "version": "0.8.42",
  "commit": {
    "message": "add table layout",
    "sha": "f0ba4ea..."
  },
  "build": {
    "time": "2026-08-11T09:14:22Z"
  }
}
```

| Field | Value |
|---|---|
| `version` | The computed version — `version.json` major/minor plus git height |
| `commit.message` | Subject of the commit the build was cut from |
| `commit.sha` | Full SHA of that commit |
| `build.time` | Build timestamp, UTC, ISO 8601 |

The file is read once at startup. When it is absent — a local `dotnet run` from a source tree that
was never published, for example — the app still starts and reports its identity as unknown rather
than failing.

## Where it surfaces

**The page footer** shows the identity as:

```
{version} - {commit-sha} - {build-time}
```

`{commit-sha}` is the first 8 characters of the commit SHA; the full SHA stays in the JSON
and at `GET /api/diagnostics/version`.

**`GET /api/diagnostics/version`** returns the contents of `ProductionVersion.json` as JSON. This
is what a health check or a deployment script reads; it needs no page parsing and no browser.
