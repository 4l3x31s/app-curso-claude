# Page notifications and new pages

## Objective

Show a notification on every page and provide three pages: Home, Contact details, and Addresses.

## Problem and why

The app is the unmodified `dotnet new mvc` template. It has no notification mechanism and only the Home, Privacy, and Error pages.

## Scope and constraints

- Notification = one Bootstrap 5.3.3 toast per page, shown on load. No new packages, no server-side state.
- Shared contract: a view sets `ViewData["NotificationMessage"]` and `ViewData["NotificationType"]` (`info | success | warning | danger`); the layout renders it.
- Contact and address data are hardcoded sample data (no database).
- UI copy in English.
- `Controllers/HomeController.cs` has an uncommitted local change and is not touched by any task.
- `gh` is not installed: branches are pushed to `origin`; pull requests and merges are the user's decision.

## Delivery

Strategy: `/batch` — five independent branches from `master` (`1ea5c13`), one isolated worktree each. Forecast: well under 400 authored lines in total.

## Tasks

Route for every task: delegated (one background worker per task in an isolated worktree, per the user-invoked `/batch` flow).

- [x] T1 Notification infrastructure — `Views/Shared/_PageNotification.cshtml`, `Views/Shared/_Layout.cshtml` (one line after `</footer>`), `wwwroot/js/site.js`, `Views/Shared/Error.cshtml` — branch `feat/page-notifications`, port 5111 — commit fe878ae
- [x] T2 Home page — `Views/Home/Index.cshtml` — branch `feat/home-page`, port 5112 — commit 9fb9044
- [x] T3 Contact details page — `Controllers/ContactController.cs`, `Models/ContactDetailsViewModel.cs`, `Views/Contact/Index.cshtml` — branch `feat/contact-details-page`, port 5113 — commit caaee25
- [x] T4 Addresses page — `Controllers/AddressesController.cs`, `Models/AddressViewModel.cs`, `Views/Addresses/Index.cshtml` — branch `feat/addresses-page`, port 5114 — commit ce5efef
- [x] T5 Navigation and Privacy notification — `Views/Shared/_Layout.cshtml` (navbar `<ul>` only), `Views/Home/Privacy.cshtml` — branch `feat/nav-and-privacy-notification`, port 5115 — commit 7265326
- [x] T6 Local integration check (no push): merge the five branches into a temporary branch in a worktree, `dotnet build`, run the app, confirm the five routes return 200 and each includes its toast; remove the worktree.

## Acceptance criteria

- `/`, `/Contact`, `/Addresses`, `/Home/Privacy`, and `/Home/Error` return 200 and each renders its own toast once all branches are merged.
- The navbar links to Home, Contact details, Addresses, and Privacy.
- `dotnet build` finishes with 0 errors and no new warnings.

## Checks

- Per task: `dotnet build`; run on the task port; `curl` the task route for status 200 and page-specific text. No test project exists, so there is no RED/GREEN cycle.
- Toasts for T2–T5 are only observable in T6.

## Progress and evidence

- 2026-10-05: plan approved; workers launched.
- 2026-10-05: T1–T5 pushed to origin (no PRs: gh not installed). Each worker observed `dotnet build` 0 warnings, 0 errors and HTTP 200 on its route.
- 2026-10-05: T6 observed on a temporary octopus merge of the five branches (clean auto-merge of _Layout.cshtml): build 0 warnings, 0 errors; `/`, `/Contact`, `/Addresses`, `/Home/Privacy`, `/Home/Error` returned 200, each with exactly one toast and its own message, and 4 nav links. Toast display was checked in served HTML only, not in a browser. Temporary worktree and branch removed.
- Review: RDD native review not run; each worker ran the code-review skill. Open review notes: danger/warning toasts auto-hide after 5 s; toast overlaps the navbar toggler on mobile; toast is hidden without JavaScript.

## Next step

User opens and merges the five pull requests (any order). Agent worktrees under `.claude/worktrees/agent-*` can be removed afterwards.
