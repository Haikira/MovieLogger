using AutoMapper;
using MovieLogger.Service.Dtos.Watchlist;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class WatchlistItemProfile : Profile
    {
        public WatchlistItemProfile()
        {
            CreateMap<WatchlistItem, WatchlistItemResponseDto>()
                .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Movie.Title))
                .ForMember(d => d.ReleaseYear, opt => opt.MapFrom(s => s.Movie.ReleaseYear))
                .ForMember(d => d.PosterImageUrl, opt => opt.MapFrom(s => s.Movie.PosterImageUrl));
        }
    }
}
