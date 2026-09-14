using AutoMapper;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class MovieWatchProfile : Profile
    {
        public MovieWatchProfile()
        {
            CreateMap<MovieWatch, MovieWatchResponseDto>();
            CreateMap<CreateMovieWatchDto, MovieWatch>();
            CreateMap<UpdateMovieWatchDto, MovieWatch>();
        }
    }
}
