using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.Application;

[TestFixture]
public sealed class AllocationCreationTests
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
    [Description("Creates an allocation without Discord routing when notifications are not wanted.")]
    public async Task CreateAllocation_WhenDiscordRoutingIsEmpty_CreatesAllocation()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var disbursementEvent = new DisbursementEvent { Id = eventId, Name = "Summer cup" };
        _repository.Setup(repository => repository.GetEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(disbursementEvent);
        _repository.Setup(repository => repository.Add(It.IsAny<Allocation>()));
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = new SaveAllocationRequest { Name = "Team prize", Amount = 200m };

        // Act
        var result = await _service.CreateAllocationAsync(eventId, request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.DiscordChannelId, Is.Empty);
            Assert.That(result.Value.DiscordRoleId, Is.Empty);
        });
        _repository.Verify(repository => repository.Add(It.Is<Allocation>(allocation =>
            allocation.DiscordChannelId == string.Empty && allocation.DiscordRoleId == string.Empty)), Times.Once);
    }

    [Test]
    [Description("Rejects incomplete Discord routing so an allocation cannot silently notify the wrong destination.")]
    public async Task CreateAllocation_WhenOnlyDiscordChannelIsProvided_ReturnsFailure()
    {
        // Arrange
        var request = new SaveAllocationRequest
        {
            Name = "Team prize",
            Amount = 200m,
            DiscordChannelId = "channel-123",
            DiscordChannelName = "team-prizes"
        };

        // Act
        var result = await _service.CreateAllocationAsync(Guid.NewGuid(), request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("both a channel and a role"));
        _repository.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Updates an existing allocation's details and Discord routing in one operation.")]
    public async Task UpdateAllocation_WhenRequestIsValid_UpdatesDetailsAndQueuesClaimSnapshots()
    {
        // Arrange
        var allocation = new Allocation
        {
            Name = "Old prize",
            Description = "Old description",
            Amount = 200m,
            Event = new DisbursementEvent { Name = "Summer cup" }
        };
        var application = new AllocationApplication
        {
            Allocation = allocation,
            AllocationId = allocation.Id,
            ApplicantUserId = Guid.NewGuid(),
            ApplicantName = "Applicant",
            Amount = 100m
        };
        allocation.Applications.Add(application);
        _repository.Setup(repository => repository.GetAllocationAsync(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificationOutbox.Setup(outbox => outbox.EnqueueAllocationClaimChanged(application));
        var request = new SaveAllocationRequest
        {
            Name = "Updated team prize",
            Description = "Updated description",
            Amount = 250m,
            DiscordChannelId = "channel-123",
            DiscordChannelName = "team-prizes",
            DiscordRoleId = "role-456",
            DiscordRoleName = "Team Blue"
        };

        // Act
        var result = await _service.UpdateAllocationAsync(allocation.Id, request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(allocation.Name, Is.EqualTo("Updated team prize"));
            Assert.That(allocation.Description, Is.EqualTo("Updated description"));
            Assert.That(allocation.Amount, Is.EqualTo(250m));
            Assert.That(allocation.DiscordChannelId, Is.EqualTo("channel-123"));
            Assert.That(allocation.DiscordRoleId, Is.EqualTo("role-456"));
        });
        _notificationOutbox.Verify(outbox => outbox.EnqueueAllocationClaimChanged(application), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Description("Prevents reducing an allocation below the amount reserved by existing active claims.")]
    public async Task UpdateAllocation_WhenAmountIsBelowActiveClaims_ReturnsFailure()
    {
        // Arrange
        var allocation = new Allocation
        {
            Name = "Team prize",
            Amount = 200m,
            Applications =
            [
                new AllocationApplication { Amount = 150m }
            ]
        };
        _repository.Setup(repository => repository.GetAllocationAsync(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        var request = new SaveAllocationRequest { Name = "Team prize", Amount = 100m };

        // Act
        var result = await _service.UpdateAllocationAsync(allocation.Id, request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("already claimed"));
        _notificationOutbox.VerifyNoOtherCalls();
    }
}
