using AKG.Common.Generics;
using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces;

public interface IUserRepository {
    Task<Result<User>> GetByIdAsync(Guid id);
    Task<Result<User>> GetByEmailAsync(string email);
    Task<Result<List<User>>> GetAllAsync();
    Task<Result> AddAsync(User user);
    Task<Result> UpdateAsync(User user);
    Task<Result> DeleteAsync(User user);
    Task<Result> SaveChangesAsync();
}