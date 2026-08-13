# Configuration

How Bookmarker finds its configuration, what that configuration contains, and what happens on a
first run.

## Config location

Bookmarker reads a single root config file, `.bookmarker.json`. Its location resolves in this
order:

1. A `BookmarkerConfigPath` command-line argument or environment variable, when non-empty.
2. The `BookmarkerConfigPath` setting in `appsettings.local.json`, when non-empty.
3. The `BookmarkerConfigPath` setting in `appsettings.json`, when non-empty.
4. Otherwise `C:\Program Files\Bookmarker\.bookmarker.json`.

The default sits next to the executable rather than in `%USERPROFILE%` so the path is identical
whether the app runs interactively or as a Windows Service. A service runs under `SYSTEM` or
`LOCAL SERVICE`, whose home directory is not the developer's, so a profile-relative path would
resolve somewhere invisible.

## Local settings

`appsettings.local.json` sits beside `appsettings.json` in the install directory and holds
settings belonging to one machine. It is optional — absent, nothing changes — and it is loaded
behind every other settings file, so any setting it names beats `appsettings.json`. It is not
loaded last: environment variables and command-line arguments still override it, which keeps the
usual one-off `--Urls=…` working against a machine that has a local file.

It is neither shipped with the app nor committed to the repository. That is the whole point:
installing copies over the top of the existing folder, so `appsettings.json` is replaced on every
update while `appsettings.local.json` is left alone. A setting that must survive an upgrade
belongs there.

```json
{
  "BookmarkerConfigPath": "D:\\dotfiles\\.bookmarker.json"
}
```

Changes take effect on the next start; the file is read once at startup and is not watched.

Unlike the bookmark files, it is parsed strictly. The host loads it before the app exists, so a
syntax error stops the process rather than rendering an error page — the reading tolerance below
covers `.bookmarker.json` and content files only.

## Root config

The root config defines the tabs and, for each, the ordered list of content files that make it up:

```json
{
  "Tabs": [
    { "Name": "Work", "Files": ["C:\\proj\\.bookmarker.work.json"] },
    { "Name": "Personal", "Files": ["C:\\Users\\you\\.bookmarker.personal.json"] }
  ]
}
```

Each entry in `Files` is an absolute path. A tab may list several files; each renders as its own
section within that tab.

**Missing files are silently skipped.** Referencing a file that does not exist is not an error —
it lets a user list a file they intend to create, or keep a reference to a project drive that is
not currently mounted.

## Reading tolerance

Config and content files are hand-edited, so parsing is deliberately forgiving:

- Property names match case-insensitively.
- Trailing commas are accepted.

A file that cannot be parsed at all does not take the page down. A root config that fails to parse
renders an error page naming the file and the parser message. A content file that fails to parse
renders in place as a section titled `[parse error] <filename>: <message>`, leaving every other
section intact.

## First run

When the root config file does not exist, Bookmarker creates it along with a set of example
content files in the same directory:

| File | Purpose |
|---|---|
| `.bookmarker.json` | Root config referencing the companion files |
| `.bookmarker.welcome.json` | Example content |
| `.bookmarker.social.json` | Example content |
| `.bookmarker.google.json` | Example content |

Once the root config exists these files are never overwritten, and the first-run step does nothing
on subsequent starts. Users edit them freely.

## Port

The listening port comes from the standard ASP.NET Core `Urls` setting in `appsettings.json`.
Binding to all interfaces (`http://*:5069`) is required when running as a Windows Service.
