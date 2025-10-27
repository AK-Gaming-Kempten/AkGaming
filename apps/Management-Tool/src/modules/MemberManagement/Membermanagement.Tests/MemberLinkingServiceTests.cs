using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Services;
using MemberManagement.Domain.Entities;
using Moq;
using UserManagement.Contracts.DTO;
using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Tests;

public class MemberLinkingServiceTests {
    public async Task MemberLinkingService_LinksMemberToUser() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingService = new MemberLinkingService(memberRepository.Object);
        var memberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new Member()
        {
            Id = memberId,
            UserId = null
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(memberId))
            .ReturnsAsync(Result<Member>.Success(member));
        
        memberRepository.Setup(x => x.UpdateAsync(member))
            .ReturnsAsync(Result.Success());
        
        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        
        // Act
        var result = await memberLinkingService.LinkMemberToUserAsync(memberId, userId);
        
        // Assert
        memberRepository.Verify(x => x.GetByMemberIdAsync(memberId), Times.Once);
        memberRepository.Verify(x => x.UpdateAsync(member), Times.Once);
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(member.UserId, Is.EqualTo(userId));
    }
}