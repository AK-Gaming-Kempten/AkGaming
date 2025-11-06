using MemberManagement.Application.Services;
using Moq;
using AKG.Common.Generics;
using MemberManagement.Application.Mapping;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Tests;

public class MembershipApplicationServiceTests {
    
    [Test]
    public async Task Application_CreatesMember_AndLinksUser_AndSetsStatus() {
        // Arrange
        Mock<IMemberCreationService> memberCreationService = new Mock<IMemberCreationService>();
        Mock<IMemberLinkingService> memberLinkingService = new Mock<IMemberLinkingService>();
        Mock<IMembershipUpdateService> membershipUpdateService = new Mock<IMembershipUpdateService>();
        MembershipApplicationService membershipApplicationService = new MembershipApplicationService(memberCreationService.Object, memberLinkingService.Object, membershipUpdateService.Object);
        
        var userGuid = Guid.NewGuid();
        var memberCreationDto = new MemberCreationDto {
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
            }
        };
        var member = memberCreationDto.ToMember();

        memberCreationService.Setup(x => x.CreateMemberAsync(memberCreationDto)).Returns(Task.FromResult(Result<Guid>.Success(member.Id)));
        memberLinkingService.Setup(x => x.LinkMemberToUserAsync(member.Id, userGuid)).Returns(Task.FromResult(Result.Success()));
        membershipUpdateService.Setup(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant)).Returns(Task.FromResult(Result.Success()));

        // Act
        await membershipApplicationService.ApplyForMembershipAsync(userGuid, memberCreationDto);

        // Assert
        memberCreationService.Verify(x => x.CreateMemberAsync(memberCreationDto), Times.Once);
        memberLinkingService.Verify(x => x.LinkMemberToUserAsync(member.Id, userGuid), Times.Once);
        membershipUpdateService.Verify(x => x.UpdateMembershipStatusAsync(member.Id, ContractEnums.MembershipStatus.Applicant), Times.Once);
    }
}