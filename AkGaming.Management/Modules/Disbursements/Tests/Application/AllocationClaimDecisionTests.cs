using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Core.Common.Generics;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.Application;

[TestFixture]
public sealed class AllocationClaimDecisionTests
{
    private Mock<IDisbursementRepository> _repository = null!;
    private Mock<IDisbursementNotificationOutbox> _notificationOutbox = null!;
    private Mock<IPaymentInformationService> _paymentInformationService = null!;
    private DisbursementService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IDisbursementRepository>(MockBehavior.Strict);
        _notificationOutbox = new Mock<IDisbursementNotificationOutbox>(MockBehavior.Strict);
        _paymentInformationService = new Mock<IPaymentInformationService>(MockBehavior.Strict);
        _service = new DisbursementService(
            _repository.Object,
            Mock.Of<IReceiptFileStorage>(),
            _paymentInformationService.Object,
            _notificationOutbox.Object);
    }

    [Test]
    [Description("Records a linked Discord user's first allocation approval and queues a refreshed claim snapshot.")]
    public async Task DecideFromDiscord_WhenFirstDecision_StoresDecisionAndQueuesSnapshot()
    {
        // Arrange
        var application = CreateApplication();
        var userId = Guid.NewGuid();
        var request = new DiscordAllocationDecisionRequest
        {
            UserId = userId,
            ApproverName = "  Teammate  ",
            IsApproved = true
        };
        _repository.Setup(repository => repository.GetApplicationAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _repository.Setup(repository => repository.Add(It.IsAny<AllocationApproval>()));
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application));

        // Act
        var result = await _service.DecideFromDiscordAsync(application.Id, request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(application.Approvals.Single().ApproverUserId, Is.EqualTo(userId));
            Assert.That(application.Approvals.Single().ApproverName, Is.EqualTo("Teammate"));
            Assert.That(application.Approvals.Single().IsApproved, Is.True);
        });
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Description("Prevents the allocation applicant from approving or objecting through Discord.")]
    public async Task DecideFromDiscord_WhenUserIsApplicant_ReturnsFailureWithoutSaving()
    {
        // Arrange
        var application = CreateApplication();
        var request = new DiscordAllocationDecisionRequest
        {
            UserId = application.ApplicantUserId,
            ApproverName = "Applicant",
            IsApproved = false
        };
        _repository.Setup(repository => repository.GetApplicationAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // Act
        var result = await _service.DecideFromDiscordAsync(application.Id, request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("own application"));
        _notificationOutbox.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Queues a refreshed allocation claim snapshot when an administrator changes its status.")]
    public async Task UpdateStatus_WhenStatusChanges_QueuesSnapshot()
    {
        // Arrange
        var application = CreateApplication();
        application.Status = (int)AllocationApplicationStatus.Submitted;
        _repository.Setup(repository => repository.GetApplicationAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _repository.Setup(repository => repository.TryUpdateAllocationApplicationStatusAsync(
                application,
                (int)AllocationApplicationStatus.Paid,
                application.Allocation!.Amount,
                (int)AllocationApplicationStatus.Rejected,
                (int)AllocationApplicationStatus.Cancelled,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application));

        // Act
        var result = await _service.UpdateApplicationStatusAsync(application.Id,
            new UpdateAllocationApplicationStatusRequest { Status = AllocationApplicationStatus.Paid });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(application.Status, Is.EqualTo((int)AllocationApplicationStatus.Paid));
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application), Times.Once);
    }

    [Test]
    [Description("Resets decisions and starts a new Discord review when an administrator changes a claim amount.")]
    public async Task UpdateApplication_WhenAmountChanges_ResetsApprovalsAndStartsNewReview()
    {
        // Arrange
        var application = CreateApplication();
        application.Amount = 100m;
        application.Status = (int)AllocationApplicationStatus.Approved;
        application.Approvals =
        [
            new AllocationApproval { ApproverUserId = Guid.NewGuid(), ApproverName = "Anna", IsApproved = true },
            new AllocationApproval { ApproverUserId = Guid.NewGuid(), ApproverName = "Berta", IsApproved = false }
        ];
        var approvals = application.Approvals.ToList();
        var request = new UpdateAllocationApplicationRequest { Amount = 150m, Note = "  Updated split  " };
        _repository.Setup(repository => repository.GetApplicationAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _repository.Setup(repository => repository.RemoveRange(
            It.Is<IEnumerable<AllocationApproval>>(items => items.SequenceEqual(approvals))));
        _repository.Setup(repository => repository.TryUpdateAllocationApplicationAsync(
                application,
                application.Allocation!.Amount,
                (int)AllocationApplicationStatus.Rejected,
                (int)AllocationApplicationStatus.Cancelled,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application, true));

        // Act
        var result = await _service.UpdateAllocationApplicationAsync(application.Id, request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(application.Amount, Is.EqualTo(150m));
            Assert.That(application.Note, Is.EqualTo("Updated split"));
            Assert.That(application.Status, Is.EqualTo((int)AllocationApplicationStatus.Submitted));
            Assert.That(application.Approvals, Is.Empty);
        });
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application, true), Times.Once);
    }

    [Test]
    [Description("Lets the applicant cancel their own active claim and releases it through the cancelled status.")]
    public async Task CancelOwnApplication_WhenClaimIsOwned_MarksItCancelled()
    {
        // Arrange
        var application = CreateApplication();
        var token = application.Allocation!.ShareToken;
        application.Allocation.Applications.Add(application);
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.Allocation);
        _repository.Setup(repository => repository.TryUpdateAllocationApplicationStatusAsync(
                application,
                (int)AllocationApplicationStatus.Cancelled,
                application.Allocation.Amount,
                (int)AllocationApplicationStatus.Rejected,
                (int)AllocationApplicationStatus.Cancelled,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application));

        // Act
        var result = await _service.CancelOwnAllocationApplicationAsync(
            token, application.Id, application.ApplicantUserId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(AllocationApplicationStatus.Cancelled));
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application), Times.Once);
    }

    [Test]
    [Description("Prevents someone other than the applicant from adjusting an owner-scoped claim.")]
    public async Task UpdateOwnApplication_WhenClaimBelongsToAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var application = CreateApplication();
        var token = application.Allocation!.ShareToken;
        application.Allocation.Applications.Add(application);
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.Allocation);

        // Act
        var result = await _service.UpdateOwnAllocationApplicationAsync(
            token,
            application.Id,
            Guid.NewGuid(),
            new UpdateAllocationApplicationRequest { Amount = application.Amount });

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Application not found."));
        _notificationOutbox.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Lets the claimant replace the payout method snapshot without resetting existing approvals.")]
    public async Task UpdateOwnApplication_WhenPaymentMethodChanges_PreservesApprovals()
    {
        // Arrange
        var application = CreateApplication();
        var token = application.Allocation!.ShareToken;
        var paymentInformationId = Guid.NewGuid();
        var approval = new AllocationApproval
        {
            ApproverUserId = Guid.NewGuid(),
            ApproverName = "Teammate",
            IsApproved = true
        };
        application.Approvals.Add(approval);
        application.Allocation.Applications.Add(application);
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application.Allocation);
        _paymentInformationService.Setup(service => service.GetForUserAsync(application.ApplicantUserId))
            .ReturnsAsync(Result<ICollection<PaymentInformationDto>>.Success(
            [
                new PaymentInformationDto
                {
                    Id = paymentInformationId,
                    Type = PaymentInformationType.PayPal,
                    PayPalEmail = "updated@example.org"
                }
            ]));
        _repository.Setup(repository => repository.TryUpdateAllocationApplicationAsync(
                application,
                application.Allocation.Amount,
                (int)AllocationApplicationStatus.Rejected,
                (int)AllocationApplicationStatus.Cancelled,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application, false));

        // Act
        var result = await _service.UpdateOwnAllocationApplicationAsync(
            token,
            application.Id,
            application.ApplicantUserId,
            new UpdateAllocationApplicationRequest
            {
                Amount = application.Amount,
                Note = application.Note,
                PaymentInformationId = paymentInformationId
            });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(application.PaymentMethod.PaymentInformationId, Is.EqualTo(paymentInformationId));
            Assert.That(application.PaymentMethod.PayPalEmail, Is.EqualTo("updated@example.org"));
            Assert.That(application.Approvals, Is.EqualTo(new[] { approval }));
        });
        _repository.Verify(repository => repository.RemoveRange(
            It.IsAny<IEnumerable<AllocationApproval>>()), Times.Never);
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application, false), Times.Once);
    }

    private static AllocationApplication CreateApplication()
    {
        var allocation = new Allocation
        {
            Amount = 200m,
            DiscordChannelId = "channel-123",
            DiscordRoleId = "role-456",
            Event = new DisbursementEvent { Name = "Summer cup" }
        };
        return new AllocationApplication
        {
            AllocationId = allocation.Id,
            Allocation = allocation,
            ApplicantUserId = Guid.NewGuid(),
            ApplicantName = "Applicant",
            Amount = 200m,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
