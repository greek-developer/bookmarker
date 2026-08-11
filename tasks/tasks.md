# Tasks

Running checklist. Add items as they come up; tick them as they complete.

## Done

- [x] Align the repository to the grdev Agentic standard — `AGENTS.md` synced from `agentic`,
      project specifics moved to `BRIEF.md`. Specs: all.
- [x] Emit `ProductionVersion.json` at build time and surface it in the footer and at
      `GET /api/diagnostics/version`. Specs: `diagnostics.md`, `page.md`.
- [x] Add a unit test project covering the dual-format JSON converters. Specs: `bookmark-files.md`.

## Open

- [ ] Consider flattening the hierarchy — `Groups` is often skipped or used once per section, so
      Tab → Section → Set may cover every real case. Specs: `bookmark-files.md`.
- [ ] Document the 0.5.0 config-path move (`%USERPROFILE%` → `C:\Program Files\Bookmarker\`) as a
      migration note for users upgrading from an earlier version. Specs: `configuration.md`.
- [ ] `lastRead` is a module-level variable that is only ever written and passed straight into
      `Render()`; capture it locally in `BuildHtml()` instead. Specs: none — internal.
