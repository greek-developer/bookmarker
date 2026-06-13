# Project Brief — Bookmarker

*Written for: blog post and short YouTube video. Contains everything needed without re-reading the code.*

---

## 1. Elevator Pitch

Bookmarker is a self-hosted start page you run locally and point your browser at. Your bookmarks live in plain JSON files — one per project or context — and you compose your personal page by picking which files to show. It is a tool for developers who spend their day jumping between internal tools, dashboards, staging environments, and documentation and want a single, keyboard-accessible place to go.

---

## 2. The Problem It Solves

Browser bookmarks are global. They don't know which project you're working on, they can't be checked into a git repository alongside the code, and reorganising them every time you switch focus is friction you learn to live with rather than fix.

The core insight here is that **a bookmark file is just a data file**. There's no reason it has to live in the browser. If it's a JSON file in your project repo, it can be versioned, shared with the team, and swapped in or out as you move between projects. You update a config file and hit refresh — your start page now reflects exactly what you need, nothing more.

---

## 3. Tech Stack

| Technology | Role | Why / What's notable |
|---|---|---|
| **ASP.NET Core minimal API (.NET 10)** | Web server and renderer | Chosen for zero-ceremony setup: no controllers, no views engine, no EF. The entire HTTP layer is ~20 lines. |
| **System.Text.Json** | JSON parsing | Ships with .NET, no extra dependency. A custom dual-format converter handles both `"Name=https://url"` shorthand and full object syntax. |
| **`template.html`** | UI shell | A single self-contained HTML file with inline styles. The server reads it from disk on every cache miss, so UI changes are live without a restart. |
| **CSS-only tabs** | Tab navigation | Uses the radio-button trick — no JavaScript for tab switching at all. |
| **`localStorage`** | Tab persistence | One small inline `<script>` saves and restores the active tab across page loads. The only JavaScript in the project. |
| **Microsoft.Extensions.Hosting.WindowsServices** | Background service | One NuGet package and one `builder.Host.UseWindowsService()` call turns the app into a proper Windows Service. |
| **Fira Code (Google Fonts CDN)** | Typography | Monospace throughout, loaded from CDN. The entire design direction is terminal/TUI aesthetic. |

**Non-obvious detail**: the `template.html` UI was designed by giving a detailed written design brief to an AI (the brief is in `specs/ui/design-prompt.md`). The brief described the fixed HTML structure the backend emits and the visual direction; the AI produced the full CSS. The design brief is committed to the repo as a first-class spec document.

---

## 4. Key Technical Decisions

### 1. Template file on disk, not an embedded string

**Decision**: HTML lives in `template.html`, read from disk at runtime.  
**Alternative**: build the full page in C# as a string literal, or use a view engine like Razor.  
**Why**: A file on disk means you can change the UI and bust the cache via `POST /refresh` without recompiling or restarting the process. Razor would be overengineered for a single page with no dynamic routing. A C# string literal would make the HTML unmaintainable.

### 2. CSS-only tab switching

**Decision**: Tabs are implemented with `<input type="radio">` + `<label>` triplets and the CSS adjacent-sibling selector `.tabs > input:checked + label + div { display: block; }`.  
**Alternative**: A small JavaScript event listener toggling a `.active` class.  
**Why**: No JavaScript means the page works even if the script block is stripped, and there is no flash of unstyled content on load. The flexbox `order` property is used to visually group the labels into a row while the content `<div>` stays in DOM order after them — the CSS trick requires DOM order but the visual order can differ freely.

### 3. Config path at `C:\Program Files\Bookmarker\` instead of `%USERPROFILE%`

**Decision**: Default config location is next to the executable in `C:\Program Files\Bookmarker\`.  
**Alternative**: `%USERPROFILE%\.bookmarker.json` (used in earlier versions, as seen in the git history).  
**Why**: When the app runs as a Windows Service it runs under the `SYSTEM` or `LOCAL SERVICE` account, which has its own `%USERPROFILE%` — not the developer's home directory. The config would be invisible or missing. Moving the default to a fixed path next to the exe solves this without requiring any configuration. This was a breaking change made at version 0.5.0.

### 4. Two-level config: root file references content files

**Decision**: `~/.bookmarker.json` only defines tabs and lists absolute paths to content files. Content (the actual bookmarks) lives in separate files.  
**Alternative**: A single monolithic config file with all bookmarks.  
**Why**: The separation allows individual projects to own their bookmark files. A `myproject/.bookmarker.myproject.json` file can live in that repo, be referenced from the root config, and be removed from the tab list when you stop working on that project. It also means you can share a team bookmark file via a shared drive or git submodule without merging it into your personal config.

---

## 5. Interesting Problems Solved

### The dual-format JSON converter

Bookmark entries can be written two ways:

```json
"Grafana=https://grafana.internal"
```
```json
{ "name": "Grafana", "url": "https://grafana.internal", "bookmarks": [...] }
```

The string shorthand exists because manually editing a JSON file of 30+ links is a common task and the object syntax adds a lot of noise. A custom `BookmarkJsonConverter` handles both. The tricky part is that `BookmarkSet` (a set with related pipe-separated links) inherits from `Bookmark`, and `[JsonConverter]` is not inherited in System.Text.Json — so `BookmarkSet` has its own `[JsonConverter(typeof(BookmarkSetJsonConverter))]` attribute, which delegates back to the base converter passing `typeof(BookmarkSet)` as the type hint. It's a small but non-obvious System.Text.Json gotcha.

### The CSS flexbox ordering trick for tabs

The backend emits a flat sequence of `radio → label → div` triplets — they must stay in that DOM order for the `+` adjacent-sibling selector to work. But visually, all the labels need to appear in one row above the content. The solution: every `<label>` gets `order: 0` and the content `<div>` gets `order: 1` inside a flex container. The browser reorders them visually while the DOM order (and therefore the CSS selector logic) stays intact.

### Windows Service account vs user home directory

Early versions stored config in `%USERPROFILE%`. When running as a Windows Service, `%USERPROFILE%` resolves to the service account's profile (`C:\Windows\system32\config\systemprofile`), not the developer's. The app would silently write default files there and find nothing useful. The fix — moving the default to the directory next to the exe — is simple but required recognising that `%USERPROFILE%` is not a stable concept across Windows execution contexts.

### In-memory HTML caching with one-line invalidation

The rendered HTML is cached as a `string?` module-level variable. `null` means stale. The `POST /refresh` (and now `GET /refresh`) sets it to `null`; the next `GET /` rebuilds. No cache keys, no expiry, no locking (it's a single-user local tool). The elegance is that the template file is re-read from disk on every cache miss, so a UI change takes effect immediately after a refresh without touching the app process.

---

## 6. Demo Flow

*Narrate this as a screen recording.*

1. **Open the browser** at `http://localhost:5069`. The page loads instantly — dark background, monospace font, terminal aesthetic. The app title is in the top left.

2. **See the tabs** across the top — "Welcome", "Google" (default first-run content). The active tab has a cyan underline. Click between tabs — the switch is instant with no page load, no JavaScript event firing, just CSS.

3. **Scan a section** — sections are collapsible `<details>` blocks with a `[+]/[-]` toggle. Click the summary row to collapse or expand. Groups inside a section are also collapsible with a `>` indicator.

4. **Click a link** — opens in a new tab. Related links (Dev / Staging / Prod etc.) appear on the same row separated by pipes, slightly dimmer than the primary link.

5. **Edit a JSON file** — open `C:\Program Files\Bookmarker\.bookmarker.welcome.json` in any text editor, add a new bookmark, save.

6. **Refresh without restarting** — click the `↻` button in the footer (or navigate to `/refresh`). The page reloads with the new content. No server restart.

7. **Show the root config** — open `.bookmarker.json`, add a path to a new content file from another project. Refresh. A new section appears. Remove the path. Refresh. Gone. The start page now reflects exactly what's needed.

8. **Footer** shows the config file path (lowercased) and the last-read timestamp.

---

## 7. What I'd Do Differently

**The `%USERPROFILE%` → `Program Files` config path change was a breaking change handled silently.** Existing users who had already set up their config at `~/.bookmarker.json` would find the app ignoring it after upgrade. There was no migration, no warning, and no documentation of the change in the commit message. A version bump with a clear migration note would have been the right call.

**The `BookmarksGroup` level may be one level too many.** The hierarchy is Tab → Section (file) → Group → Set → Bookmarks. In practice most uses skip the group level entirely or use a single group per section. Flattening to Tab → Section → Set might have been simpler and still covered all real use cases.

**There is no way to add bookmarks from the UI.** Everything requires editing a JSON file in a text editor. For a developer tool this is probably fine and intentional, but it does mean the app has zero discoverability for non-developer users.

**`lastRead` is a module-level variable that is only ever written, never read back elsewhere.** It gets passed into `Render()` as the `readAt` parameter for the timestamp, but the indirection through the module-level variable is unnecessary — `DateTime.Now` could be captured locally in `BuildHtml()`.

---

## 8. Potential Angles

### Blog post titles

1. **"I replaced my browser bookmarks with a JSON file and a 150-line C# app"** — Personal, opinionated, explains the core idea. Good hook for the developer productivity audience.

2. **"The CSS radio button tab trick: zero JavaScript, no framework, just HTML"** — Technical deep-dive into the CSS-only tab mechanism. Standalone value even for readers who don't care about Bookmarker.

3. **"Why your ASP.NET Core app doesn't need a view engine"** — Uses Bookmarker as a case study for minimal server-side rendering: StringBuilder, a template file, string replacements. Argues against Razor for simple use cases.

### YouTube video titles

1. **"Building a developer start page with .NET in under 200 lines of code"** — Live-build angle. Shows the minimal API, the JSON loading, and the rendering pipeline from scratch.

2. **"CSS tabs without JavaScript — the trick every frontend dev should know"** — Focused on the radio button trick. Broad appeal beyond .NET developers.

3. **"Your project should own its bookmarks (here's how)"** — Concept-first video explaining the per-project JSON file idea, then showing the app as the implementation. Softer sell, wider audience.
