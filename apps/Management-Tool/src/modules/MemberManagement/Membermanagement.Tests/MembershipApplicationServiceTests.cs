using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Services;
using MemberManagement.Domain.Entities;
using Moq;
using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Tests;

public class MembershipApplicationServiceTests {
    
    [Test]
    public async Task Application_CreatesMember_AndLinksUser_AndSetsStatus() {
        // Arrange
        Mock<IMemberRepository> memberRepository = new Mock<IMemberRepository>();
        MemberCreationService memberCreationService = new MemberCreationService(memberRepository.Object);
        MemberLinkingService memberLinkingService = new MemberLinkingService(memberRepository.Object);
        MembershipUpdateService membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        MembershipApplicationService membershipApplicationService = new MembershipApplicationService(memberCreationService, memberLinkingService, membershipUpdateService);
        
        var userGuid = Guid.NewGuid();
        var memberCreationDto = new MemberCreationDto()
        {
            FirstName = "FistName",
            LastName = "LastName",
            Email = "test@example.com",
            Phone = "1234567890",
            DiscordUsername = "DiscordUsername",
            BirthDate = DateOnly.FromDateTime(DateTime.Now),
            Address = new AddressDto()
            {
                Street = "Street",
                ZipCode = "ZipCode",
                City = "City",
                Country = "Country"
            }
        };
        
        memberRepository.Setup(x => x.AddAsync(It.IsAny<Member>())).Returns(Task.FromResult(Result<Guid>.Success(Guid.NewGuid())));
        // TODO: Add linking and status update setup
        memberRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
    
        // Act
        await membershipApplicationService.ApplyForMembershipAsync(userGuid, memberCreationDto);
    
        // Assert
        memberRepository.Verify(x => x.AddAsync(It.Is<Member>(m =>
            m.Email == memberCreationDto.Email &&
            m.FirstName == memberCreationDto.FirstName &&
            m.LastName == memberCreationDto.LastName &&
            m.BirthDate == memberCreationDto.BirthDate &&
            m.Address.Street == memberCreationDto.Address.Street &&
            m.Address.ZipCode == memberCreationDto.Address.ZipCode &&
            m.Address.City == memberCreationDto.Address.City &&
            m.Address.Country == memberCreationDto.Address.Country
        )), Times.Once);
        // TODO add linking and status assertions
    }
}