using AkGaming.Core.Common.Generics;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.InvoiceGenerator.Core.Models;
using AkGaming.InvoiceGenerator.Core.Rendering;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;
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
    [Description("Creates dues only for eligible members and evaluates a returning trial member's latest trial interval.")]
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
        trialOutOfWindow.StatusChanges.Add(new MembershipStatusChangeEvent {
            MemberId = trialOutOfWindow.Id,
            OldStatus = DomainEnums.MembershipStatus.Applicant,
            NewStatus = DomainEnums.MembershipStatus.InTrial,
            Timestamp = dueDate.AddMonths(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                .AddDays(-MemberManagementConstants.DefaultTrialPeriodInDays)
        });
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
            DueDate = new DateOnly(2026, 4, 1),
            LastReminderSentAt = new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero),
            LastReminderSendStatus = DomainEnums.MembershipDueReminderSendStatus.Sent
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
            SettlementReference = "BANK-REF-1",
            LastReminderSentAt = null,
            LastReminderSendStatus = ContractEnums.MembershipDueReminderSendStatus.Failed
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
            Assert.That(existingDue.LastReminderSentAt, Is.EqualTo(new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero)));
            Assert.That(existingDue.LastReminderSendStatus, Is.EqualTo(DomainEnums.MembershipDueReminderSendStatus.Sent));
        }
        dueRepository.Verify(x => x.Update(existingDue), Times.Once);
        dueRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task GetReminderEmailPreviewAsync_ReturnsThemedReminderEmail() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var memberId = Guid.NewGuid();
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 18,
            Name = "SS 2026",
            DueDate = new DateOnly(2026, 4, 1),
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var member = new Member {
            Id = memberId,
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var due = new MembershipDue {
            Id = 42,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = memberId,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            PaidAmount = null,
            DueDate = paymentPeriod.DueDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(memberId)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        var result = await service.GetReminderEmailPreviewAsync(due.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.RecipientEmail, Is.EqualTo("max@example.com"));
            Assert.That(result.Value!.Subject, Does.Contain("SS 2026"));
            Assert.That(result.Value!.TextBody, Does.Contain("Hi Max!"));
            Assert.That(result.Value!.TextBody, Does.Contain(ClubConstants.BankAccount.Iban));
            Assert.That(result.Value!.HtmlBody, Does.Contain("Mitgliedsbeitrag offen"));
            Assert.That(result.Value!.HtmlBody, Does.Contain(ClubConstants.Urls.MembershipFees));
            Assert.That(result.Value!.HtmlBody, Does.Contain("linear-gradient(145deg,#0f221e,#163328)"));
        }
    }

    [Test]
    public async Task GetReminderEmailPreviewAsync_FailsForNonPendingDue() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Paid",
            LastName = "Member",
            Email = "paid@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 5,
            Name = "SS 2026",
            DueDate = new DateOnly(2026, 4, 1),
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 77,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Paid,
            DueAmount = 15m,
            PaidAmount = 15m,
            DueDate = new DateOnly(2026, 4, 1)
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        var result = await service.GetReminderEmailPreviewAsync(due.Id);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Due is already paid."));
    }

    [Test]
    [Description("Renders an eligible membership due reminder as a personalized PDF notice.")]
    public async Task RenderReminderPdfAsync_ReturnsRendererBytes() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var pdfRenderer = new Mock<INoticePdfRenderer>();
        var expectedPdf = new byte[] { 1, 2, 3, 4 };
        NoticeDocument? renderedNotice = null;
        pdfRenderer
            .Setup(renderer => renderer.Render(It.IsAny<NoticeDocument>()))
            .Callback<NoticeDocument>(notice => renderedNotice = notice)
            .Returns(expectedPdf);
        var service = new MembershipDueService(
            dueRepository.Object,
            paymentPeriodRepository.Object,
            memberRepository.Object,
            noticePdfRenderer: pdfRenderer.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Reminder",
            Email = "max@example.com",
            Address = new Address {
                Street = "Musterstraße 1",
                ZipCode = "87435",
                City = "Kempten"
            },
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 22,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 88,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate
        };
        dueRepository.Setup(repository => repository.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(repository => repository.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(repository => repository.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        // Act
        var result = await service.RenderReminderPdfAsync(due.Id);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(expectedPdf));
            Assert.That(renderedNotice, Is.Not.Null);
            Assert.That(renderedNotice!.Title, Is.EqualTo("Mitgliedsbeitrag offen"));
            Assert.That(renderedNotice.RecipientName, Is.EqualTo("Max Reminder"));
            Assert.That(renderedNotice.RecipientAddressLines, Does.Contain("Musterstraße 1"));
            Assert.That(renderedNotice.Sections.SelectMany(section => section.Paragraphs), Has.Some.Contains(ClubConstants.BankAccount.Iban));
            Assert.That(due.LastReminderSentAt, Is.Null);
            Assert.That(due.LastReminderSendStatus, Is.EqualTo(DomainEnums.MembershipDueReminderSendStatus.None));
        }
    }

    [Test]
    [Description("Renders an eligible suspension notice as a PDF without changing membership status.")]
    public async Task RenderSuspensionPdfAsync_ReturnsRendererBytesWithoutSuspendingMember() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var pdfRenderer = new Mock<INoticePdfRenderer>();
        var expectedPdf = new byte[] { 5, 6, 7 };
        pdfRenderer.Setup(renderer => renderer.Render(It.IsAny<NoticeDocument>())).Returns(expectedPdf);
        var service = new MembershipDueService(
            dueRepository.Object,
            paymentPeriodRepository.Object,
            memberRepository.Object,
            noticePdfRenderer: pdfRenderer.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Suspend",
            Email = "max@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 23,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 89,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate
        };
        dueRepository.Setup(repository => repository.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(repository => repository.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(repository => repository.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        // Act
        var result = await service.RenderSuspensionPdfAsync(due.Id);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(expectedPdf));
            Assert.That(member.Status, Is.EqualTo(DomainEnums.MembershipStatus.Member));
        }
        pdfRenderer.Verify(renderer => renderer.Render(It.Is<NoticeDocument>(notice =>
            notice.Title == "Mitgliedschaft suspendiert" &&
            notice.IntroParagraphs.Any(paragraph => paragraph.Contains("§6.9", StringComparison.Ordinal)))), Times.Once);
    }

    [Test]
    [Description("Builds a suspension mail preview with the Satzung context and outstanding due details.")]
    public async Task GetSuspensionEmailPreviewAsync_ReturnsSatzungBasedSuspensionEmail() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Suspend",
            Email = "max@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 19,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 43,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            PaidAmount = 5m,
            DueDate = overdueDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        // Act
        var result = await service.GetSuspensionEmailPreviewAsync(due.Id);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.RecipientEmail, Is.EqualTo("max@example.com"));
            Assert.That(result.Value!.Subject, Does.Contain("Suspendierung"));
            Assert.That(result.Value!.TextBody, Does.Contain("§4.4"));
            Assert.That(result.Value!.TextBody, Does.Contain("§6.9"));
            Assert.That(result.Value!.TextBody, Does.Contain("Aktuell offen: 10 €"));
            Assert.That(result.Value!.TextBody, Does.Contain("Mitgliederversammlung"));
            Assert.That(result.Value!.HtmlBody, Does.Contain("Mitgliedschaft suspendiert"));
            Assert.That(result.Value!.HtmlBody, Does.Contain(ClubConstants.Urls.ArticlesOfAssociation));
        }
    }

    [Test]
    public async Task GetReminderDispatchPreviewForPaymentPeriodAsync_ReturnsRecipientsAndSkippedMembers() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3));
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 23,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var sendableMember = new Member { Id = Guid.NewGuid(), FirstName = "Anna", LastName = "Able", Email = "anna@example.com", Status = DomainEnums.MembershipStatus.Member };
        var missingEmailMember = new Member { Id = Guid.NewGuid(), FirstName = "Ben", LastName = "Blank", Email = null, Status = DomainEnums.MembershipStatus.Member };
        var paidMember = new Member { Id = Guid.NewGuid(), FirstName = "Cara", LastName = "Cleared", Email = "cara@example.com", Status = DomainEnums.MembershipStatus.Member };
        var noDueMember = new Member { Id = Guid.NewGuid(), FirstName = "Dora", LastName = "Detached", Email = "dora@example.com", Status = DomainEnums.MembershipStatus.Member };
        var futureDueMember = new Member { Id = Guid.NewGuid(), FirstName = "Evan", LastName = "Early", Email = "evan@example.com", Status = DomainEnums.MembershipStatus.Member };
        var formerTrialMember = CreateFormerTrialMember(Guid.NewGuid(), paymentPeriod.DueDate.AddMonths(4), paymentPeriod.DueDate.AddMonths(4).AddDays(1));

        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        memberRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(Result<List<Member>>.Success([
            sendableMember, missingEmailMember, paidMember, noDueMember, futureDueMember, formerTrialMember
        ]));
        dueRepository.Setup(x => x.GetByPaymentPeriodIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<List<MembershipDue>>.Success([
            new MembershipDue { Id = 1, PaymentPeriodId = paymentPeriod.Id, MemberId = sendableMember.Id, Status = DomainEnums.MembershipDueStatus.Pending, DueAmount = 15m, DueDate = overdueDate },
            new MembershipDue { Id = 2, PaymentPeriodId = paymentPeriod.Id, MemberId = missingEmailMember.Id, Status = DomainEnums.MembershipDueStatus.Pending, DueAmount = 15m, DueDate = overdueDate },
            new MembershipDue { Id = 3, PaymentPeriodId = paymentPeriod.Id, MemberId = paidMember.Id, Status = DomainEnums.MembershipDueStatus.Paid, DueAmount = 15m, PaidAmount = 15m, DueDate = overdueDate },
            new MembershipDue { Id = 4, PaymentPeriodId = paymentPeriod.Id, MemberId = futureDueMember.Id, Status = DomainEnums.MembershipDueStatus.Pending, DueAmount = 15m, DueDate = futureDate }
        ]));

        var result = await service.GetReminderDispatchPreviewForPaymentPeriodAsync(paymentPeriod.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.Recipients, Has.Count.EqualTo(1));
            Assert.That(result.Value!.Recipients.Single().MemberId, Is.EqualTo(sendableMember.Id));
            Assert.That(result.Value!.SkippedMembers, Has.Count.EqualTo(5));
            Assert.That(result.Value!.SkippedMembers.Any(x => x.MemberId == missingEmailMember.Id && x.Reason == "Member has no email address."), Is.True);
            Assert.That(result.Value!.SkippedMembers.Any(x => x.MemberId == paidMember.Id && x.Reason == "Due is already paid."), Is.True);
            Assert.That(result.Value!.SkippedMembers.Any(x => x.MemberId == noDueMember.Id && x.Reason == "No due exists in this payment period."), Is.True);
            Assert.That(result.Value!.SkippedMembers.Any(x => x.MemberId == futureDueMember.Id && x.Reason == "Due date has not passed yet."), Is.True);
            Assert.That(result.Value!.SkippedMembers.Any(x => x.MemberId == formerTrialMember.Id && x.Reason == "Member was in trial for this payment period and therefore had no due."), Is.True);
        }
    }

    [Test]
    public async Task SendReminderEmailAsync_SendsReminderForEligibleDue() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var emailSender = new Mock<IEmailSender>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object, emailSender.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Mailer",
            Email = "max@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 91,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 9,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        emailSender.Setup(x => x.SendAsync(member.Email!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        dueRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        var result = await service.SendReminderEmailAsync(due.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(due.LastReminderSendStatus, Is.EqualTo(DomainEnums.MembershipDueReminderSendStatus.Sent));
            Assert.That(due.LastReminderSentAt, Is.Not.Null);
        }
        emailSender.Verify(x => x.SendAsync(
            member.Email!,
            It.Is<string>(subject => subject.Contains("SS 2026")),
            It.Is<string>(text => text.Contains("Mitgliedsbeitrag")),
            It.Is<string?>(html => html != null && html.Contains("Mitgliedsbeitrag offen")),
            It.IsAny<CancellationToken>()), Times.Once);
        dueRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    [Description("Sends the suspension mail and changes the member status to suspended only after the mail send succeeds.")]
    public async Task SendSuspensionEmailAsync_SendsMailAndSuspendsMember() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var emailSender = new Mock<IEmailSender>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object, emailSender.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Sam",
            LastName = "Suspend",
            Email = "sam@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 94,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 12,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        memberRepository.Setup(x => x.Update(member)).Returns(Result.Success());
        memberRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        emailSender.Setup(x => x.SendAsync(member.Email!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.SendSuspensionEmailAsync(due.Id);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(member.Status, Is.EqualTo(DomainEnums.MembershipStatus.Suspended));
            Assert.That(member.StatusChanges, Has.Count.EqualTo(1));
            Assert.That(member.StatusChanges.Single().NewStatus, Is.EqualTo(DomainEnums.MembershipStatus.Suspended));
        }
        emailSender.Verify(x => x.SendAsync(
            member.Email!,
            It.Is<string>(subject => subject.Contains("Suspendierung")),
            It.Is<string>(text => text.Contains("§6.9")),
            It.Is<string?>(html => html != null && html.Contains("Mitgliedschaft suspendiert")),
            It.IsAny<CancellationToken>()), Times.Once);
        memberRepository.Verify(x => x.Update(member), Times.Once);
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    [Description("Does not suspend the member when the suspension mail send fails.")]
    public async Task SendSuspensionEmailAsync_DoesNotSuspendWhenMailFails() {
        // Arrange
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var emailSender = new Mock<IEmailSender>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object, emailSender.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Fail",
            LastName = "Suspend",
            Email = "fail@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 95,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 13,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        emailSender.Setup(x => x.SendAsync(member.Email!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP offline"));

        // Act
        var result = await service.SendSuspensionEmailAsync(due.Id);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Failed to send suspension email: SMTP offline"));
            Assert.That(member.Status, Is.EqualTo(DomainEnums.MembershipStatus.Member));
            Assert.That(member.StatusChanges, Is.Empty);
        }
        memberRepository.Verify(x => x.Update(It.IsAny<Member>()), Times.Never);
        memberRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task SendReminderEmailAsync_FailsWhenDueIsNotEligible() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var emailSender = new Mock<IEmailSender>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object, emailSender.Object);

        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(4));
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Nina",
            LastName = "NotDue",
            Email = "nina@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 92,
            Name = "SS 2026",
            DueDate = futureDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 10,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = futureDate
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));

        var result = await service.SendReminderEmailAsync(due.Id);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Due date has not passed yet."));
        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        dueRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task SendReminderEmailAsync_PersistsFailureStatusWhenSendingFails() {
        var dueRepository = new Mock<IMembershipDueRepository>();
        var paymentPeriodRepository = new Mock<IMembershipPaymentPeriodRepository>();
        var memberRepository = new Mock<IMemberRepository>();
        var emailSender = new Mock<IEmailSender>();
        var service = new MembershipDueService(dueRepository.Object, paymentPeriodRepository.Object, memberRepository.Object, emailSender.Object);

        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
        var previousReminderSentAt = new DateTimeOffset(2026, 3, 1, 8, 15, 0, TimeSpan.Zero);
        var member = new Member {
            Id = Guid.NewGuid(),
            FirstName = "Faye",
            LastName = "Failing",
            Email = "faye@example.com",
            Status = DomainEnums.MembershipStatus.Member
        };
        var paymentPeriod = new MembershipPaymentPeriod {
            Id = 93,
            Name = "SS 2026",
            DueDate = overdueDate,
            DefaultDueAmount = 15m,
            ReducedDueAmount = 5m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var due = new MembershipDue {
            Id = 11,
            PaymentPeriodId = paymentPeriod.Id,
            MemberId = member.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = 15m,
            DueDate = overdueDate,
            LastReminderSentAt = previousReminderSentAt,
            LastReminderSendStatus = DomainEnums.MembershipDueReminderSendStatus.Sent
        };

        dueRepository.Setup(x => x.GetByIdAsync(due.Id)).ReturnsAsync(Result<MembershipDue>.Success(due));
        memberRepository.Setup(x => x.GetByMemberIdAsync(member.Id)).ReturnsAsync(Result<Member>.Success(member));
        paymentPeriodRepository.Setup(x => x.GetByIdAsync(paymentPeriod.Id)).ReturnsAsync(Result<MembershipPaymentPeriod>.Success(paymentPeriod));
        emailSender.Setup(x => x.SendAsync(member.Email!, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP offline"));
        dueRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());

        var result = await service.SendReminderEmailAsync(due.Id);

        using (Assert.EnterMultipleScope()) {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("Failed to send reminder email: SMTP offline"));
            Assert.That(due.LastReminderSendStatus, Is.EqualTo(DomainEnums.MembershipDueReminderSendStatus.Failed));
            Assert.That(due.LastReminderSentAt, Is.EqualTo(previousReminderSentAt));
        }
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

    private static Member CreateFormerTrialMember(Guid id, DateOnly trialEndDate, DateOnly memberSinceDate) {
        var trialStart = trialEndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            .AddDays(-MemberManagementConstants.DefaultTrialPeriodInDays);

        return new Member {
            Id = id,
            Status = DomainEnums.MembershipStatus.Member,
            StatusChanges = new List<MembershipStatusChangeEvent> {
                new() {
                    MemberId = id,
                    OldStatus = DomainEnums.MembershipStatus.Applicant,
                    NewStatus = DomainEnums.MembershipStatus.InTrial,
                    Timestamp = trialStart
                },
                new() {
                    MemberId = id,
                    OldStatus = DomainEnums.MembershipStatus.InTrial,
                    NewStatus = DomainEnums.MembershipStatus.Member,
                    Timestamp = memberSinceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                }
            }
        };
    }
}
