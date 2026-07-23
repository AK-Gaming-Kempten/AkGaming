using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.Extensions.Logging;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberLinkingServiceTests {
    [Test]
    [Description("Merges a user's placeholder profile into the selected legacy membership record when linking.")]
    public async Task LinkMemberToUserAsync_MergesPlaceholderProfile() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var requestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var service = new MemberLinkingService(memberRepository.Object, requestRepository.Object, auditLogWriter.Object, emailSender.Object, logger.Object);
        var userId = Guid.NewGuid();
        var target = new Member { Id = Guid.NewGuid(), FirstName = "Legacy" };
        var profile = new Member {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Current"
        };
        var paymentInformation = new PaymentInformation { MemberId = profile.Id };
        profile.PaymentInformation.Add(paymentInformation);
        memberRepository.Setup(x => x.GetByMemberIdAsync(target.Id)).ReturnsAsync(Result<Member>.Success(target));
        memberRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(Result<Member>.Success(profile));
        memberRepository.Setup(x => x.TryDelete(profile.Id)).Returns(Result.Success());
        memberRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        // Act
        var result = await service.LinkMemberToUserAsync(target.Id, userId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() => {
            Assert.That(target.UserId, Is.EqualTo(userId));
            Assert.That(target.FirstName, Is.EqualTo("Current"));
            Assert.That(paymentInformation.MemberId, Is.EqualTo(target.Id));
            Assert.That(target.PaymentInformation, Does.Contain(paymentInformation));
        });
        memberRepository.Verify(x => x.TryDelete(profile.Id), Times.Once);
    }

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
    [Description("Accepts a member linking request and queues an applicant decision notification.")]
    public async Task AcceptMemberLinkingRequest_SendsDecisionEmail() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingRequestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var notificationOutbox = new Mock<IMemberNotificationOutbox>();
        var service = new MemberLinkingService(
            memberRepository.Object,
            memberLinkingRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object,
            notificationOutbox.Object);

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

        // Act
        var result = await service.AcceptMemberLinkingRequestAsync(requestId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync("linking@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedTextBody, Does.Contain(ClubConstants.Urls.ManagementMembership));
        Assert.That(capturedTextBody, Does.Contain("Kontoverknüpfung"));
        Assert.That(capturedTextBody, Does.Contain("angenommen"));
        Assert.That(capturedHtmlBody, Does.Contain(ClubConstants.Urls.ManagementMembership));
        Assert.That(capturedHtmlBody, Does.Contain("Zur Mitgliedschaft"));
        Assert.That(capturedHtmlBody, Does.Contain("linear-gradient(145deg,#0f221e,#163328)"));
        Assert.That(capturedHtmlBody, Does.Contain(ClubConstants.Urls.LogoAsset));
        notificationOutbox.Verify(x => x.EnqueueMemberLinkingRequestStatusChanged(request, true), Times.Once);
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
        Assert.That(capturedTextBody, Does.Not.Contain(ClubConstants.Urls.ManagementMembership));
        Assert.That(capturedHtmlBody, Does.Not.Contain(ClubConstants.Urls.ManagementMembership));
    }

    [Test]
    [Description("Creates a member linking request, emails the board, and enqueues a Discord notification transactionally.")]
    public async Task CreateMemberLinkingRequest_SendsNotificationEmailToVorstand() {
        var memberRepository = new Mock<IMemberRepository>();
        var memberLinkingRequestRepository = new Mock<IMemberLinkingRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MemberLinkingService>>();
        var notificationOutbox = new Mock<IMemberNotificationOutbox>();
        var service = new MemberLinkingService(
            memberRepository.Object,
            memberLinkingRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object,
            notificationOutbox.Object);

        var request = new MemberLinkingRequestDto {
            IssuingUserId = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            DiscordUserName = "max#1234",
            Reason = ContractEnums.MemberLinkingRequestReason.NewRegistration,
            PrivacyPolicyAccepted = true
        };

        memberLinkingRequestRepository.Setup(x => x.Add(It.IsAny<MemberLinkingRequest>())).Returns(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        memberLinkingRequestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        string? capturedTextBody = null;
        string? capturedHtmlBody = null;
        emailSender.Setup(x => x.SendAsync(ClubConstants.EmailAddresses.Board, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((_, _, textBody, htmlBody, _) => {
                capturedTextBody = textBody;
                capturedHtmlBody = htmlBody;
            })
            .Returns(Task.CompletedTask);

        var result = await service.CreateMemberLinkingRequestAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync(ClubConstants.EmailAddresses.Board, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        notificationOutbox.Verify(x => x.EnqueueMemberLinkingRequestCreated(
            It.Is<MemberLinkingRequest>(linking => linking.IssuingUserId == request.IssuingUserId)), Times.Once);
        Assert.That(capturedTextBody, Does.Contain(ClubConstants.Urls.ManagementMemberRequests));
        Assert.That(capturedHtmlBody, Does.Contain(ClubConstants.Urls.ManagementMemberRequests));
        Assert.That(capturedHtmlBody, Does.Not.Contain("Dieses Schreiben wurde maschinell erstellt"));
        Assert.That(capturedHtmlBody, Does.Contain("linear-gradient(145deg,#0f221e,#163328)"));
        Assert.That(capturedHtmlBody, Does.Contain(ClubConstants.Urls.LogoAsset));
    }
}
