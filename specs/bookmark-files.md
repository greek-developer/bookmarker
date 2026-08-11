# Bookmark files

The format of a content file — the unit a user actually edits, and the unit a project repository
can own and version alongside its code.

## Shape

One content file renders as one collapsible section:

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

## Hierarchy

```
Tab                 ← named in the root config
  └─ Section        ← one content file, titled by its `name`
       └─ Group     ← groups[].name — collapsible when named
            └─ Set  ← sets[].name + optional url
                 └─ Related bookmarks ← sets[].bookmarks[]
```

| Property | Controls |
|---|---|
| `name` (file level) | The section heading inside the tab |
| `groups[].name` | Group heading; when empty the group renders without a wrapper and without a collapsible header |
| `groups[].layout` | `table` lays the group out as an aligned grid; anything else (or absent) renders a list |
| `sets[].name` | The link label |
| `sets[].url` | The primary link. When omitted the name renders as plain text — useful as a row label |
| `sets[].bookmarks[]` | Related links shown on the same row |

## Two ways to write a bookmark

Any bookmark — a set or a related link — accepts either form:

```json
"Label=https://url.com"
```
```json
{ "name": "Label", "url": "https://url.com" }
```

The shorthand splits on the first `=`, so a URL containing `=` in its query string is preserved.
The shorthand exists because hand-editing thirty links in object syntax is mostly punctuation.

A set written in shorthand has a name and a URL and no related links; the object form is what
carries `bookmarks`.

## Layouts

**List** (the default) — each set is a row: the primary link, then its related links after a pipe
separator, dimmer than the primary.

**Table** (`"layout": "table"`) — each set is a grid row, one cell per link, columns aligned across
the whole group. The column count is set by the widest row in the group.

## Ownership

A content file is a normal file on disk with an absolute path, which is the point: a project
repository can carry `.bookmarker.<project>.json` next to its code, tracked in git, shared with the
team. Adding the project means adding one path to a tab; dropping it means removing that path.
