using System.ComponentModel.DataAnnotations;
using MovieLogger.Service.Enums;

namespace MovieLogger.Service.Dtos.Genres
{
    public class UpdateGenreDto
    {
        [EnumDataType(typeof(Enums.GenreTitle))]
        public Enums.GenreTitle Title { get; set; }
    }
}
