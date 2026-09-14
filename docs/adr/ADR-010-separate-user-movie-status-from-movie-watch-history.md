# ADR-010: Separate user movie status from movie watch history

## Status
Accepted

## Context
The domain needs to represent two different things about a user and a movie: whether the user currently considers the movie a favourite or owns it, and the history of times the user has actually watched it. These are conceptually different. Favourite/owned status is current state — it has exactly one value at any point in time, and setting it again simply replaces the previous value. A watch, by contrast, is a repeatable event — a user can watch the same movie multiple times, each time producing its own date, score, and review, none of which should overwrite the others. Modelling both as a single record keyed on the user/movie pair would conflate these two shapes: either watch history is lost (each new watch overwrites the previous watch's score/review on the same row), or the record would need multiple rows per user/movie pair despite favourite/owned being meant to have a single current value, undermining the one-status-per-pair invariant that relationship is supposed to have.

## Decision
Model these as two separate entities.

`UserMovie` represents the user's current, standing relationship with a movie. It is keyed on the composite `(UserId, MovieId)`, guaranteeing at most one record per user/movie pair, and holds only `IsFavourite` and `IsOwned`.

`MovieWatch` represents a single viewing event. It has its own surrogate `Id`, independent of the user or movie, and holds `UserId`, `MovieId`, `WatchedAt`, `Score`, and `Review`. Because the key is a surrogate `Id` rather than `(UserId, MovieId)`, a user can have any number of `MovieWatch` records for the same movie — one per time they watched it.

Score and review live exclusively on `MovieWatch`. They are deliberately not duplicated on `UserMovie`, since `UserMovie` represents current relationship state, not a historical event — storing a rating there would either need to be kept in sync with watch history, risking staleness, or would ambiguously represent "the" rating when several watches might each carry their own.

## Consequences
- Favourite/owned status and watch history have independent lifecycles: a user can own a movie without ever logging a watch, and can log any number of watches without affecting favourite/owned status.
- Every watch a user logs is preserved as its own record; watching a movie again never overwrites an earlier watch's score or review.
- No rating or review is stored redundantly against the user/movie relationship, so there is nothing to keep in sync with watch history or that can go stale relative to it.
- Any feature that needs to show a single "current" rating or review for a movie must define its own selection or aggregation rule over the relevant `MovieWatch` records — this ADR does not prescribe that rule (e.g. latest vs. average), since that decision has not yet been made.
