using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MemberShipUpdateServiceTests {

    [Test]
    [Description("Updates membership status and queues a private notification for the linked member.")]
    public async Task UpdateMembershipStatusAsync_UpdatesMembershipStatus() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var notificationOutbox = new Mock<IMemberNotificationOutbox>();
        var membershipUpdateService = new MembershipUpdateService(
            memberRepository.Object, notificationOutbox.Object);
        var guid = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var newStatus = ContractEnums.MembershipStatus.Applicant;
        
        var currentMember = new Member {
            Id = guid,
            UserId = userId,
            Status = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None,
            StatusChanges = new List<MembershipStatusChangeEvent>()
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(currentMember));
        
        memberRepository.Setup(x => x.Update(It.IsAny<Member>()))
            .Returns(Result.Success());
        
        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        
        //Act
        var result = await membershipUpdateService.UpdateMembershipStatusAsync(guid, newStatus);
        
        //Assert
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(currentMember.Status, Is.EqualTo((DomainEnums.MembershipStatus)newStatus));
        Assert.That(currentMember.StatusChanges.Count, Is.EqualTo(1));
        Assert.That(currentMember.StatusChanges.First().OldStatus, Is.EqualTo((DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None));
        Assert.That(currentMember.StatusChanges.First().NewStatus, Is.EqualTo((DomainEnums.MembershipStatus)newStatus));
        notificationOutbox.Verify(x => x.EnqueueMembershipStatusChanged(
            currentMember, DomainEnums.MembershipStatus.None), Times.Once);
    }
    
    [Test]
    public async Task UpdateMembershipStatusAsync_Fails_WhenMemberDoesNotExist() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var newStatus = ContractEnums.MembershipStatus.Applicant;
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Failure("Member not found"));
        
        //Act
        var result = await membershipUpdateService.UpdateMembershipStatusAsync(guid, newStatus);
        
        //Assert
        memberRepository.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        Assert.That(result, Has.Property("IsSuccess").False);
    }
    
    [Test]
    public async Task UpdateMembershipStatusAsync_Fails_WhenMemberAlreadyHasStatus() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var newStatus = ContractEnums.MembershipStatus.Applicant;
        
        var currentMember = new Member {
            Id = guid,
            Status = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.Applicant,
            StatusChanges = new List<MembershipStatusChangeEvent>()
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(currentMember));
        
        memberRepository.Setup(x => x.Update(It.IsAny<Member>()))
            .Returns(Result.Success());
        
        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        
        //Act
        var result = await membershipUpdateService.UpdateMembershipStatusAsync(guid, newStatus);
        
        //Assert
        memberRepository.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        Assert.That(result, Has.Property("IsSuccess").False);
    }
    
    [Test]
    public async Task InsertMembershipStatusChangeEventAsync_InsertsMembershipStatusChangeEvent() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var currentStatus = DomainEnums.MembershipStatus.Member;
        var member = new Member {
            Id = guid,
            Status = currentStatus,
            StatusChanges = new List<MembershipStatusChangeEvent>()
        };
        
        var changeEvent = new MembershipStatusChangeEventDto {
            OldStatus = ContractEnums.MembershipStatus.None,
            NewStatus = ContractEnums.MembershipStatus.Applicant,
            Timestamp = DateTime.UtcNow
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(member));
        
        memberRepository.Setup(x => x.Update(It.IsAny<Member>()))
            .Returns(Result.Success());
        
        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());
        
        //Act
        var result = await membershipUpdateService.InsertMembershipStatusChangeEventAsync(guid, changeEvent);
        
        //Assert
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(member.Status, Is.EqualTo((DomainEnums.MembershipStatus)changeEvent.NewStatus));
        Assert.That(member.StatusChanges.Count, Is.EqualTo(1));
        Assert.That(member.StatusChanges.First().OldStatus, Is.EqualTo((DomainEnums.MembershipStatus)changeEvent.OldStatus));
        Assert.That(member.StatusChanges.First().NewStatus, Is.EqualTo((DomainEnums.MembershipStatus)changeEvent.NewStatus));
        Assert.That(member.StatusChanges.First().Timestamp, Is.EqualTo(changeEvent.Timestamp));
    }

    [Test]
    public async Task InsertMembershipStatusChangeEventAsync_DoesNotUpdateMemberStatus_WhenEventIsOlderThanExistingEvents() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var currentStatus = DomainEnums.MembershipStatus.Member;
        var existingNewestTimestamp = DateTime.UtcNow;
        var olderTimestamp = existingNewestTimestamp.AddDays(-1);

        var member = new Member {
            Id = guid,
            Status = currentStatus,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new MembershipStatusChangeEvent {
                    OldStatus = DomainEnums.MembershipStatus.InTrial,
                    NewStatus = DomainEnums.MembershipStatus.Member,
                    Timestamp = existingNewestTimestamp
                }
            }
        };

        var changeEvent = new MembershipStatusChangeEventDto {
            OldStatus = ContractEnums.MembershipStatus.Member,
            NewStatus = ContractEnums.MembershipStatus.Suspended,
            Timestamp = olderTimestamp
        };

        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(member));

        memberRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(Result.Success());

        //Act
        var result = await membershipUpdateService.InsertMembershipStatusChangeEventAsync(guid, changeEvent);

        //Assert
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(member.Status, Is.EqualTo(currentStatus));
        Assert.That(member.StatusChanges.Count, Is.EqualTo(2));
    }
    
    [Test]
    [Description("Calculates the default trial end from the member's trial start.")]
    public async Task GetDefaultEndOfTrialPeriodAsync_ReturnsDefaultEndOfTrialPeriod() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var member = new Member {
            Id = guid,
            Status = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new MembershipStatusChangeEvent {
                    OldStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None,
                    NewStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.Applicant,
                    Timestamp = DateTime.UtcNow.AddDays(-20).Date
                },
                new MembershipStatusChangeEvent {
                    OldStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.Applicant,
                    NewStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.InTrial,
                    Timestamp = DateTime.UtcNow.Date
                },
                new MembershipStatusChangeEvent {
                    OldStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.InTrial,
                    NewStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.Member,
                    Timestamp = DateTime.UtcNow.AddDays(20).Date
                }
            }
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(member));
        
        //Act
        var result = await membershipUpdateService.GetDefaultEndOfTrialPeriodAsync(guid);
        
        //Assert
        Assert.That(result, Has.Property("IsSuccess").True);
        Assert.That(result.Value, Is.EqualTo(DateTime.UtcNow.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays).Date));
    }

    [Test]
    [Description("Calculates the default trial end from the latest entry when a member enters trial more than once.")]
    public async Task GetDefaultEndOfTrialPeriodAsync_UsesLatestTrialEntry() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var memberId = Guid.NewGuid();
        var latestTrialStart = DateTime.UtcNow.AddDays(-10).Date;
        var member = new Member {
            Id = memberId,
            Status = DomainEnums.MembershipStatus.InTrial,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new() {
                    OldStatus = DomainEnums.MembershipStatus.Applicant,
                    NewStatus = DomainEnums.MembershipStatus.InTrial,
                    Timestamp = DateTime.UtcNow.AddDays(-400).Date
                },
                new() {
                    OldStatus = DomainEnums.MembershipStatus.Applicant,
                    NewStatus = DomainEnums.MembershipStatus.InTrial,
                    Timestamp = latestTrialStart
                }
            }
        };
        memberRepository.Setup(repository => repository.GetByMemberIdAsync(memberId))
            .ReturnsAsync(Result<Member>.Success(member));

        // Act
        var result = await membershipUpdateService.GetDefaultEndOfTrialPeriodAsync(memberId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(
            result.Value,
            Is.EqualTo(latestTrialStart.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays).Date));
    }
    
    [Test]
    [Description("Fails to calculate a trial end when the member has no trial entry.")]
    public async Task GetDefaultEndOfTrialPeriodAsync_Fails_WhenMemberDidNotStartTrialPeriod() {
        //Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var membershipUpdateService = new MembershipUpdateService(memberRepository.Object);
        var guid = Guid.NewGuid();
        var member = new Member {
            Id = guid,
            Status = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new MembershipStatusChangeEvent {
                    OldStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.None,
                    NewStatus = (DomainEnums.MembershipStatus)ContractEnums.MembershipStatus.Applicant,
                    Timestamp = DateTime.UtcNow.AddDays(-20).Date
                }
            }
        };
        
        memberRepository.Setup(x => x.GetByMemberIdAsync(guid))
            .ReturnsAsync(Result<Member>.Success(member));
        
        //Act
        var result = await membershipUpdateService.GetDefaultEndOfTrialPeriodAsync(guid);
        
        //Assert
        Assert.That(result, Has.Property("IsSuccess").False);
        Assert.That(result.Value, Is.Null);
    }
}
