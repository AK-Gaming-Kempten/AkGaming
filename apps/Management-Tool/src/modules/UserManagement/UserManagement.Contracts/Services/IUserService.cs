using UserManagement.Contracts.DTO;

namespace UserManagement.Contracts.Services;

public interface IUserService {
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task LinkDiscordAsync(Guid userId, string discordId);
}