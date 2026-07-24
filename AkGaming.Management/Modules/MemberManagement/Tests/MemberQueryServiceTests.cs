using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

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

    [Test]
    [Description("Returns only unresolved trial members and derives the interval from the latest trial entry.")]
    public async Task GetTrialMembersAsync_ReturnsUnresolvedMembersUsingLatestTrialEntry() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var firstTrialStart = DateTime.UtcNow.AddDays(-400);
        var activeTrialStart = DateTime.UtcNow.AddDays(-20);
        var activeMember = CreateMemberInTrial(
            "Active",
            firstTrialStart,
            activeTrialStart);
        var resolvedMember = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Resolved",
            Status = DomainEnums.MembershipStatus.Member,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                CreateTrialEntry(DateTime.UtcNow.AddDays(-200))
            }
        };
        memberRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(Result<List<Member>>.Success([activeMember, resolvedMember]));

        // Act
        var result = await memberQueryService.GetTrialMembersAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value!.Single().Member.Id, Is.EqualTo(activeMember.Id));
        Assert.That(result.Value.Single().TrialStartedAt, Is.EqualTo(activeTrialStart));
        Assert.That(
            result.Value.Single().TrialEndsAt,
            Is.EqualTo(activeTrialStart.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays).Date));
    }

    [Test]
    [Description("Orders trial decisions by deadline and places members with missing trial history last.")]
    public async Task GetTrialMembersAsync_OrdersByDeadlineWithMissingHistoryLast() {
        // Arrange
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var memberQueryService = new MemberQueryService(memberRepositoryMock.Object);
        var urgentMember = CreateMemberInTrial("Urgent", DateTime.UtcNow.AddDays(-170));
        var laterMember = CreateMemberInTrial("Later", DateTime.UtcNow.AddDays(-10));
        var missingHistoryMember = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Missing",
            Status = DomainEnums.MembershipStatus.InTrial
        };
        memberRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(Result<List<Member>>.Success([laterMember, missingHistoryMember, urgentMember]));

        // Act
        var result = await memberQueryService.GetTrialMembersAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var trialMembers = result.Value!;
        Assert.That(
            trialMembers.Select(item => item.Member.Id),
            Is.EqualTo(new[] { urgentMember.Id, laterMember.Id, missingHistoryMember.Id }));
        Assert.That(trialMembers.Last().TrialEndsAt, Is.Null);
    }

    private static Member CreateMemberInTrial(string firstName, params DateTime[] trialStarts) {
        return new Member {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            Status = DomainEnums.MembershipStatus.InTrial,
            StatusChanges = trialStarts.Select(CreateTrialEntry).ToList()
        };
    }

    private static MembershipStatusChangeEvent CreateTrialEntry(DateTime timestamp) {
        return new MembershipStatusChangeEvent {
            OldStatus = DomainEnums.MembershipStatus.Applicant,
            NewStatus = DomainEnums.MembershipStatus.InTrial,
            Timestamp = timestamp
        };
    }
}
