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
    private DisbursementService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IDisbursementRepository>(MockBehavior.Strict);
        _service = new DisbursementService(
            _repository.Object,
            Mock.Of<IReceiptFileStorage>(),
            Mock.Of<IPaymentInformationService>(),
            Mock.Of<IDisbursementNotificationOutbox>());
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
}
