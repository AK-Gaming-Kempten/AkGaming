using MemberManagement.Application.Services;
using Moq;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Tests;

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
        MembershipApplicationService membershipApplicationService = new MembershipApplicationService(
            memberCreationService.Object, 
            memberLinkingService.Object, 
            membershipUpdateService.Object,
            memberQueryService.Object,
            membershipApplicationRequestRepository.Object,
            auditLogWriter.Object
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
}
