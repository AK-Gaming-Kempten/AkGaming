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
    
    [Test]
    public async Task Application_CreatesMember_AndLinksUser_AndSetsStatus() {
        // Arrange
        Mock<IMemberCreationService> memberCreationService = new Mock<IMemberCreationService>();
        Mock<IMemberLinkingService> memberLinkingService = new Mock<IMemberLinkingService>();
        Mock<IMembershipUpdateService> membershipUpdateService = new Mock<IMembershipUpdateService>();
        Mock<IMemberQueryService> memberQueryService = new Mock<IMemberQueryService>();
        Mock<IMembershipApplicationRequestRepository> membershipApplicationRequestRepository = new Mock<IMembershipApplicationRequestRepository>();
        MembershipApplicationService membershipApplicationService = new MembershipApplicationService(
            memberCreationService.Object, 
            memberLinkingService.Object, 
            membershipUpdateService.Object,
            memberQueryService.Object,
            membershipApplicationRequestRepository.Object
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
            Address = new AddressDto()
            {
                Street = "Street",
                ZipCode = "ZipCode",
                City = "City",
                Country = "Country"
            }},
            ApplicationText = "",
            IsResolved = false
        };
        
        var member = membershipApplicationRequestDto.MemberCreationInfo.ToMember();

        memberCreationService.Setup(x => x.CreateMemberAsync(membershipApplicationRequestDto.MemberCreationInfo)).Returns(Task.FromResult(Result<Guid>.Success(member.Id)));
        memberLinkingService.Setup(x => x.LinkMemberToUserAsync(member.Id, userGuid)).Returns(Task.FromResult(Result.Success()));
        membershipUpdateService.Setup(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant)).Returns(Task.FromResult(Result.Success()));
        membershipApplicationRequestRepository.Setup(x => x.Add(It.IsAny<MembershipApplicationRequest>())).Returns(Result.Success());
        membershipApplicationRequestRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
        
        // Act
        await membershipApplicationService.ApplyForMembershipAsync(membershipApplicationRequestDto);

        // Assert
        memberCreationService.Verify(x => x.CreateMemberAsync(membershipApplicationRequestDto.MemberCreationInfo), Times.Once);
        memberLinkingService.Verify(x => x.LinkMemberToUserAsync(member.Id, userGuid), Times.Once);
        membershipUpdateService.Verify(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant), Times.Once);
    }
}