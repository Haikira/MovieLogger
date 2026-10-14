# Movie Logger frontend

The Movie Logger web app: a React single-page app built from the Figma designs in
[`docs/design`](../docs/design) and backed by the ASP.NET Core API in [`server`](../server).

- React 19 + TypeScript, built with Vite
- React Router for navigation
- TanStack Query for server state (caching, refetching, invalidation)
- A small typed `fetch` wrapper for API calls
- React Context for authentication
- CSS Modules plus a few design tokens (no UI framework)
- Lucide icons, Inter font
- Vitest + React Testing Library

## Running it locally

Prerequisites: Node.js 20+ (developed on Node 24), and the API running locally (see the
[root README](../README.md), [`docs/migrations.md`](../docs/migrations.md) and the `Jwt:Key`
user secret described in `server/src/MovieLogger.Api/Program.cs`).

1. Start the API with its `http` launch profile (listens on `http://localhost:5021`):

   ```
   dotnet run --project server/src/MovieLogger.Api --launch-profile http
   ```

2. Install dependencies and start the dev server from `client/`:

   ```
   npm install
   npm run dev
   ```

3. Open http://localhost:5173.

On Windows, if PowerShell's execution policy blocks `npm`, use `npm.cmd` / `npx.cmd` (for example
`npm.cmd run dev`) rather than changing the policy.

### Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Vite dev server with hot reload, proxying `/api` to the API |
| `npm run build` | Type-checks (`tsc`) and builds a production bundle into `dist/` |
| `npm run preview` | Serves the production build locally |
| `npm test` | Runs the test suite once |
| `npm run test:watch` | Runs the tests in watch mode |
| `npm run lint` | ESLint (TypeScript + React Hooks rules) |
| `npm run typecheck` | TypeScript only |

## Configuration and the API base URL

Copy `.env.example` to `.env.local` to override the defaults (both are optional):

| Variable | Default | Used for |
| --- | --- | --- |
| `VITE_API_BASE_URL` | empty (same origin) | Prefix for every API call made by the browser, e.g. `https://api.example.com`. |
| `VITE_API_PROXY_TARGET` | `http://localhost:5021` | Where the **dev server** proxies `/api/*`. |

In development the browser only ever talks to the Vite dev server, which proxies `/api` to the
API. That keeps everything on one origin, so the API needs no CORS configuration. For a
deployment you can either serve the built `dist/` from the same origin as the API (leave
`VITE_API_BASE_URL` empty), or host it separately and set `VITE_API_BASE_URL`; the latter needs the
frontend's origin adding to the API's `Cors:AllowedOrigins` (see
[`docs/deployment.md`](../docs/deployment.md)). `VITE_*` values are baked in at build time, so set
`VITE_API_BASE_URL` before `npm run build`.

## Project structure

```
src/
  api/          Typed API layer: DTO types, the fetch wrapper and error handling, one function per endpoint, query keys
  auth/         Token storage, AuthProvider/useAuth, route guards
  components/
    layout/     App shell (280px sidebar), auth card layout, page header
    movies/     Movie table cell, movie picker
    ui/         Buttons, fields, dialogs, rating input, pagination, filters, feedback states
  hooks/        TanStack Query hooks, list/URL state, debouncing, form state
  lib/          Pure helpers: dates, formatting, validation rules, watchlist filtering
  pages/        One component per route
  styles/       Design tokens and global styles
  test/         Test setup, fixtures, fetch stub and app renderer
```

Pages compose hooks and components; they never call `fetch` directly. All requests go through
`src/api/endpoints.ts`, and every cached query key lives in `src/api/queryKeys.ts` so that
mutations invalidate consistently.

## Authentication

- **Real JWT auth against the API.** Login/register call `/api/auth/login` and
  `/api/auth/register`; every other request sends `Authorization: Bearer <token>`.
- **Identity always comes from the server.** On load, a stored token is exchanged for the user via
  `GET /api/users/me`; the user object is never read from storage or accepted from input. Profile
  updates and account deletion use the id from that response (the API also rejects any other id
  with 403).
- **Session end.** Any authenticated request that gets 401, an expired token, or `/me` returning
  401/404 (e.g. a deleted account) signs the user out, clears cached data and returns them to the
  login page with a "session has ended" message. A timer also signs the user out when the
  token's `expiresAt` passes. If the API can't be reached while restoring a session, the user gets
  a retry screen instead of being logged out.
- **Protected routes.** Everything except `/login` and `/register` is behind `RequireAuth`, which
  redirects to `/login` and returns the user to the page they wanted after logging in.

### Token storage

The API issues a bearer token (60 minutes by default) in the response body and has no refresh
tokens or cookie support, so the token has to be kept in JavaScript-accessible storage:

- With **Remember me** ticked, the token goes in `localStorage` and survives closing the browser
  (until it expires).
- Otherwise (and after registering) it goes in `sessionStorage`, so it's discarded when the tab
  closes.
- Only the token and its expiry are stored; expired tokens are removed when read.

Storage that JavaScript can read is exposed to any script injected into the page (XSS). The app
mitigates this by never rendering user content as HTML (React escapes everything), loading no
third-party scripts, and keeping sessions short. The stronger option, an `HttpOnly` cookie set by
the API, would need backend changes (cookie issuing, CSRF protection, CORS credentials) and is
worth revisiting if the app is deployed publicly.

## Dates

`src/lib/dates.ts` is the only place that converts dates:

- **Timestamps** (`createdAt`, `dateAdded`, `expiresAt`) are UTC instants. The API serialises
  them without a trailing `Z`, so they're parsed as UTC and then shown in the user's local time.
- **Watched dates** (`dateWatched`, `lastWatchedAt`) are calendar days. They're sent as
  `YYYY-MM-DD`, read back by taking the date part of the string, and never passed through a
  `Date`, so the day shown can't shift with the user's time zone. "Today" (the default and the
  maximum for the date picker) is the user's local date.

## Design decisions and deviations from the Figma

These follow the decisions agreed before implementation:

- **My Movies rows** are whole movies (the API aggregates watches), so **Edit** opens the movie's
  details at *Your History*, where each individual log can be edited or deleted. There is no
  delete action on the aggregate row.
- **Search** has no "Release Date" sort: the API can't sort by it. The API filters title,
  director and year separately, so a **Search by** selector (Title / Director / Year) sits where
  the sort control was.
- **Watchlist** search, genre filter, sorting and pagination run in the browser over the full
  watchlist response, which now includes director, runtime and genres (a small backend change).
- **Forgot password** is omitted from the login page: the API has no password reset yet.
- **Avatars** use the display name's initial; Change photo/Remove are omitted (no avatar storage).
- **Passwords** in the register and change-password forms must be 8+ characters with a number,
  as in the design. The API enforces the same rule.
- **Add Movie** requires at least one genre, chosen from `GET /api/genres`.
- **Logging a watchlisted movie** doesn't remove it from the watchlist. After saving, the user is
  asked whether to remove it.
- **Dashboard "Recently Watched"** uses `GET /api/moviewatches/my-movies` sorted by newest watched,
  because the dashboard endpoint's `recentlyWatched` only contains movie ids. The stat tiles and
  top genres come from `GET /api/dashboard`; "added this week" is counted from the watchlist's
  `dateAdded` values.
- **Search "Your History"** counts come from `GET /api/moviewatches` (all of the user's logs) and
  the watchlist, fetched once and cached, rather than one request per movie.
- **Page sizes** follow the design (8 rows); requests never exceed the API maximum of 100.

## Accessibility

- Every form control has a label; errors are linked with `aria-describedby`, flagged with
  `aria-invalid`, and form-level/API errors are announced through live regions.
- Visible focus styles throughout; a "Skip to main content" link; focus moves to each page's
  heading after navigation.
- Dialogs trap focus, close on Escape, return focus to the trigger, and destructive ones start on
  Cancel.
- Filters are native `<select>`s; the rating input is a radio group; watchlist toggles use
  `aria-pressed`; icon-only buttons have accessible names; star ratings have text alternatives.
- State isn't conveyed by colour alone (e.g. "On watchlist" text plus a filled bookmark).

**Known issue:** the design's orange (`#f97316`) has about 2.8:1 contrast against white, below
WCAG AA for normal-size text (orange links, white text on orange buttons). The colours are kept as
designed; `--color-primary-text` in `src/styles/tokens.css` is the single place to darken link
text (for example to `#c2410c`, 5.2:1) if the design is revisited.

## Tests

`npm test` runs Vitest with jsdom. Tests render the real app (routes, providers, auth) and replace
`fetch` with a small stub (`src/test/mockApi.ts`) that fails on any request a test didn't set up.
They cover:

- auth: protected-route redirects, session restore, expired/rejected tokens, login/register
  validation and API errors, "remember me" storage
- the API client: bearer tokens, error normalisation (400/401/403/404/409/5xx/network)
- search debouncing, pagination and page resets, filters and sorting
- watchlist behaviour (409/404 handled as success, undo, stepping back a page after removals)
- logging and editing watches (date rules, clearable rating, API validation errors, the
  post-save watchlist prompt)
- settings (profile, password rules, logout, delete-account confirmation)
- date handling, validation rules and sidebar highlighting as unit tests
