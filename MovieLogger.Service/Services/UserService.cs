using AutoMapper;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class UserService(IUserRepository userRepository, IMapper mapper) : IUserService
    {
        public async Task<IReadOnlyList<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = await userRepository.GetAllAsync(cancellationToken);
            return users.Select(mapper.Map<UserResponseDto>).ToList();
        }

        public async Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(id, cancellationToken);
            return user is null ? null : mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = mapper.Map<User>(dto);
            user.CreatedAt = DateTime.UtcNow;

            await userRepository.AddAsync(user, cancellationToken);
            return mapper.Map<UserResponseDto>(user);
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(id, cancellationToken);
            if (user is null)
            {
                return false;
            }

            mapper.Map(dto, user);
            await userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return userRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
