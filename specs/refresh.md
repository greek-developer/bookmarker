# Refresh

How edits to bookmark files reach the page.

## Caching

The rendered page is built on the first request and held in memory. Every subsequent request
serves that same HTML, so the app does no file I/O in the steady state.

There is no expiry and no cache key. The cache is either present or stale, and only an explicit
refresh makes it stale. This is a single-user local tool; concurrent-access behavior is not a
concern.

## Refreshing

`POST /refresh` and `GET /refresh` both discard the cached page and redirect to `/`, which rebuilds
it. The rebuild re-reads:

- the root config,
- every content file it references,
- and `template.html` itself.

The refresh control in the page footer issues the `POST`.

Because the template is re-read too, a change to the page's markup or styling takes effect on
refresh **without restarting the process**. A change to application code does not — that requires a
restart.

## What a user does

1. Edit a bookmark file, or add a path to a tab in the root config.
2. Click the refresh control in the footer.
3. The page reflects the change.

Adding a path to a file that does not exist yet is harmless — it is skipped until the file appears,
at which point the next refresh picks it up.
