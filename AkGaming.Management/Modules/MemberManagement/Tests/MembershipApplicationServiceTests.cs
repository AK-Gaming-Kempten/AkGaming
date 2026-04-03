using AkGaming.Management.Modules.MemberManagement.Application.Services;
using Moq;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MembershipApplicationServiceTests {
    
    [TestCase (false,false, false, false)]
    [TestCase (false,false, true, true)]
    [TestCase (false,true, false, true)]
    [TestCase (false,true, true, true)]
    [TestCase (true,false, false, true)]
    [TestCase (true,false, true, true)]
    [TestCase (true,true, false, true)]
    [TestCase (true,true, true, true)]
    public async Task Application_CreatesMember_AndLinksUser_AndSetsStatus(bool memberExists, bool hasPendingApplicationRequest, bool hasPendingLinkingRequest, bool shouldThrow) {
        // Arrange
        Mock<IMemberCreationService> memberCreationService = new Mock<IMemberCreationService>();
        Mock<IMemberLinkingService> memberLinkingService = new Mock<IMemberLinkingService>();
        Mock<IMembershipUpdateService> membershipUpdateService = new Mock<IMembershipUpdateService>();
        Mock<IMemberQueryService> memberQueryService = new Mock<IMemberQueryService>();
        Mock<IMembershipApplicationRequestRepository> membershipApplicationRequestRepository = new Mock<IMembershipApplicationRequestRepository>();
        Mock<IMemberAuditLogWriter> auditLogWriter = new Mock<IMemberAuditLogWriter>();
        Mock<IEmailSender> emailSender = new Mock<IEmailSender>();
        Mock<ILogger<MembershipApplicationService>> logger = new Mock<ILogger<MembershipApplicationService>>();
        MembershipApplicationService membershipApplicationService = new MembershipApplicationService(
            memberCreationService.Object, 
            memberLinkingService.Object, 
            membershipUpdateService.Object,
            memberQueryService.Object,
            membershipApplicationRequestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object
        );
        
        var userGuid = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var membershipApplicationRequestDto = new MembershipApplicationRequestDto {
            Id = requestId,
            IssuingUserId = userGuid,
            MemberCreationInfo = new MemberCreationDto {
                FirstName = "FistName",
                LastName = "LastName",
                Email = "test@example.com",
                Phone = "1234567890",
                DiscordUserName = "DiscordUsername",
                BirthDate = DateOnly.FromDateTime(DateTime.Now),
                Address = new AddressDto {
                    Street = "Street",
                    ZipCode = "ZipCode",
                    City = "City",
                    Country = "Country"
                }
            },
            ApplicationText = "",
            PrivacyPolicyAccepted = true,
            IsResolved = false
        };
        
        var member = membershipApplicationRequestDto.MemberCreationInfo.ToMember();

        memberLinkingService.Setup(x => x.GetMemberLinkingRequestsFromUserAsync(userGuid)).Returns(Task.FromResult(hasPendingLinkingRequest ? Result<ICollection<MemberLinkingRequestDto>>.Success(new List<MemberLinkingRequestDto> { new MemberLinkingRequestDto() }) : Result<ICollection<MemberLinkingRequestDto>>.Failure("No pending linking requests")));
        membershipApplicationRequestRepository.Setup(x => x.GetAllRequestFromUserAsync(userGuid)).Returns(Task.FromResult(hasPendingApplicationRequest ? Result<List<MembershipApplicationRequest>>.Success(new List<MembershipApplicationRequest>{ new MembershipApplicationRequest() }) : Result<List<MembershipApplicationRequest>>.Failure("No pending application requests")));
        memberQueryService.Setup(x => x.GetMemberByUserGuidAsync(userGuid)).Returns(Task.FromResult(memberExists ? Result<MemberDto>.Success(new MemberDto()) : Result<MemberDto>.Failure("Member not found")));
        memberCreationService.Setup(x => x.CreateMemberAsync(membershipApplicationRequestDto.MemberCreationInfo)).Returns(Task.FromResult(Result<Guid>.Success(member.Id)));
        memberLinkingService.Setup(x => x.LinkMemberToUserAsync(member.Id, userGuid)).Returns(Task.FromResult(Result.Success()));
        membershipUpdateService.Setup(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant)).Returns(Task.FromResult(Result.Success()));
        membershipApplicationRequestRepository.Setup(x => x.Add(It.IsAny<MembershipApplicationRequest>())).Returns(Result.Success());
        membershipApplicationRequestRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        
        // Act
        await membershipApplicationService.ApplyForMembershipAsync(membershipApplicationRequestDto);

        // Assert
        memberCreationService.Verify(x => x.CreateMemberAsync(membershipApplicationRequestDto.MemberCreationInfo), shouldThrow ? Times.Never : Times.Once);
        memberLinkingService.Verify(x => x.LinkMemberToUserAsync(member.Id, userGuid), shouldThrow ? Times.Never : Times.Once);
        membershipUpdateService.Verify(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant), shouldThrow ? Times.Never : Times.Once);
    }

    [Test]
    public async Task AcceptMembershipApplication_SendsDecisionEmail() {
        var creationService = new Mock<IMemberCreationService>();
        var linkingService = new Mock<IMemberLinkingService>();
        var membershipUpdateService = new Mock<IMembershipUpdateService>();
        var memberQueryService = new Mock<IMemberQueryService>();
        var requestRepository = new Mock<IMembershipApplicationRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MembershipApplicationService>>();

        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var request = new MembershipApplicationRequest {
            Id = requestId,
            IssuingUserId = userId,
            Email = "applicant@example.com",
            IsResolved = false
        };

        requestRepository.Setup(x => x.GetByIdAsync(requestId)).ReturnsAsync(Result<MembershipApplicationRequest>.Success(request));
        memberQueryService.Setup(x => x.GetMemberByUserGuidAsync(userId)).ReturnsAsync(Result<MemberDto>.Success(new MemberDto { Id = memberId }));
        membershipUpdateService.Setup(x => x.UpdateMembershipStatusAsync(memberId, ContractEnums.MembershipStatus.InTrial)).ReturnsAsync(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        requestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        emailSender.Setup(x => x.SendAsync("applicant@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new MembershipApplicationService(
            creationService.Object,
            linkingService.Object,
            membershipUpdateService.Object,
            memberQueryService.Object,
            requestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object);

        var result = await service.AcceptMembershipApplicationAsync(requestId);

        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync("applicant@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ApplyForMembership_SendsNotificationEmailToVorstand_WhenRequestCreated() {
        var creationService = new Mock<IMemberCreationService>();
        var linkingService = new Mock<IMemberLinkingService>();
        var membershipUpdateService = new Mock<IMembershipUpdateService>();
        var memberQueryService = new Mock<IMemberQueryService>();
        var requestRepository = new Mock<IMembershipApplicationRequestRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var emailSender = new Mock<IEmailSender>();
        var logger = new Mock<ILogger<MembershipApplicationService>>();
        var service = new MembershipApplicationService(
            creationService.Object,
            linkingService.Object,
            membershipUpdateService.Object,
            memberQueryService.Object,
            requestRepository.Object,
            auditLogWriter.Object,
            emailSender.Object,
            logger.Object);

        var userId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var request = new MembershipApplicationRequestDto {
            IssuingUserId = userId,
            MemberCreationInfo = new MemberCreationDto {
                FirstName = "Erika",
                LastName = "Mustermann",
                Email = "erika@example.com",
                Phone = "123",
                DiscordUserName = "erika#1234",
                BirthDate = new DateOnly(2000, 1, 1),
                Address = new AddressDto {
                    Street = "Street",
                    ZipCode = "12345",
                    City = "City",
                    Country = "Country"
                }
            },
            ApplicationText = "Hello",
            PrivacyPolicyAccepted = true
        };

        memberQueryService.Setup(x => x.GetMemberByUserGuidAsync(userId)).ReturnsAsync(Result<MemberDto>.Failure("not found"));
        requestRepository.Setup(x => x.GetAllRequestFromUserAsync(userId)).ReturnsAsync(Result<List<MembershipApplicationRequest>>.Failure("none"));
        linkingService.Setup(x => x.GetMemberLinkingRequestsFromUserAsync(userId)).ReturnsAsync(Result<ICollection<MemberLinkingRequestDto>>.Failure("none"));
        requestRepository.Setup(x => x.Add(It.IsAny<MembershipApplicationRequest>())).Returns(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>())).Returns(Result.Success());
        requestRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        creationService.Setup(x => x.CreateMemberAsync(It.IsAny<MemberCreationDto>())).ReturnsAsync(Result<Guid>.Success(memberId));
        linkingService.Setup(x => x.LinkMemberToUserAsync(memberId, userId)).ReturnsAsync(Result.Success());
        membershipUpdateService.Setup(x => x.UpdateMembershipStatusAsync(memberId, ContractEnums.MembershipStatus.Applicant)).ReturnsAsync(Result.Success());
        string? capturedTextBody = null;
        string? capturedHtmlBody = null;
        emailSender.Setup(x => x.SendAsync(ClubConstants.EmailAddresses.Board, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((_, _, textBody, htmlBody, _) => {
                capturedTextBody = textBody;
                capturedHtmlBody = htmlBody;
            })
            .Returns(Task.CompletedTask);

        var result = await service.ApplyForMembershipAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        emailSender.Verify(x => x.SendAsync(ClubConstants.EmailAddresses.Board, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedTextBody, Does.Contain(ClubConstants.Urls.ManagementMemberRequests));
        Assert.That(capturedHtmlBody, Does.Contain(ClubConstants.Urls.ManagementMemberRequests));
    }
}
