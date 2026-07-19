using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.GeneralMeetings.Api.Controllers;
using AkGaming.Management.Modules.GeneralMeetings.Api.Realtime;
using AkGaming.Management.Modules.GeneralMeetings.Application.Services;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace AkGaming.Management.Modules.GeneralMeetings.Tests.WebApi;

[TestFixture]
public sealed class GeneralMeetingsControllerTests
{
    private Mock<IGeneralMeetingService> _service = null!;
    private Mock<IClientProxy> _clientProxy = null!;
    private GeneralMeetingsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IGeneralMeetingService>();
        _clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        var hub = new Mock<IHubContext<GeneralMeetingHub>>();
        hub.SetupGet(x => x.Clients).Returns(hubClients.Object);
        _controller = new GeneralMeetingsController(
            _service.Object,
            Mock.Of<IMemberQueryService>(),
            hub.Object,
            new MeetingPresenceTracker());
    }

    [Test]
    [Description("Broadcasts a ballot change to the verified meeting group after accepting an anonymous vote.")]
    public async Task CastVote_AfterSuccessfulSubmission_BroadcastsBallotChange()
    {
        // Arrange
        var ballotId = Guid.NewGuid();
        var meetingId = Guid.NewGuid();
        var request = new CastVoteRequest("anonymous-credential", [Guid.NewGuid()]);
        _service.Setup(x => x.CastVoteAsync(ballotId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(meetingId));

        // Act
        var result = await _controller.CastVote(ballotId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _clientProxy.Verify(x => x.SendCoreAsync(
            "BallotChanged",
            It.Is<object?[]>(arguments => arguments.Length == 1 && Equals(arguments[0], meetingId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
