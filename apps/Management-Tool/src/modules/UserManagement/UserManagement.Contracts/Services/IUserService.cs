using AKG.Common.Generics;
using UserManagement.Contracts.DTO;

namespace UserManagement.Contracts.Services;

public interface IUserService {
    Task<Result<UserDto>> GetUserByIdAsync(Guid id);
    Task<Result<UserDto>> GetUserByEmailAsync(string email);
    Task<Result> LinkDiscordAsync(Guid userId, string discordId);
}