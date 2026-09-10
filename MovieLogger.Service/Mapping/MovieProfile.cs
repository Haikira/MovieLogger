using AutoMapper;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class MovieProfile : Profile
    {
        public MovieProfile()
        {
            CreateMap<Movie, MovieResponseDto>();

            CreateMap<CreateMovieDto, Movie>()
                .ForMember(d => d.Genres, opt => opt.Ignore());

            CreateMap<UpdateMovieDto, Movie>()
                .ForMember(d => d.Genres, opt => opt.Ignore());
        }
    }
}
