using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Core.Common.Email;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.Extensions.Logging;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberLinkingServiceTests {
    [Test]
    public async Task MemberLinkingService_LinksMemberToUser() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingRequestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var memberLinkingService = new MemberLinkingService(
            memberRepository.Object,
            memberLinkingRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object);
        var memberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new Member()
        {
            Id = memberId,
            UserId = null
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(memberId))
            .ReturnsAsync(Result<Member>.Success(member));
        
        memberRepository.Setup(x => x.Update(member))
            .Returns(Result.Success());
        
        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>()))
            .Returns(Result.Success());
        
        // Act
        var result = await memberLinkingService.LinkMemberToUserAsync(memberId, userId);
        
        // Assert
        memberRepository.Verify(x => x.GetByMemberIdAsync(memberId), Times.Once);
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(member.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task AcceptMemberLinkingRequest_SendsDecisionEmail() {
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingRequestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var service = new MemberLinkingService(
            memberRepository.Object,
            memberLinkingRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object);

        var requestId = Guid.NewGuid();
        var request = new MemberLinkingRequest {
            Id = requestId,
            Email = "linking@example.com",
            IsResolved = false
        };

        memberLinkingRequestRepository.Setup(x => x.GetByIdAsync(requestId)).ReturnsAsync(Result<MemberLinkingRequest>.Success(request));
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        memberLinkingRequestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        string? capturedTextBody = null;
        string? capturedHtmlBody = null;
        emailSender.Setup(x => x.SendAsync("linking@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((_, _, textBody, htmlBody, _) => {
                capturedTextBody = textBody;
                capturedHtmlBody = htmlBody;
            })
            .Returns(Task.CompletedTask);

        var result = await service.AcceptMemberLinkingRequestAsync(requestId);

        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync("linking@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedTextBody, Does.Contain("https://management.akgaming.de/membership/"));
        Assert.That(capturedHtmlBody, Does.Contain("<a href=\"https://management.akgaming.de/membership/\">update your personal data</a>"));
    }

    [Test]
    public async Task RejectMemberLinkingRequest_DoesNotIncludeUpdatePersonalDataLink() {
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingRequestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var service = new MemberLinkingService(
            memberRepository.Object,
            memberLinkingRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object);

        var requestId = Guid.NewGuid();
        var request = new MemberLinkingRequest {
            Id = requestId,
            Email = "linking@example.com",
            IsResolved = false
        };

        memberLinkingRequestRepository.Setup(x => x.GetByIdAsync(requestId)).ReturnsAsync(Result<MemberLinkingRequest>.Success(request));
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        memberLinkingRequestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        string? capturedTextBody = null;
        string? capturedHtmlBody = null;
        emailSender.Setup(x => x.SendAsync("linking@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((_, _, textBody, htmlBody, _) => {
                capturedTextBody = textBody;
                capturedHtmlBody = htmlBody;
            })
            .Returns(Task.CompletedTask);

        var result = await service.RejectMemberLinkingRequestAsync(requestId);

        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync("linking@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedTextBody, Does.Not.Contain("https://management.akgaming.de/membership/"));
        Assert.That(capturedHtmlBody, Does.Not.Contain("https://management.akgaming.de/membership/"));
    }
}
