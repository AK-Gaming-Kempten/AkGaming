using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberCreationServiceTests {
    [Test]
    [Description("Creates an empty status-None profile linked to the authenticated user.")]
    public async Task CreateUserProfileAsync_CreatesLinkedProfile() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MemberCreationService(memberRepository.Object);
        var userId = Guid.NewGuid();
        Member? createdProfile = null;
        memberRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(Result<Member>.Failure("Member not found."));
        memberRepository.Setup(x => x.Add(It.IsAny<Member>()))
            .Callback<Member>(member => createdProfile = member)
            .Returns(Result.Success());
        memberRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        // Act
        var result = await service.CreateUserProfileAsync(userId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(createdProfile, Is.Not.Null);
        Assert.Multiple(() => {
            Assert.That(createdProfile!.UserId, Is.EqualTo(userId));
            Assert.That(createdProfile.Status, Is.EqualTo(Domain.Enums.MembershipStatus.None));
            Assert.That(createdProfile.Address, Is.Not.Null);
        });
    }

    [Test]
    [Description("Returns the existing profile instead of creating a duplicate for the same user.")]
    public async Task CreateUserProfileAsync_ReturnsExistingProfile() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MemberCreationService(memberRepository.Object);
        var userId = Guid.NewGuid();
        var existingProfile = new Member { Id = Guid.NewGuid(), UserId = userId };
        memberRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(Result<Member>.Success(existingProfile));

        // Act
        var result = await service.CreateUserProfileAsync(userId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(existingProfile.Id));
        memberRepository.Verify(x => x.Add(It.IsAny<Member>()), Times.Never);
    }

    [Test]
    [Description("Does not create a profile when the existing-profile lookup fails for a database reason.")]
    public async Task CreateUserProfileAsync_DoesNotCreateAfterLookupFailure() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MemberCreationService(memberRepository.Object);
        var userId = Guid.NewGuid();
        memberRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(Result<Member>.Failure("Database error: unavailable"));

        // Act
        var result = await service.CreateUserProfileAsync(userId);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        memberRepository.Verify(x => x.Add(It.IsAny<Member>()), Times.Never);
    }

    
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
