using MemberManagement.Application.Interfaces;
using MemberManagement.Application.Services;
using MemberManagement.Domain.Entities;
using Moq;
using UserManagement.Contracts.DTO;
using AKG.Common.Generics;
using Membermanagement.Contracts.DTO;
using ContractEnums = Membermanagement.Contracts.Enums;
using DomainEnums = MemberManagement.Domain.Enums;
using MemberManagement.Domain.ValueObjects;

namespace Membermanagement.Tests;

public class MemberQueryServiceTests {
    [Test]
    public async Task GetMemberByGuidAsync_ReturnsMemberDto() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid = Guid.NewGuid();
        var member = new Member {
            Id = guid
        };
        
        memberRepositoryMock.Setup(x => x.GetByMemberIdAsync(guid)).ReturnsAsync(Result<Member>.Success(member));
        
        // Act
        var result = await memberQueryService.GetMemberByGuidAsync(guid);
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<MemberDto>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Property("Id").EqualTo(guid));
    }
    
    [Test]
    public async Task GetMemberByGuidAsync_Fails_WhenMemberDoesNotExist() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid = Guid.NewGuid();
        
        memberRepositoryMock.Setup(x => x.GetByMemberIdAsync(guid)).ReturnsAsync(Result<Member>.Failure("Member not found"));
        
        // Act
        var result = await memberQueryService.GetMemberByGuidAsync(guid);
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<MemberDto>>());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.Null);
    }
    
    [Test]
    public async Task GetAllMembersAsync_ReturnsListMemberDto() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var member1 = new Member {
            Id = guid1
        };
        var member2 = new Member {
            Id = guid2
        };
        
        memberRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member> { member1, member2 }));
        
        // Act
        var result = await memberQueryService.GetAllMembersAsync();
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<ICollection<MemberDto>>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value, Has.Some.With.Property(nameof(MemberDto.Id)).EqualTo(guid1));
        Assert.That(result.Value, Has.Some.With.Property(nameof(MemberDto.Id)).EqualTo(guid2));
    }

    [Test]
    public async Task GetAllMembersAsync_ReturnsEmptyList_WhenNoMembersInRepository() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        
        memberRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member>()));
        
        // Act
        var result = await memberQueryService.GetAllMembersAsync();
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<ICollection<MemberDto>>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(0));
    }
    
    [Test]
    public async Task GetMembersWithStatusAsync_ReturnsListMemberDto_WithOnlyMembersWithStatus() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var member1 = new Member {
            Id = guid1,
            Status = DomainEnums.MembershipStatus.Member
        };
        var member2 = new Member {
            Id = guid2,
            Status = DomainEnums.MembershipStatus.Applicant
        };
        
        memberRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member> { member1, member2 }));
        
        // Act
        var result = await memberQueryService.GetMembersWithStatusAsync(ContractEnums.MembershipStatus.Member);
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<ICollection<MemberDto>>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value, Has.One.With.Property(nameof(MemberDto.Id)).EqualTo(guid1));
        Assert.That(result.Value, Has.None.With.Property(nameof(MemberDto.Id)).EqualTo(guid2));
    }
    
    [Test]
    public async Task GetMembersWithStatusAsync_Multiple_ReturnsListMemberDto_WithOnlyMembersWithStatus() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();
        var member1 = new Member {
            Id = guid1,
            Status = DomainEnums.MembershipStatus.Member
        };
        var member2 = new Member {
            Id = guid2,
            Status = DomainEnums.MembershipStatus.Applicant
        };
        var member3 = new Member {
            Id = guid3,
            Status = DomainEnums.MembershipStatus.None
        };
        
        memberRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member> { member1, member2, member3 }));
        
        // Act
        var result = await memberQueryService.GetMembersWithStatusAsync(new List<ContractEnums.MembershipStatus> {
            ContractEnums.MembershipStatus.Member, 
            ContractEnums.MembershipStatus.Applicant
        });
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<ICollection<MemberDto>>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value, Has.One.With.Property(nameof(MemberDto.Id)).EqualTo(guid1));
        Assert.That(result.Value, Has.One.With.Property(nameof(MemberDto.Id)).EqualTo(guid2));
        Assert.That(result.Value, Has.None.With.Property(nameof(MemberDto.Id)).EqualTo(guid3));
    }
    
    [Test]
    public async Task GetMembersWithStatusAsync_ReturnsEmptyListMemberDto_WhenNoMembersWithStatus() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var member1 = new Member {
            Id = guid1,
            Status = DomainEnums.MembershipStatus.Member
        };
        var member2 = new Member {
            Id = guid2,
            Status = DomainEnums.MembershipStatus.Applicant
        };
        
        memberRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member> { member1, member2 }));
        
        // Act
        var result = await memberQueryService.GetMembersWithStatusAsync(ContractEnums.MembershipStatus.Expelled);
        
        // Assert
        Assert.That(result, Is.InstanceOf<Result<ICollection<MemberDto>>>());
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(0));
    }
}