# The page

What a user sees at `GET /` and how they move around it.

## Layout

A single page, monospace throughout, dark, in a terminal/TUI visual register. From top to bottom:
the app title, a row of tab labels, the active tab's content, and a footer.

## Tabs

Every tab in the root config gets a label in the row at the top. The first tab is active on load.
Selecting a label swaps the content instantly — no page load, no network request.

Tab switching involves **no JavaScript**. It is a sequence of radio input, label and content
triplets, and a CSS adjacent-sibling rule that shows the checked one. The page therefore has no
flash of unstyled content and keeps working if scripting is unavailable.

The active tab is reflected in the document title as `bookmarker: <tab name>`.

The active tab persists across page loads, so a refresh returns the user to the tab they were on.

## Sections and groups

Each content file in the active tab renders as a section with its `name` as the heading. Sections
are collapsible and **open by default on every load** — collapsing is a per-visit convenience, not
a stored preference.

A named group renders as a nested collapsible heading inside its section. An unnamed group renders
its links directly, with no heading and no collapse control.

## Links

Every link opens in a new tab. A set's related links sit on the same row as the primary link,
visually subordinate to it. A set with no URL renders as a plain text label — a way to caption a
row rather than link it.

## Footer

The footer shows, in order:

- The path of the root config file in use, lowercased — so it is always obvious which file the page
  was built from.
- The time the bookmark files were last read, as `yyyy/MM/dd HH:mm`.
- The running build's identity: `{version} - {commit-sha} - {build-time}`.
- A refresh control.

## Error page

When the root config cannot be parsed the page is replaced by a plain error page naming the file
and the parser's message, rather than a stack trace or an empty page. A content file that fails to
parse does not reach this path — it degrades to an error-titled section and the rest of the page
renders normally.
