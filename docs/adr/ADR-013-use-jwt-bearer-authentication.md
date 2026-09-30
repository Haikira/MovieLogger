# ADR-013: Use JWT bearer authentication

## Status
Accepted

## Context
The API had no authentication: every endpoint was open, and user-specific endpoints (e.g. a user's movie watches, favourites) trusted a `UserId`/`userId` supplied directly by the client in the route or body. A React frontend is being built next, and it needs a real login flow plus a way for the API to know which user is making a request without trusting client-supplied identifiers.

## Decision
Use stateless JWT bearer authentication:

- `POST /api/auth/register` and `POST /api/auth/login` (`AuthController`/`AuthService`) issue a signed JWT containing the user's `Id` as the `sub`/`NameIdentifier` claim.
- Passwords are hashed with PBKDF2-SHA256 (`Pbkdf2PasswordHasher`), implemented directly against `System.Security.Cryptography` rather than pulling in ASP.NET Core Identity purely for its `PasswordHasher<TUser>` — same underlying algorithm, no extra framework dependency in `MovieLogger.Service`.
- `Program.cs` configures `AddJwtBearer` against `Jwt:Issuer`/`Jwt:Audience`/`Jwt:Key`/`Jwt:ExpirationMinutes`. Only `Issuer`/`Audience`/`ExpirationMinutes` are committed in `appsettings.json`; `Jwt:Key` is supplied via `dotnet user-secrets` locally (and would come from real secret storage, e.g. an environment variable or a secrets manager, in any deployed environment) and is never committed.
- Controllers read the current user's id from the validated JWT via a `ClaimsPrincipal.GetUserId()` extension (`MovieLogger.Api.Security`), not from any client-supplied id. Ownership-scoped endpoints (movie watches, watchlist, movie lists, account) use only that id; `UsersController`'s remaining id-scoped actions return `403 Forbidden` if the route id doesn't match the caller.
- Movie lists (`ListsController`) are ownership-scoped: a list's owner is taken from the authenticated JWT when it is created (`CreateMovieListDto` has no `UserId`), and reading, updating, deleting or adding/removing movies on a list the caller doesn't own returns `404 Not Found`, the same as movie watches. This was added after the original decision, when API integration testing found that `ListsController` was unauthenticated and trusted a client-supplied `UserId`.
- No refresh tokens: tokens are short-lived (`Jwt:ExpirationMinutes`, default 60) and the frontend re-authenticates on expiry. Logout is client-side (discard the token) since the tokens are stateless.

## Consequences
- Every previously-open endpoint that returns or mutates user-specific data now requires `[Authorize]`, which is a breaking change to the API surface — acceptable here since no frontend consumes it yet.
- A compromised or leaked token is valid until it expires; there is no server-side revocation. This is an accepted tradeoff for keeping the auth implementation simple, per the scope of this change — revisit if the project later needs immediate revocation (e.g. a refresh-token/rotation scheme, or an allow/deny list).
- Local development requires a one-time `dotnet user-secrets set "Jwt:Key" "<random string>"` from `server/src/MovieLogger.Api`; the app fails fast at startup if `Jwt:Key` is missing rather than silently running unsigned or with a hardcoded key.
- Swagger UI is configured with a `Bearer` security scheme so protected endpoints can be exercised directly from `/swagger` after logging in and pasting the returned token.
