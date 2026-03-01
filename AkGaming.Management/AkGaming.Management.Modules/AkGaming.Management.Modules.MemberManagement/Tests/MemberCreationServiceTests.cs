using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberCreationServiceTests {
    
    [Test]
    public async Task CreateMemberAsync_CreatesMember() {
        // Arrange
        Mock<IMemberRepository> memberRepository = new Mock<IMemberRepository>();
        MemberCreationService memberCreationService = new MemberCreationService(memberRepository.Object);
        var memberCreationDto = new MemberCreationDto()
        {
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
        
        memberRepository.Setup(x => x.Add(It.IsAny<Member>())).Returns(Result<Guid>.Success(Guid.NewGuid()));
        memberRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
    
        // Act
        await memberCreationService.CreateMemberAsync(memberCreationDto);
    
        // Assert
        memberRepository.Verify(x => x.Add(It.Is<Member>(m =>
            m.Email == memberCreationDto.Email &&
            m.FirstName == memberCreationDto.FirstName &&
            m.LastName == memberCreationDto.LastName &&
            m.BirthDate == memberCreationDto.BirthDate &&
            m.Address.Street == memberCreationDto.Address.Street &&
            m.Address.ZipCode == memberCreationDto.Address.ZipCode &&
            m.Address.City == memberCreationDto.Address.City &&
            m.Address.Country == memberCreationDto.Address.Country
        )), Times.Once);
    }
    
    [Test]
    public async Task CreateMemberAsync_Fails_WhenDatabaseFails() {
        // Arrange
        Mock<IMemberRepository> memberRepository = new Mock<IMemberRepository>();
        MemberCreationService memberCreationService = new MemberCreationService(memberRepository.Object);
        var memberCreationDto = new MemberCreationDto()
        {
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
        
        memberRepository.Setup(x => x.Add(It.IsAny<Member>())).Returns(Result<Guid>.Failure("Database failed. Member was not added."));
        memberRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
    
        // Act
        var result = await memberCreationService.CreateMemberAsync(memberCreationDto);
    
        // Assert
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        Assert.That(result, Has.Property("IsSuccess").False);
    }
}