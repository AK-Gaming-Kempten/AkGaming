using AKG.Common.Generics;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

/// <summary>
/// Interface for the membership application request repository
/// </summary>
public interface IMembershipApplicationRequestRepository {
    Task<Result<MembershipApplicationRequest>> GetByIdAsync(Guid id);
    Task<Result<List<MembershipApplicationRequest>>> GetAllRequestFromUserAsync(Guid userId);
    Task<Result<List<MembershipApplicationRequest>>> GetAllAsync();
    Result Add(MembershipApplicationRequest membershipApplicationRequest);
    Result Update(MembershipApplicationRequest membershipApplicationRequest);
    Result Delete(Guid id);
    Task<Result> SaveChangesAsync();
    Task<Result> DisposeChangesAsync();
}