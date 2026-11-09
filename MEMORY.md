# CampusRent Project Memory

## Working conventions

- Read this file before starting project work and update it with technical findings, implementation changes, and verification results. Do not store routine conversation transcripts.
- Keep this file and session-compaction summaries in English. Reply to the user in Vietnamese.
- At the initial review on 2026-11-10 (Asia/Saigon), the repository had no `MEMORY.md`, no repository-local `AGENTS.md` found in the source inventory, and no root `.codegraph/` directory. CodeGraph was therefore not used or initialized.
- Treat current source code and EF migrations as evidence of implemented behavior. `PROJECT.md` contains older, conflicting specifications and is not a reliable description of the current runtime.

## Project overview

CampusRent is a monorepo for finding student rental rooms near universities. It supports geographic search, room details and monthly cost estimates, accounts, saved rooms, viewing appointments, listing management, reviews, violation reports, and administration. Students can also submit requests to upgrade to landlord accounts with image/PDF attachments.

- `client/`: React 18.3, Vite 6, React Router 7, Leaflet/react-leaflet, and lucide-react. Source is JavaScript/JSX. Styling is custom CSS in `client/src/index.css`, with responsive layouts and a Be Vietnam Pro font; Tailwind is not installed. HTTP calls use native `fetch`, not Axios.
- `server/`: ASP.NET Core 8 Web API, EF Core 8, SQLite and Npgsql providers, JWT bearer authentication, BCrypt password hashing, Cloudinary integration, and a background worker.
- `tests/CampusRent.Tests/`: xUnit service and HTTP integration tests, SQLite relational fixtures, an optional PostgreSQL migration/persistence test, and coverlet collection.
- `client/src/**/*.test.{js,jsx}`: Vitest and React Testing Library. `client/e2e/`: Playwright desktop/mobile workflows.
- `docs/`: implementation/testing/deployment reports, a PostgreSQL SQL export, Word/Excel project documents, UML diagrams, and survey images.
- Root operational files: `README.md`, `Dockerfile`, `compose.yaml`, `global.json`, `.config/dotnet-tools.json`, `run.ps1`, and `run.bat`.
- `server/Views/`, template files in `server/wwwroot/`, and root `db.sql` are historical scaffolding/documentation. React and API controllers form the active application.

## Frontend entry points and routes

`client/src/main.jsx` renders the application under React StrictMode and `ErrorBoundary`. `client/src/App.jsx` wraps routes in `BrowserRouter`, `AppProvider`, and the shared `Layout`.

| Route | Page | Access |
| --- | --- | --- |
| `/` | `SearchPage.jsx` | Public |
| `/rooms/:id` | `RoomDetailPage.jsx` | Public for available, unexpired rooms; API permits owners/admins to inspect their hidden listings |
| `/login`, `/register` | `AuthPage.jsx` | Authentication forms |
| `/saved` | `SavedPage.jsx` | Authenticated |
| `/bookings` | `BookingsPage.jsx` | Authenticated |
| `/account` | `AccountPage.jsx` | Authenticated |
| `/post`, `/my-rooms`, `/rooms/:id/edit` | `PostRoomPage.jsx`, `MyRoomsPage.jsx` | LANDLORD or ADMIN |
| `/admin` | `AdminPage.jsx` | ADMIN |

- `components/ProtectedRoute.jsx` waits for authentication initialization, redirects anonymous users to login with a return destination, and checks allowed roles.
- `context/AppContext.jsx` owns the user profile, authentication loading state, saved-room list, and transient notifications. It restores the profile via `/auth/me` and loads `/favorites` when a token exists.
- `lib/api.js` reads `VITE_API_BASE_URL` or defaults to `/api`, adds the bearer token, supports JSON and FormData, returns the response envelope's `data`, handles 204, and turns API/network errors into Vietnamese messages.
- The JWT is stored in `sessionStorage` under `campusrent-token`. A 401 removes it and dispatches `auth-expired`; the context clears user/saved state. The session survives reload within the same tab.
- `components/Common.jsx` provides loading/error/empty states, page titles, scroll reset, login prompts, and a modal with Escape handling, focus restoration, basic focus trapping, and body-scroll locking.
- `components/Layout.jsx` provides desktop/mobile navigation and role-aware account links. `RoomCard.jsx`, `RoomMap.jsx`, and `LocationPickerMap.jsx` are the main reusable room/map components.

## Main application flows

### Room search and maps

- `SearchPage.jsx` stores filters and pagination in URL search parameters. Changing a filter resets `page`; room requests use AbortController to discard superseded requests.
- Filters include keyword, university or arbitrary center coordinates, radius, minimum/maximum base price, amenity, and sorting. Selecting a university or point defaults the radius to 3 km.
- The location field debounces suggestions by 300 ms, supports keyboard navigation and GPS, and resolves Google place IDs when coordinates are missing.
- `RoomMap.jsx` displays OpenStreetMap tiles, price markers, room popups, and an optional search-radius circle. It fits room bounds or centers on the selected point. Clicking the map can update the search center.
- `RoomsController.Search` begins with available, unexpired rooms. SQL handles keyword and price conditions; the controller then loads matches and computes Haversine distance, applies radius/amenity filters, sorts, and paginates in memory.
- `RoomQueryParameters` defaults to page 1 and page size 12, allows page sizes up to 100, and validates coordinates and a radius of 0.1-100 km. Supported sorts are `price_asc`, `price_desc`, `distance`, and newest by default.

### Room details, costs, and listing management

- `RoomDetailPage.jsx` loads `/rooms/:id`, renders photos, landlord/university information and reviews, and offers save, appointment, review, and report actions.
- Monthly cost is `basePrice + electricPrice * kWh + waterPrice * m3 + internetPrice + otherFee`. Defaults are 80 kWh and 4 m3. The frontend `estimateCost` and backend `LivingCostService` implement this formula; the API also exposes `/rooms/:id/cost-estimate`.
- `PostRoomPage.jsx` creates/edits listings, chooses coordinates through `LocationPickerMap`, manages amenities and photos, and posts to `/rooms` or puts to `/rooms/:id`.
- `RoomsController` requires LANDLORD/ADMIN for publishing and management, and checks ownership unless the caller is ADMIN. Listings need 1-10 absolute HTTPS image URLs and at most 20 amenities. DTOs validate text lengths, coordinates, prices, area, and occupancy.
- Room statuses are AVAILABLE, RENTED, and HIDDEN. New listings expire after 30 days. Renewal is explicit through `PATCH /rooms/:id/status` with `renew=true`; ordinary editing does not renew expiry.
- `DELETE /rooms/:id` archives by changing the status to HIDDEN, preserving appointment/report history. `MyRoomsPage.jsx` loads `/rooms/mine` and manages status and renewal.

### Accounts and landlord upgrade requests

- `AuthController` normalizes email to lowercase, hashes passwords with BCrypt, prevents self-registration as ADMIN, checks duplicate emails, and keeps academic-email verification false for newly registered users.
- Registration UI and API only create STUDENT accounts. Landlord access requires an approved upgrade request; users cannot register directly as LANDLORD/ADMIN.
- `TokenService` issues HMAC-SHA256 JWTs with user ID, email, name, and role, expiring after 12 hours. Backend authorization reads the token's role claim.
- `AccountPage.jsx` updates name/phone through `/auth/me`. For STUDENT accounts it also loads `/landlord-requests/my`, uploads supporting images/PDFs, submits an upgrade request, and cancels a pending request.
- `LandlordRequestsController` accepts requests only from STUDENT accounts and rejects another pending request for the same user. Title/reason are required; up to 10 image URLs and an optional PDF reference are stored. Only the owner can cancel a pending request.
- `AdminController` lists requests, approves by setting the request to APPROVED and the user's role to LANDLORD, or rejects with an optional admin note. It also lists/searches users and updates their name, phone, role, and `IsEduVerified` flag.
- JWT validation loads the account's current database role and credential fingerprint. Approval/demotion applies to existing tokens immediately; the client refreshes profile state on focus and while an upgrade is pending. Password changes return a new JWT and invalidate tokens issued against the previous password hash.

### Saved rooms, viewing appointments, reviews, and reports

- `ActivityController` exposes `/favorites` and `/bookings` under authenticated access. Saving/removing favorites is idempotent; saved lists only return available, unexpired rooms.
- An authenticated non-owner can request an appointment from one hour to 30 days in the future. Duplicate active appointments for the same account/room are rejected. Past active appointments are completed before rebooking.
- Appointment states are string values PENDING, CONFIRMED, CANCELLED, and COMPLETED. The listing owner can confirm; either participant can cancel subject to state/time validation. `/bookings` includes appointments where the caller is the requester or listing owner.
- `ReviewsController` accepts authenticated non-owner reviews, enforces one review per user/room, and flags prohibited language through `ProfanityFilterService`. Its authorization is not limited to STUDENT despite the older specification.
- Flagged reviews are excluded from public review lists, average ratings, and review counts. Admins can approve them.
- `ReportsController` requires exactly one existing room or review target. Reports are PENDING, RESOLVED, or DISMISSED. Admin resolution can hide a room or flag a review while retaining history.
- `AutoExpireRoomsWorker` runs every 30 minutes, hides expired AVAILABLE rooms, and marks elapsed PENDING/CONFIRMED appointments COMPLETED. Public room queries check expiry immediately, independently of the worker.

## Location and upload services

- `PlacesController` exposes `/api/places/suggest`, `/resolve`, and `/reverse`, validates inputs, and uses a 30-requests/minute/IP policy.
- `PlaceSearchService` merges internal university/available-listing addresses with Google Places suggestions, falling back to Photon/OpenStreetMap when Google yields no suggestions. Results are deduplicated, capped at eight, and cached for 10 minutes.
- `GooglePlacesClient` uses Places API (New), Vietnamese language and region restriction, and a server-side configuration key. Its suggestions initially have no coordinates; `/resolve` obtains them for `google:places/...` IDs.
- `OsmPlacesClient` uses `https://photon.komoot.io/` for suggestions and reverse geocoding. Internal/OSM suggestions already contain coordinates. Both external HTTP clients use five-second timeouts.
- `LocationPickerMap.jsx` supports selecting/dragging a pin, GPS, suggestions debounced by 250 ms, and reverse-address lookup/application. Its selection handler currently only acts on suggestions that already contain coordinates; unlike the search page, it does not resolve coordinate-free Google suggestions.
- `/api/images` is LANDLORD/ADMIN-only, accepts JPG/PNG/WebP up to 5 MB, and uses Cloudinary or persistent local public-image storage. `PostRoomPage.jsx` uses this endpoint.
- `/api/uploads/image` validates verification images up to 10 MB and always stores them privately under the authenticated owner's directory.
- `/api/uploads/pdf` validates a `.pdf` extension and `%PDF-` signature up to 15 MB and stores a private UUID-named file. `/api/uploads/files/:owner/:file` requires the owner or ADMIN. `AccountPage.jsx` uses these endpoints.
- Uploads are served by controllers with ownership checks; static middleware does not expose private storage. Legacy verification documents/images are also protected. Public image URLs resolve against the configured API origin. Compose persists uploads in `uploads_data`.

## Persistence and backend startup

- `CampusRentDbContext` has nine persistent entities: User, University, Room, RoomImage, Review, Report, Booking, Favorite, and LandlordRequest.
- Main entities map to snake_case tables/columns; Booking and Favorite retain conventional `Bookings` and `Favorites` names.
- Constraints include unique user email, unique university short name, unique user/room reviews, a composite favorite primary key, and a partial unique student/room appointment index for PENDING/CONFIRMED states.
- Room amenities and landlord-request image URLs are JSON-serialized strings with EF value comparers. DateTime values are normalized to UTC. Enum states/roles are stored as strings.
- PostgreSQL schema history: `20261021100657_InitialProduction`, `20261102092534_AddLandlordRequests`, and `20261109183728_EnforcePendingLandlordRequest`. The last migration adds a unique pending-request index. `DesignTimeDbContextFactory` always targets PostgreSQL.
- `Program.cs` selects PostgreSQL when `Database:Provider=Postgres`; otherwise SQLite is allowed only in Development/Testing. PostgreSQL startup applies EF migrations. SQLite uses EnsureCreated and a supplemental `CREATE TABLE IF NOT EXISTS` for landlord requests.
- `--migrate` applies schema changes and exits before seeding or running the app. `--healthcheck` probes `http://127.0.0.1:8080/health` and exits with its result.
- Development seeding creates five universities, five demo users, five rooms, and two reviews. Production seeds university reference data and optionally creates an admin from configuration; it does not seed demo accounts/listings. Testing uses independent fixtures rather than automatic demo seeding.
- JWT issuer/audience/lifetime/signature are validated; production requires a configured secret of at least 32 bytes and the PostgreSQL provider. Authentication endpoints are limited to 20 requests/minute/IP.
- CORS uses `ClientUrl`. Middleware applies security headers, production HSTS/CSP, static assets and uploads, rate limiting, authentication, authorization, API routes, `/health`, an API 404 fallback, and the React index fallback. Swagger is enabled in Development/Testing.
- Responses generally use `{ success, data, message }`; framework validation and some empty HTTP error/status responses use normal ASP.NET behavior. Unhandled request errors are logged and converted to a generic response when headers have not been sent.
- `PostgresConnection.Normalize` safely parses connection strings/URLs, preserves escaped credentials, and uses verified TLS for external hosts unless explicitly configured otherwise. It does not guess Supabase regions/poolers. Real PostgreSQL migrations/persistence and installer connectivity were tested.

## Running, checking, and deploying

Commands below were read from source/configuration and were not executed during the project-reading task.

- Toolchain: .NET 8 SDK selected by `global.json` with latest-feature roll-forward; Node.js 22.12+ recommended by README. The local EF tool manifest pins dotnet-ef 8.0.32.
- Restore/install: `dotnet restore server` and `npm --prefix client ci`.
- Development API: `dotnet run --project server --urls http://localhost:5059`.
- Development frontend: `npm --prefix client run dev`; Vite listens on 5173 and proxies `/api`, `/health`, and `/uploads` to port 5059. `VITE_API_BASE_URL` overrides the API origin when needed.
- Backend tests: `dotnet test tests/CampusRent.Tests`, optionally with `--settings tests/coverage.runsettings --collect:"XPlat Code Coverage"`.
- Frontend tests/build: `npm --prefix client test` and `npm --prefix client run build`.
- API build: `dotnet build server`. Build before Playwright because its API launcher uses `--no-build`.
- Playwright: `npm --prefix client run test:e2e`; uses API port 5065, Vite port 5174, and `campusrent.e2e.db`. Projects are Desktop Chrome and iPhone 13 dimensions using Chromium, with one worker. `PLAYWRIGHT_CHANNEL=chrome` can select installed Chrome.
- xUnit fixtures use a unique temporary SQLite database per fixture with cleanup. `PostgresTests` runs only with `CAMPUSRENT_TEST_POSTGRES`, creates an isolated schema, and checks repeatable migrations and persistence. Coverage excludes generated migration code and historical Razor views.
- Existing tests cover accounts/permissions, geographic search, costs, image replacement, favorite idempotency, appointment transitions/UTC, reviews/moderation, reports, upgrades, user administration, uploads, and place clients. Browser tests cover search/maps, sessions, listing creation/editing, moderation views, and student workflows on desktop/mobile.
- CI in `.github/workflows/ci.yml` runs backend tests against a PostgreSQL service, migration consistency, frontend tests/build/format/audit, and Playwright, followed by Docker build and HTTP smoke checks.
- GHCR publication runs through the reusable `docker-publish.yml` after CI application/container tests pass. Direct manual publishing requires a successful CI run for the same commit.
- Dockerfile builds React with Node 22, publishes the API with .NET 8, copies the SPA into `wwwroot`, and runs on port 8080 as the .NET application user.
- Current Compose services are `web` and `db` (PostgreSQL 16), with `uploads_data` and `postgres_data` volumes. Startup migrations happen in the API process; there is no separate Compose migration service. Current commands use `docker compose up -d --build` or target `web`.
- `run.ps1` and `run.bat` are convenience launchers for API, Vite, and the browser. They were inspected but not run. The ignored root `.env` was not opened; secret values were not recorded here.

## Documentation differences to remember

- `PROJECt.md` mixes ASP.NET/EF instructions with older Node/Express/Prisma/cron sections, describes Tailwind/Axios/React Router 6, and specifies automatic academic-email verification. These differ from the implemented stack and authentication behavior.
- `docs/DEPLOYMENT.md` now documents the POSIX installer, external PostgreSQL/Caddy production deployment, bundled local web/db stack, immutable private configuration, updates, migration review and backups.
- `docs/schema.postgres.sql` is the complete idempotent export of all three EF migrations; root `db.sql` remains historical.
- `docs/TESTING.md` records current verification rather than the old 2026-10-21 counts. Docker/PostgreSQL, private uploads, current roles, password changes, map resolution, responsive navigation and installer pipelines are covered.

## Technical activity log

### 2026-11-10 - Initial project-reading task

- Checked repository instructions/context, root layout, source inventory, and Git status. Initial branch: `main`. Initial working-tree change: tracked `ui.html` already deleted. This deletion was preserved.
- Read README, CLAUDE specification, implementation/deployment/testing reports; inspected frontend/backend manifests, runtime configuration, launch scripts, container configuration, and CI workflows.
- Read the API startup pipeline, DbContext, models, main DTOs, controllers, services, seeding/design-time setup, and the landlord-request migration; inspected the SQL exports to distinguish current and historical schemas.
- Read frontend routing/state/API transport and the core search, details, posting, account, administration, map, and shared-component logic; surveyed remaining page actions and responsive CSS.
- Inspected backend test structure/fixtures and representative recent cases, frontend test expectations, Playwright workflow coverage, and coverage/browser configurations. Large binary Word/Excel documents, uploaded user files, and diagram/image contents were inventoried but not opened.
- Created this English technical memory as the only project write. No application source, configuration, dependencies, database data, or existing user changes were modified. No application launch, build, test execution, deployment, or external-service call was performed.
- Verified that the report exists at the project root and contains no trailing whitespace. Final Git status contains only the new untracked `MEMORY.md` and the pre-existing deletion of `ui.html`; `git diff --check` completed successfully.

### 2026-11-10 - Production completion and Docker installer

- User requested a functional/UI audit, completion of outstanding application behavior, production preparation, and a `deploy.sh` installer supporting `curl <file-url> | sh` or Bash with interactive PostgreSQL configuration. Deployment instructions must include the quick-install command.
- Re-read project memory and checked repository/tool availability. Origin is `https://github.com/bonelag/CampusRent`; branch remains `main`; the existing deleted `ui.html` is preserved. .NET SDK 8.0.426 and Node.js 22.23.4 are installed; Docker is not on PATH.
- Initial priorities: run current tests/build and browser checks; enforce approved landlord registration and current account permissions; protect uploaded verification documents; fix map/upload flows and production configuration; add a Linux installer with prompts read from the terminal even when stdin contains the script; update deployment/verification documentation.
- Baseline frontend verification: 15 Vitest tests passed; Vite production build and Prettier checks passed. A WSL Ubuntu 22.04 environment is available, but it has no Docker/PostgreSQL/ShellCheck installed initially.
- Removed a credential-bearing connection example from `.env.example` without copying its values into this report. Replaced all secret examples with placeholders, added LF rules for shell/YAML files, and placed the intended raw-GitHub quick-install command near the top of `docs/DEPLOYMENT.md`. The URL becomes usable after the installer is published to main. Existing exposed database credentials require rotation by the database owner before production use.
- Added an E2E database connection override so verification can use a fresh isolated database without modifying existing local application data.
- Baseline backend verification: 86 tests passed and the optional PostgreSQL test was skipped. Baseline browser verification: all 18 desktop/mobile cases passed against an isolated database.
- Added student-only public registration, password-change logic with session credential fingerprints, database-backed current role authorization, safe PostgreSQL URI normalization/TLS defaults, forwarded-header allowlists, and response compression. Removed the hard-coded Supabase pooler guess and blanket certificate trust.
- Split public listing images from private verification uploads. Listing uploads can now use persistent local storage without Cloudinary; verification images/PDFs are stored under owner-specific private directories and served only to the owner/admin. Legacy document/image access is also constrained. Attachment submissions must reference the submitting account's uploads. Protected files are not served by static middleware.
- Added safeguards against invalid admin role values and removal of the last administrator. WSL Docker/Compose/ShellCheck dependencies are being installed to validate real Linux containers and the installer.
- Docker 29.1.3 and Compose v2.40.3 now run in WSL; an isolated PostgreSQL 16 test container is available on port 55432. Added `compose.production.yaml`, Caddy HTTPS configuration, and a POSIX `deploy.sh` that prompts through `/dev/tty`, generates secrets, validates PostgreSQL from Docker, downloads/builds source, retains credentials/uploads on reruns, and waits for health/HTTPS checks. Configuration uses a protected JSON file rather than interpolating database passwords into shell/Compose expressions.
- Added lazy loading for secondary pages, authenticated attachment previews/downloads, account role refresh after approval/focus, password-change UI, and fixes for coordinate-free Google suggestions, zero-valued coordinates, stale searches/reverse-geocode responses, and map recentering. Pending upgrade requests now have a database uniqueness guard; test fixtures use isolated upload storage and explicitly prepared roles.
- Excluded uploads, generated production configuration, pgpass files and runtime environment files from Docker build context so local user documents/credentials cannot be baked into the image.
- Added migration `20261109183728_EnforcePendingLandlordRequest` and regenerated the complete idempotent PostgreSQL SQL export; EF model consistency check passed. UTC migration timestamps can differ from the Asia/Saigon activity date.
- New regression tests verified account approval/demotion with existing JWTs, private attachment ownership, password session invalidation, pending-request database uniqueness, and administrator retention. The first full run with real PostgreSQL passed 93/94 tests; the new TLS test caught Npgsql `ContainsKey` reporting supported defaults instead of supplied keys. Fixed explicit-SSL detection with a plain connection-string parser. Frontend tests/build still passed; main JS decreased from 113.77 KB to 43.85 KB (gzip 31.22 KB to 14.14 KB).
- Installer deployments use immutable per-release configuration snapshots so existing containers retain consistent JWT/database settings during updates. Shell syntax and ShellCheck checks passed. Added browser coverage for private attachments/approval, local listing uploads, password changes, and resolving Google suggestions at zero coordinates; browser upload storage and development test rate limits are isolated from normal app settings.
- Fixed public upload URLs to resolve against the configured API origin, so local image previews work when Vite/E2E uses a backend on a different port. libpq preflight uses system certificate roots for VerifyFull, matching the application TLS defaults.
- Final application regression results: 95 backend tests passed including real PostgreSQL; 17 frontend tests passed; all 24 desktop/mobile E2E cases passed. Provider timeout/cancellation behavior is covered and does not discard internal location suggestions.
- All seven Linux installer checks passed, including actual `curl` pipelines under sh/Bash, masked password input, special characters, protected configuration permissions, invalid-input refusal, and two full Docker/PostgreSQL rollouts with data and JWT sessions preserved. The full installer suite completed in 75 seconds.
- Detected and preserved user workspace changes: `CLAUDE.md` was replaced by `PROJECT.md`, a root `AGENTS.md` was added with the existing memory/language conventions, and memory references were edited. The root still has no CodeGraph index.
- Kept the bundled PostgreSQL username `postgres` for compatibility with existing Compose volumes. Installer/admin-password validation now matches the backend's 12-character minimum and 72-byte BCrypt limit; failed startup prints the exact diagnostic command. README was updated for approved landlord registration, private uploads and the installer workflow.
- Rewrote deployment documentation around the actual installer and local/external PostgreSQL modes, protected runtime files, Caddy/DNS, configuration reuse, migration review, backups, logs, credential rotation and current limitations. CI now tests the installer against real Docker/PostgreSQL before publishing images; automatic GHCR publication depends on test/container success, with manual publication requiring a successful CI run for the same commit.
- npm/NuGet advisory scans found no current vulnerabilities. Verified the built Docker image uses UID 1654. A font-loaded review of 16 screens found no JavaScript errors but identified real narrow-screen header overflow and an unreadable active list-view button; late desktop nav CSS was overriding responsive rules. Reordered the nav rules, added tablet navigation behavior, fixed active-button contrast and mobile card image sizing, and added a semantic search-page heading. Expanded the visual review to 390/768/1024/1440-pixel widths.
- The corrected 32-screen review passed with no overflow or JavaScript errors. Extended the review to 320/360 px, found remaining guest-header crowding, and moved the duplicate sign-in link into the existing mobile menu on compact phones. Added browser regression coverage for font-loaded widths, active-view contrast and mobile navigation. Fixed the login test's success condition to use navigation rather than the visibility of the header link.
- Actionlint checked the GitHub workflows and identified intentional Markdown backticks in single-quoted summary text; added scoped ShellCheck annotations. Publication workflow permissions include the caller's actions-read permission needed by manual CI validation.
- Final visual review covered 48 screens at 320/360/390/768/1024/1440 px with no overflow or JavaScript errors. Final installer suite passed all seven checks; local Compose database/web health and SPA deep links passed, and Caddy automatic-HTTPS configuration validated. Actionlint passed both workflows after correcting the annotation syntax.
- Final source review found the search filter helper was also deleting the page parameter when navigating backward, incorrectly jumping from page 3 to page 1. Fixed pagination to retain its page parameter while filter changes still reset it; added a browser regression for page 3 -> 2 -> 1 with filters preserved.
- Final results after pagination/responsive fixes: all 28 E2E tests passed; frontend build and Prettier passed. Main JS is 44.06 KB (14.21 KB gzip), compared with the initial 113.77 KB (31.22 KB gzip). Backend remains verified at 95 passes, frontend at 17 passes, and the installer at seven passes including real repeated Docker/PostgreSQL deployment. Testing/deployment reports and this architecture memory were updated to the final behavior.
- Final Git whitespace verification uses the Windows-aware CR-at-EOL setting; disabling Git newline normalization had incorrectly reported CRLF line endings as trailing whitespace. No code/content changes were needed for that diagnostic.
- Final repository verification passed with zero whitespace errors; installer and updated deployment/testing reports exist. All temporary API/browser servers were stopped. Removed the isolated PostgreSQL test container and its anonymous volume; retained verification screenshots/reports and Docker build cache. User specification/instruction changes and the deleted `ui.html` remain preserved. Changes are local and have not been committed, pushed, or deployed to a public domain.

### 2026-11-10 - Commit and publish

- User authorized committing and pushing the completed workspace changes to `origin/main`.
- Re-read project instructions/memory, checked the worktree and untracked source files, and fetched origin. The remote main branch has 12 newer QA commits covering PostRoomPage tests, Postman collections, CSS, a workflow adjustment and the historical `ui.html`.
- Publication workflow: commit the reviewed production changes and project documentation/instruction edits, integrate the remote QA commits without rewriting remote history, validate any overlap, push main, and verify the remote commit and installer URL. The user's deletion of the historical prototype remains intentional; generated outputs and local `.env` stay excluded.
- Created the production commit and rebased it onto the newer origin/main. Resolved the CSS overlap by keeping production navigation rules in their responsive-safe order and retaining QA's compact registration-link rule. Kept the intentional deletion of the obsolete ui.html despite the remote prototype edit. Updated the newly received PostRoomPage test mock to preserve real utility exports such as mediaUrl, while keeping API calls mocked. Remote Postman collections and QA history are retained.
- Integrated frontend checks passed all 21 tests (including four new QA form tests) and the production build. Git's Windows checkout changed frontend line endings to CRLF during rebase and triggered Prettier warnings; added LF attributes for JS/JSX/CSS/JSON and normalized formatting so future checkouts remain consistent. Updated the testing report for the additional QA coverage.
- All 28 desktop/mobile E2E checks also passed after integrating QA CSS and tests. Formatting and whitespace checks passed. The production commit is being finalized on top of the remote QA merge and will be pushed with a normal fast-forward push.
- Production commit `d8e5ffe6c128d462f4076512032b5f6a3ef1c966` was successfully pushed to origin/main with a normal fast-forward push; ls-remote matched local HEAD and the worktree was clean.
- Anonymous repository/raw-installer requests returned HTTP 404. An authenticated metadata request, using the same cached Git credential only in memory and without printing it, confirmed `bonelag/CampusRent` is private. Added optional `CAMPUSRENT_GITHUB_CURL_CONFIG` support for authenticated source downloads and documented the GitHub contents-API curl pipeline with a private credential file. Credential files are excluded from Git and Docker context; no repository visibility or credentials were changed.
- The private-repository installer extension passed POSIX/Bash syntax, ShellCheck and all six configuration/pipeline checks; the full Docker rollout had already passed before this download-only extension. Publication follow-up workflow: commit the authenticated-download support and release record, push normally to main, verify remote HEAD/worktree and compare the authenticated contents-API installer bytes with the committed file.

### 2026-11-10 - Repair GitHub Actions Playwright runner

- User reported CI failing at `npx playwright install --with-deps chromium`: ubuntu-latest resolved to unsupported Ubuntu 26.04 for Playwright 1.58.2.
- Re-read instructions/memory, confirmed the worktree and origin/main were synchronized, and inspected the installed Playwright host-platform mapping. Ubuntu 24.04 is explicitly supported; newer Ubuntu versions are not supported by this pinned package.
- GitHub Actions run 27 (`42689012025`, commit e7b68e5) was confirmed failed through authenticated metadata requests without exposing credentials.
- Pinned the test, container and reusable publish jobs to `ubuntu-24.04` so runner OS updates cannot invalidate browser/dependency installation. Kept the browser version and E2E configuration unchanged. Documented the runner compatibility constraint. Validation/publication workflow: lint the workflows, commit/push the runner fix, and monitor the new CI run through browser installation, E2E and container checks.
- Both workflows passed Actionlint/ShellCheck and Git whitespace checks after pinning the runner. The source change is limited to the runner labels plus the compatibility documentation/report; browser tests remain enabled.
