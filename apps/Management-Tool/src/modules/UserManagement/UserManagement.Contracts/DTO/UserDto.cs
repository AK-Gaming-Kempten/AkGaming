namespace UserManagement.Contracts.DTO;

public record UserDto(
    Guid Id,
    string Email,
    bool IsActive,
    DateTime CreatedAt
);