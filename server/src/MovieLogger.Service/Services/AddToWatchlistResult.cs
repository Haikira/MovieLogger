using MovieLogger.Service.Dtos.Watchlist;

namespace MovieLogger.Service.Services
{
    public enum AddToWatchlistOutcome
    {
        Success,
        MovieNotFound,
        AlreadyExists
    }

    public class AddToWatchlistResult
    {
        public required AddToWatchlistOutcome Outcome { get; init; }

        public WatchlistItemResponseDto? WatchlistItem { get; init; }

        public static AddToWatchlistResult Success(WatchlistItemResponseDto watchlistItem) =>
            new() { Outcome = AddToWatchlistOutcome.Success, WatchlistItem = watchlistItem };

        public static AddToWatchlistResult MovieNotFound() =>
            new() { Outcome = AddToWatchlistOutcome.MovieNotFound };

        public static AddToWatchlistResult AlreadyExists() =>
            new() { Outcome = AddToWatchlistOutcome.AlreadyExists };
    }
}
