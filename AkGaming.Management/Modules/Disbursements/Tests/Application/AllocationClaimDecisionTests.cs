using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.Application;

[TestFixture]
public sealed class AllocationClaimDecisionTests
{
    private Mock<IDisbursementRepository> _repository = null!;
    private Mock<IDisbursementNotificationOutbox> _notificationOutbox = null!;
    private DisbursementService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IDisbursementRepository>(MockBehavior.Strict);
        _notificationOutbox = new Mock<IDisbursementNotificationOutbox>(MockBehavior.Strict);
        _service = new DisbursementService(
            _repository.Object,
            Mock.Of<IReceiptFileStorage>(),
            Mock.Of<IPaymentInformationService>(),
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
