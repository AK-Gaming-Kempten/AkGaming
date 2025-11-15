using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMemberLinkingRequestRepository : IMemberLinkingRequestRepository {
    private readonly MemberManagementDbContext _dbContext;
    
    public EfMemberLinkingRequestRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }
    
    public async Task<Result<MemberLinkingRequest>> GetByIdAsync(Guid id) {
        try {
            var memberLinkingRequest = await _dbContext.MemberLinkingRequests
                .FirstOrDefaultAsync(m => m.Id == id);

            if (memberLinkingRequest is null)
                return Result<MemberLinkingRequest>.Failure("Member linking request not found.");

            return Result<MemberLinkingRequest>.Success(memberLinkingRequest);
        }
        catch (Exception ex) {
            return Result<MemberLinkingRequest>.Failure($"Database error: {ex.Message}");
        }
    }
    
    public async Task<Result<List<MemberLinkingRequest>>> GetAllRequestFromUserAsync(Guid userId) {
       try {
            var memberLinkingRequests = await _dbContext.MemberLinkingRequests
                .Where(m => m.IssuingUserId == userId)
                .ToListAsync();

            return Result<List<MemberLinkingRequest>>.Success(memberLinkingRequests);
       }
       catch (Exception ex) {
            return Result<List<MemberLinkingRequest>>.Failure($"Database error: {ex.Message}");
       }
    }
    
    public async Task<Result<List<MemberLinkingRequest>>> GetAllAsync() {
       try {
            var memberLinkingRequests = await _dbContext.MemberLinkingRequests
                .ToListAsync();

            return Result<List<MemberLinkingRequest>>.Success(memberLinkingRequests);
       }
       catch (Exception ex) {
            return Result<List<MemberLinkingRequest>>.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Add(MemberLinkingRequest memberLinkingRequest) {
       try {
            _dbContext.MemberLinkingRequests.Add(memberLinkingRequest);
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Update(MemberLinkingRequest memberLinkingRequest) {
       try {
            _dbContext.MemberLinkingRequests.Update(memberLinkingRequest);
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Delete(Guid id) {
       try {
            var memberLinkingRequest = _dbContext.MemberLinkingRequests
                .FirstOrDefault(m => m.Id == id);

            if (memberLinkingRequest is null)
                return Result.Failure("Member linking request not found.");

            _dbContext.MemberLinkingRequests.Remove(memberLinkingRequest);
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public async Task<Result> SaveChangesAsync() {
       try {
            await _dbContext.SaveChangesAsync();
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public async Task<Result> DisposeChangesAsync() {
       try {
            await _dbContext.DisposeAsync();
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
}