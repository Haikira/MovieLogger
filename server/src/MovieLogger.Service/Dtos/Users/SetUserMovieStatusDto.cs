namespace MovieLogger.Service.Dtos.Users
{
    public class SetUserMovieStatusDto
    {
        public bool IsFavourite { get; set; }

        public bool IsOwned { get; set; }
    }
}
