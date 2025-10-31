using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Services;
using MemberManagement.Domain.Entities;
using Moq;
using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Tests;

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
            DiscordUsername = "DiscordUsername",
            BirthDate = DateTime.Now,
            Address = new AddressDto()
            {
                Street = "Street",
                ZipCode = "ZipCode",
                City = "City",
                Country = "Country"
            }
        };
        
        memberRepository.Setup(x => x.AddAsync(It.IsAny<Member>())).Returns(Task.FromResult(Result.Success()));
        memberRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
    
        // Act
        await memberCreationService.CreateMemberAsync(memberCreationDto);
    
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
            DiscordUsername = "DiscordUsername",
            BirthDate = DateTime.Now,
            Address = new AddressDto()
            {
                Street = "Street",
                ZipCode = "ZipCode",
                City = "City",
                Country = "Country"
            }
        };
        
        memberRepository.Setup(x => x.AddAsync(It.IsAny<Member>())).Returns(Task.FromResult(Result.Failure("Database failed. Member was not added.")));
        memberRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.FromResult(Result.Success()));
    
        // Act
        var result = await memberCreationService.CreateMemberAsync(memberCreationDto);
    
        // Assert
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        Assert.That(result, Has.Property("IsSuccess").False);
    }
}