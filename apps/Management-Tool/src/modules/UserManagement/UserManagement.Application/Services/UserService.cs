using UserManagement.Application.Interfaces;
using UserManagement.Contracts.DTO;
using UserManagement.Contracts.Services;

namespace UserManagement.Application.Services;

public class UserService : IUserService {
    private readonly IUserRepository _users;

    public async Task<UserDto?> GetUserByIdAsync(Guid id) {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return null;

        return new UserDto(
            user.Id,
            user.Email,
            user.IsActive,
            user.CreatedAt
        );
    }
    
    public async Task<UserDto?> GetUserByEmailAsync(string email) {
        var user = await _users.GetByEmailAsync(email);
        if (user is null) return null;

        return new UserDto(
            user.Id,
            user.Email,
            user.IsActive,
            user.CreatedAt
        );
    }

    public async Task LinkDiscordAsync(Guid userId, string discordId) {
        throw new NotImplementedException();
    }
}