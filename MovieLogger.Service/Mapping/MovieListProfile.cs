using AutoMapper;
using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class MovieListProfile : Profile
    {
        public MovieListProfile()
        {
            CreateMap<MovieList, MovieListResponseDto>()
                .ForMember(d => d.Movies, opt => opt.MapFrom(s => s.ListMovies.Select(lm => lm.Movie)));

            CreateMap<CreateMovieListDto, MovieList>();
            CreateMap<UpdateMovieListDto, MovieList>();
        }
    }
}
