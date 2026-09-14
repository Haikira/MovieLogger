using System.ComponentModel.DataAnnotations;
using MovieLogger.Service.Enums;

namespace MovieLogger.Service.Dtos.Genres
{
    public class CreateGenreDto
    {
        [EnumDataType(typeof(Enums.GenreTitle))]
        public Enums.GenreTitle Title { get; set; }
    }
}
