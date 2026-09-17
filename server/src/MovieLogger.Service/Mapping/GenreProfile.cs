using AutoMapper;
using MovieLogger.Service.Dtos.Genres;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class GenreProfile : Profile
    {
        public GenreProfile()
        {
            CreateMap<Genre, GenreResponseDto>();
            CreateMap<CreateGenreDto, Genre>();
            CreateMap<UpdateGenreDto, Genre>();
        }
    }
}
