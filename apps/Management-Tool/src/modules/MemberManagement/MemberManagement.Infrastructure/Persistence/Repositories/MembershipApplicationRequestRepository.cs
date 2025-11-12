using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMembershipApplicationRequestRepository : IMembershipApplicationRequestRepository {
    private readonly MemberManagementDbContext _dbContext;
    
    public EfMembershipApplicationRequestRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }
    
    public async Task<Result<MembershipApplicationRequest>> GetByIdAsync(Guid id) {
       try {
            var membershipApplicationRequest = await _dbContext.MembershipApplicationRequests
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membershipApplicationRequest is null)
                return Result<MembershipApplicationRequest>.Failure("Membership application request not found.");

            return Result<MembershipApplicationRequest>.Success(membershipApplicationRequest);
       }
       catch (Exception ex) {
            return Result<MembershipApplicationRequest>.Failure($"Database error: {ex.Message}");
       }
    }
    
    public async Task<Result<List<MembershipApplicationRequest>>> GetAllRequestFromUserAsync(Guid userId) {
       try {
            var membershipApplicationRequests = await _dbContext.MembershipApplicationRequests
                .Where(m => m.IssuingUserId == userId)
                .ToListAsync();

            return Result<List<MembershipApplicationRequest>>.Success(membershipApplicationRequests);
       }
       catch (Exception ex) {
            return Result<List<MembershipApplicationRequest>>.Failure($"Database error: {ex.Message}");
       }
    }
    
    public async Task<Result<List<MembershipApplicationRequest>>> GetAllAsync() {
       try {
            var membershipApplicationRequests = await _dbContext.MembershipApplicationRequests
                .ToListAsync();

            return Result<List<MembershipApplicationRequest>>.Success(membershipApplicationRequests);
       }
       catch (Exception ex) {
            return Result<List<MembershipApplicationRequest>>.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Add(MembershipApplicationRequest membershipApplicationRequest) {
       try {
            _dbContext.MembershipApplicationRequests.Add(membershipApplicationRequest);
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Update(MembershipApplicationRequest membershipApplicationRequest) {
       try {
            _dbContext.MembershipApplicationRequests.Update(membershipApplicationRequest);
            return Result.Success();
       }
       catch (Exception ex) {
            return Result.Failure($"Database error: {ex.Message}");
       }
    }
    
    public Result Delete(Guid id) {
       try {
            var membershipApplicationRequest = _dbContext.MembershipApplicationRequests
                .FirstOrDefault(m => m.Id == id);

            if (membershipApplicationRequest is null)
                return Result.Failure("Membership application request not found.");

            _dbContext.MembershipApplicationRequests.Remove(membershipApplicationRequest);
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