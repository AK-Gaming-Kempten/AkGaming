using AKG.Common.Generics;
using UserManagement.Application.Interfaces;
using UserManagement.Contracts.DTO;
using UserManagement.Contracts.Services;

namespace UserManagement.Application.Services;

public class UserService : IUserService {
    private readonly IUserRepository _users;

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id) {
        var userResult = await _users.GetByIdAsync(id);
        if (!userResult.IsSuccess)
            return Result<UserDto>.Failure("User not found");
        var user = userResult.Value!;

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.IsActive,
            user.CreatedAt
        ));
    }
    
    public async Task<Result<UserDto>> GetUserByEmailAsync(string email) {
        var userResult = await _users.GetByEmailAsync(email);
        if (!userResult.IsSuccess)
            return Result<UserDto>.Failure("User not found");
        var user = userResult.Value!;

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.IsActive,
            user.CreatedAt
        ));
    }

    public async Task<Result> LinkDiscordAsync(Guid userId, string discordId) {
        throw new NotImplementedException();
    }
}