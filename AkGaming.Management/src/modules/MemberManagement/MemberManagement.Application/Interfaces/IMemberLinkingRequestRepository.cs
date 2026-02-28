using AKG.Common.Generics;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

/// <summary>
/// Interface for the member linking request repository
/// </summary>
public interface IMemberLinkingRequestRepository {
    Task<Result<MemberLinkingRequest>> GetByIdAsync(Guid id);
    Task<Result<List<MemberLinkingRequest>>> GetAllRequestFromUserAsync(Guid userId);
    Task<Result<List<MemberLinkingRequest>>> GetAllAsync();
    Result Add(MemberLinkingRequest memberLinkingRequest);
    Result Update(MemberLinkingRequest memberLinkingRequest);
    Result Delete(Guid id);
    Task<Result> SaveChangesAsync();
    Task<Result> DisposeChangesAsync();
}