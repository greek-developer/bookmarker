# UI Design Brief — Bookmarker

## What is Bookmarker?

Bookmarker is a self-hosted ASP.NET Core app that serves a single HTML page: a personal or team bookmark dashboard. The page is generated server-side by injecting dynamic HTML into a file called `template.html`. Your job is to redesign that file.

The use case is a **developer start page** — opened in the browser and used throughout the workday to quickly navigate to internal tools, environments, dashboards, and documentation. Readability and speed of scanning are the top priorities.

---

## How the template works

The server replaces exactly two placeholders in `template.html` before sending the page:

- `{{content}}` — the full tab/section/link structure (see HTML shape below)
- `{{footer}}` — a plain file path string, e.g. `c:\users\thomas\.bookmarker.json`

Everything else in the file is static. You own all of it: structure, styles, external CSS links, whatever is needed.

---

## The generated HTML shape

The server injects the following fixed HTML structure into `{{content}}`. **You cannot change this structure** — it is produced by the backend. Your CSS must style it as-is.

```html
<!-- One block per tab -->
<input type="radio" id="0" name="tabs" checked>
<label for="0"> Tab Name </label>
<div>

  <!-- One <details> per bookmark file/section, first one is open -->
  <details open>
    <summary><h2>Section Title</h2></summary>
    <div>

      <!-- Groups: either bare (no name) or wrapped in a named <details> -->
      <details open>                          <!-- only present if group has a name -->
        <summary><h3>Group Name</h3></summary>
        <div>
          <ul>

            <!-- A set: main link + optional pipe-separated related links -->
            <li>
              <a href="https://..." target="_blank">Main Link</a>
               | <a href="https://..." target="_blank">Related 1</a>
               | <a href="https://..." target="_blank">Related 2</a>
            </li>

            <!-- A set with no URL renders as a plain label -->
            <li>
              Label Text:
            </li>

          </ul>
        </div>
      </details>

    </div>
  </details>

</div>

<!-- Second tab (not checked) -->
<input type="radio" id="1" name="tabs">
<label for="1"> Another Tab </label>
<div>
  ...
</div>
```

The tab mechanism relies on the CSS pattern:

```css
/* show content when the radio before the label is checked */
.tabs > input[type="radio"]:checked + label + div { display: block; }
```

This works because the structure is a flat sequence of `radio → label → div` triplets inside `.tabs`. The `.tabs` container wraps all of them. **This CSS-only tab approach must be preserved** — no JavaScript tab switching.

---

## Design requirements

### Must-haves
- Drop-in replacement for `template.html` — a single self-contained file
- The `{{content}}` and `{{footer}}` placeholders must be present and in the right positions
- CSS-only tabs (the radio button trick above must keep working)
- Works offline / on localhost — any external CDN links are acceptable but must be from well-known, reliable sources (e.g. Google Fonts, cdnjs, unpkg)
- Readable at a glance — this is a dense link dashboard, not a marketing page
- Responsive enough to be usable at normal desktop widths (1200px+); mobile is not a priority

### Nice-to-haves
- Compact but not cramped — high link density without feeling cluttered
- Clear visual hierarchy: tabs → sections → groups → links
- The pipe-separated related links (`| Dev | Staging | Prod`) should be visually distinct from the main link but still easy to click
- `<details>/<summary>` open/close triangles should feel intentional, not browser-default
- Visual direction: **monospace / terminal feel, clean** — think a well-designed developer tool or TUI, not a consumer app. Monospace font throughout, dark or near-dark background, muted color palette with one clear accent (e.g. a green, amber, or cyan that echoes classic terminal colors). Clean means generous whitespace within the density constraints — no decorative chrome, no gradients, no shadows beyond what aids readability

### Hard constraints
- **No JavaScript** (or absolute minimum — one small inline script is acceptable only if there is genuinely no CSS-only alternative for a specific polish detail)
- **Minimal external dependencies** — at most one CSS framework or utility library (e.g. a small reset or a single Google Font), no full UI frameworks
- The `<footer>` element should display `{{footer}}` and a copyright line; keep it unobtrusive

---

## Delivery

Provide a single `template.html` file. No other files. All styles must be either inline in a `<style>` block or loaded from a CDN `<link>`. The file must work by dropping it into `src\Bookmarker\template.html` with no other changes to the project.

Optionally include a short note at the top of the file (HTML comment) explaining any non-obvious CSS choices.
