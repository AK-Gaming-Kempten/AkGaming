using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;
using Moq;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberUpdateServiceTests {
    
    [Test]
    public async Task UpdateMemberAsync_UpdatesMember() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var memberUpdateService = new MemberUpdateService(memberRepository.Object, auditLogWriter.Object);
        var guid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();

        var memberDto = new MemberDto {
            Id = guid,
            UserId = userGuid,
            FirstName = "NewFirstName",
            LastName = "NewLastName",
            Email = "NewTest@example.com",
            Phone = "New1234567890",
            DiscordUserName = "NewDiscordUsername",
            BirthDate = DateOnly.FromDateTime(new DateTime(2024, 1, 1)),
            Address = new AddressDto("NewStreet", "NewZipCode", "NewCity", "NewCountry")
        };

        var currentMember = new Member {
            Id = guid,
            UserId = userGuid,
            FirstName = "OldFirst",
            LastName = "OldLast",
            Email = "old@example.com",
            PhoneNumber = "000000",
            DiscordUsername = "OldDiscord",
            BirthDate = DateOnly.FromDateTime(new DateTime(2024, 1, 1)),
            Address = new Address()
            {
                Street = "OldStreet",
                ZipCode = "OldZipCode",
                City = "OldCity",
                Country = "OldCountry"
            }
        };

        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(currentMember));

        Member? updatedMember = null;
        memberRepository.Setup(x => x.Update(It.IsAny<Member>()))
            .Callback<Member>(m => updatedMember = m)
            .Returns(Result.Success());

        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>()))
            .Returns(Result.Success());

        // Act
        var result = await memberUpdateService.UpdateMemberAsync(guid, memberDto);

        // Assert
        memberRepository.Verify(x => x.Update(It.IsAny<Member>()), Times.Once);

        Assert.That(updatedMember, Is.Not.Null);
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.Multiple(() => {
            Assert.That(updatedMember!.Id, Is.EqualTo(guid));
            Assert.That(updatedMember.UserId, Is.EqualTo(userGuid));
            Assert.That(updatedMember.FirstName, Is.EqualTo("NewFirstName"));
            Assert.That(updatedMember.LastName, Is.EqualTo("NewLastName"));
            Assert.That(updatedMember.Email, Is.EqualTo("NewTest@example.com"));
            Assert.That(updatedMember.PhoneNumber, Is.EqualTo("New1234567890"));
            Assert.That(updatedMember.DiscordUsername, Is.EqualTo("NewDiscordUsername"));
            Assert.That(updatedMember.Address.Street, Is.EqualTo("NewStreet"));
            Assert.That(updatedMember.Address.ZipCode, Is.EqualTo("NewZipCode"));
            Assert.That(updatedMember.Address.City, Is.EqualTo("NewCity"));
            Assert.That(updatedMember.Address.Country, Is.EqualTo("NewCountry"));
        });
    }
    
    [Test]
    public async Task UpdateMemberAsync_Fails_WhenMemberDoesNotExist() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var auditLogWriter = new Mock<IMemberAuditLogWriter>();
        var memberUpdateService = new MemberUpdateService(memberRepository.Object, auditLogWriter.Object);
        var guid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();

        var memberDto = new MemberDto {
            Id = guid,
            UserId = userGuid,
            FirstName = "NewFirstName",
            LastName = "NewLastName",
            Email = "NewTest@example.com",
            Phone = "New1234567890",
            DiscordUserName = "NewDiscordUsername",
            BirthDate = DateOnly.FromDateTime(new DateTime(2024, 1, 1)),
            Address = new AddressDto("NewStreet", "NewZipCode", "NewCity", "NewCountry")
        };

        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Failure("Member not found"));

        Member? updatedMember = null;
        memberRepository.Setup(x => x.Update(It.IsAny<Member>()))
            .Callback<Member>(m => updatedMember = m)
            .Returns(Result.Success());

        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        auditLogWriter.Setup(x => x.Add(It.IsAny<MemberAuditLog>()))
            .Returns(Result.Success());

        // Act
        var result = await memberUpdateService.UpdateMemberAsync(guid, memberDto);

        // Assert
        memberRepository.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);

        Assert.That(updatedMember, Is.Null);
        Assert.That(result, Has.Property("IsSuccess").False);
    }
}
