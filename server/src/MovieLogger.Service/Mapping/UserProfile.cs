using AutoMapper;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserResponseDto>();
            CreateMap<UpdateUserDto, User>();

            CreateMap<UserMovie, UserMovieResponseDto>();
        }
    }
}
