using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class MembershipDueServiceTests {
    [Test]
    public async Task GetPaymentPeriodsAsync_ReturnsSortedPaymentPeriods() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var newer = new MembershipPaymentPeriod {
            Id = 2,
            Name = "2026-05",
            DueDate = new DateOnly(2026, 5, 1),
            DefaultDueAmount = 10m,
            CreatedAt = new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero)
        };
        var older = new MembershipPaymentPeriod {
            Id = 1,
            Name = "2026-04",
            DueDate = new DateOnly(2026, 4, 1),
            DefaultDueAmount = 10m,
            CreatedAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)
        };

        paymentPeriodRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<MembershipPaymentPeriod>>.Success(new List<MembershipPaymentPeriod> { newer, older }));

        // Act
        var result = await service.GetPaymentPeriodsAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value!.First().Id, Is.EqualTo(2));
        Assert.That(result.Value!.Last().Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetCurrentPaymentPeriodDuesAsync_ReturnsCurrentPeriodDues() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 10,
            Name = "2026-04",
            DueDate = new DateOnly(2026, 4, 1),
            DefaultDueAmount = 10m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var dues = new List<MembershipDue> {
            new() {
                Id = 1,
                PaymentPeriodId = paymentPeriod.Id,
                MemberId = Guid.NewGuid(),
                Status = DomainEnums.MembershipDueStatus.Pending,
                DueAmount = 10m,
                DueDate = paymentPeriod.DueDate
            }
        };

        paymentPeriodRepository.Setup(x => x.GetCurrentAsync()).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        dueRepository.Setup(x => x.GetByPaymentPeriodIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<List<MembershipDue>>.Success(dues));

        // Act
        var result = await service.GetCurrentPaymentPeriodDuesAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value!.First().Id, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPaymentPeriodDuesAsync_ReturnsPeriodDues() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var paymentPeriodId = 5;
        var memberId = Guid.NewGuid();
        var dues = new List<MembershipDue> {
            new() {
                Id = 2,
                PaymentPeriodId = paymentPeriodId,
                MemberId = memberId,
                Status = DomainEnums.MembershipDueStatus.Paid,
                DueAmount = 10m,
                PaidAmount = 10m,
                DueDate = new DateOnly(2026, 4, 1),
                SettledAt = DateTimeOffset.UtcNow
            }
        };

        dueRepository.Setup(x => x.GetByPaymentPeriodIdAsync(paymentPeriodId)).ReturnsAsync(Result<List<MembershipDue>>.Success(dues));

        // Act
        var result = await service.GetPaymentPeriodDuesAsync(paymentPeriodId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value!.First().PaymentPeriodId, Is.EqualTo(paymentPeriodId));
        Assert.That(result.Value!.First().MemberId, Is.EqualTo(memberId));
    }

    [Test]
    public async Task GetDuesForMemberAsync_ReturnsMemberDues() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var memberId = Guid.NewGuid();
        var dues = new List<MembershipDue> {
            new() {
                Id = 3,
                PaymentPeriodId = 6,
                MemberId = memberId,
                Status = DomainEnums.MembershipDueStatus.Pending,
                DueAmount = 12m,
                DueDate = new DateOnly(2026, 5, 1)
            }
        };

        dueRepository.Setup(x => x.GetByMemberIdAsync(memberId)).ReturnsAsync(Result<List<MembershipDue>>.Success(dues));

        // Act
        var result = await service.GetDuesForMemberAsync(memberId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value!.First().MemberId, Is.EqualTo(memberId));
        Assert.That(result.Value!.First().Id, Is.EqualTo(3));
    }

    [Test]
    public async Task CreatePaymentPeriodAsync_CreatesDues_OnlyForQualifiedMembers() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var dueDate = new DateOnly(2026, 4, 1);
        var member = new Member { Id = Guid.NewGuid(), Status = DomainEnums.MembershipStatus.Member };
        var trialInWindow = CreateTrialMember(Guid.NewGuid(), dueDate.AddMonths(2));
        var trialOutOfWindow = CreateTrialMember(Guid.NewGuid(), dueDate.AddMonths(4));
        var applicant = new Member { Id = Guid.NewGuid(), Status = DomainEnums.MembershipStatus.Applicant };
        var members = new List<Member> { member, trialInWindow, trialOutOfWindow, applicant };

        List<MembershipDue>? addedDues = null;
        memberRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(members));
        paymentPeriodRepository.Setup(x => x.Add(It.IsAny<MembershipPaymentPeriod>())).Returns(Result.Success());
        dueRepository.Setup(x => x.AddRange(It.IsAny<ICollection<MembershipDue>>()))
            .Callback<ICollection<MembershipDue>>(dues => addedDues = dues.ToList())
            .Returns(Result.Success());
        dueRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        var request = new MembershipPaymentPeriodCreateDto {
            Name = "2026-04",
            DueDate = dueDate,
            DefaultDueAmount = 12.5m
        };

        // Act
        var result = await service.CreatePaymentPeriodAsync(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(addedDues, Is.Not.Null);
        Assert.That(addedDues!, Has.Count.EqualTo(2));
        Assert.That(addedDues!.Select(x => x.MemberId), Contains.Item(member.Id));
        Assert.That(addedDues!.Select(x => x.MemberId), Contains.Item(trialInWindow.Id));
        Assert.That(addedDues!.Select(x => x.MemberId), Does.Not.Contain(trialOutOfWindow.Id));
        Assert.That(addedDues!.Select(x => x.MemberId), Does.Not.Contain(applicant.Id));
    }

    [Test]
    public async Task AddMembersToPaymentPeriodAsync_AddsOnlyMissingMembers() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 7,
            Name = "2026-04",
            DueDate = new DateOnly(2026, 4, 1),
            DefaultDueAmount = 10m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var memberA = new Member { Id = Guid.NewGuid(), Status = DomainEnums.MembershipStatus.Member };
        var memberB = new Member { Id = Guid.NewGuid(), Status = DomainEnums.MembershipStatus.Member };
        var existingDue = new MembershipDue {
            Id = 1,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = memberA.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 10m,
            DueDate = paymentPeriod.DueDate
        };

        List<MembershipDue>? addedDues = null;
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        dueRepository.Setup(x => x.GetByPaymentPeriodIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<List<MembershipDue>>.Success(new List<MembershipDue> { existingDue }));
        memberRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success(new List<Member> { memberA, memberB }));
        dueRepository.Setup(x => x.AddRange(It.IsAny<ICollection<MembershipDue>>()))
            .Callback<ICollection<MembershipDue>>(dues => addedDues = dues.ToList())
            .Returns(Result.Success());
        dueRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        // Act
        var result = await service.AddMembersToPaymentPeriodAsync(paymentPeriod.Id, new List<Guid> { memberA.Id, memberB.Id, memberB.Id });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(addedDues, Is.Not.Null);
        Assert.That(addedDues!, Has.Count.EqualTo(1));
        Assert.That(addedDues![0].MemberId, Is.EqualTo(memberB.Id));
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateDueAsync_UpdatesProperties_AndSaves() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var existingDue = new MembershipDue {
            Id = 5,
            PaymentPeriodId = 7,
            MemberId = Guid.NewGuid(),
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 10m,
            DueDate = new DateOnly(2026, 4, 1)
        };

        dueRepository.Setup(x => x.GetByIdAsync(existingDue.Id)).ReturnsAsync(Result<MembershipDue>.Success(existingDue));
        dueRepository.Setup(x => x.Update(It.IsAny<MembershipDue>())).Returns(Result.Success());
        dueRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        var dueDto = new MembershipDueDto {
            Id = existingDue.Id,
            PaymentPeriodId = existingDue.PaymentPeriodId,
            MemberId = existingDue.MemberId,
            Status = ContractEnums.MembershipDueStatus.Paid,
            DueAmount = 15m,
            PaidAmount = 15m,
            DueDate = new DateOnly(2026, 4, 15),
            SettledAt = DateTimeOffset.UtcNow,
            SettlementReference = "BANK-REF-1"
        };

        // Act
        var result = await service.UpdateDueAsync(existingDue.Id, dueDto);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(existingDue.Status, Is.EqualTo(DomainEnums.MembershipDueStatus.Paid));
            Assert.That(existingDue.DueAmount, Is.EqualTo(15m));
            Assert.That(existingDue.PaidAmount, Is.EqualTo(15m));
            Assert.That(existingDue.DueDate, Is.EqualTo(new DateOnly(2026, 4, 15)));
            Assert.That(existingDue.SettledAt, Is.EqualTo(dueDto.SettledAt));
            Assert.That(existingDue.SettlementReference, Is.EqualTo("BANK-REF-1"));
        }
        dueRepository.Verify(x => x.Update(existingDue), Times.Once);
        dueRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    private static Member CreateTrialMember(Guid id, DateOnly trialEndDate) {
        var trialStart = trialEndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            .AddDays(-MemberManagementConstants.DefaultTrialPeriodInDays);

        var member = new Member {
            Id = id,
            Status = DomainEnums.MembershipStatus.InTrial,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new() {
                    MemberId = id,
                    OldStatus = DomainEnums.MembershipStatus.Applicant,
                    NewStatus = DomainEnums.MembershipStatus.InTrial,
                    Timestamp = trialStart
                }
            }
        };

        return member;
    }
}
