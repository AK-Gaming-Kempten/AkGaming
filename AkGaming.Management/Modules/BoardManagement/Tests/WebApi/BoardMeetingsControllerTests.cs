using System.Security.Claims;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.BoardManagement.Api.Controllers;
using AkGaming.Management.Modules.BoardManagement.Application.Services;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.BoardManagement.Tests.WebApi;

[TestFixture]
public sealed class BoardMeetingsControllerTests
{
    private Mock<IBoardMeetingService> _service = null!;
    private BoardMeetingsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IBoardMeetingService>();
        _controller = new BoardMeetingsController(_service.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("name", "Board Member")], "test")) } };
    }

    [Test]
    [Description("Uses the authenticated identity when a board member submits an availability forecast.")]
    public async Task SetAvailability_AuthenticatedUser_ReturnsSavedForecast()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var userId = Guid.Parse(_controller.User.FindFirstValue("sub")!);
        var request = new SetBoardAvailabilityRequest(BoardAvailabilityStatusDto.Available);
        var dto = new BoardAvailabilityDto(userId, "Board Member", BoardAvailabilityStatusDto.Available, DateTimeOffset.UtcNow);
        _service.Setup(x => x.SetAvailabilityAsync(meetingId, userId, "Board Member", request.Status, null, It.IsAny<CancellationToken>())).ReturnsAsync(Result<BoardAvailabilityDto>.Success(dto));

        // Act
        var result = await _controller.SetAvailability(meetingId, request, CancellationToken.None);

        // Assert
        var ok = result.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.EqualTo(dto));
    }

    [Test]
    [Description("Returns the deleted agenda item when the service removes it successfully.")]
    public async Task DeleteAgendaItem_ExistingItem_ReturnsDeletedItem()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var dto = new BoardAgendaItemDto(
            itemId,
            Guid.NewGuid(),
            "Old topic",
            null,
            BoardAgendaItemStatusDto.Scheduled,
            1,
            DateTimeOffset.UtcNow);
        _service.Setup(x => x.DeleteAgendaItemAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BoardAgendaItemDto>.Success(dto));

        // Act
        var result = await _controller.DeleteAgendaItem(itemId, CancellationToken.None);

        // Assert
        var ok = result.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.EqualTo(dto));
        _service.Verify(x => x.DeleteAgendaItemAsync(itemId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Description("Creates a Discord-submitted item at the end of the next meeting agenda using the linked user's identity.")]
    public async Task CreateNextMeetingAgendaItemFromDiscord_ValidRequest_AppendsItem()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var meeting = new BoardMeetingDto(
            meetingId,
            "Board meeting",
            DateTimeOffset.UtcNow.AddDays(1),
            90,
            null,
            BoardMeetingStatusDto.Scheduled,
            1,
            [],
            [],
            [new BoardAgendaItemDto(Guid.NewGuid(), meetingId, "Existing", null, BoardAgendaItemStatusDto.Scheduled, 0, DateTimeOffset.UtcNow)]);
        var request = new CreateDiscordBoardAgendaItemRequest(userId, "New topic", "Details");
        var created = new BoardAgendaItemDto(Guid.NewGuid(), meetingId, request.Title, request.Description, BoardAgendaItemStatusDto.Scheduled, 1, DateTimeOffset.UtcNow);
        _service.Setup(service => service.GetNextMeetingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BoardMeetingDto>.Success(meeting));
        _service.Setup(service => service.CreateAgendaItemAsync(
                It.Is<SaveBoardAgendaItemRequest>(value => value.MeetingId == meetingId && value.Order == 1 && value.Title == request.Title),
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BoardAgendaItemDto>.Success(created));

        // Act
        var result = await _controller.CreateNextMeetingAgendaItemFromDiscord(request, CancellationToken.None);

        // Assert
        var response = result.Result as CreatedResult;
        Assert.That(response?.Value, Is.EqualTo(created));
        _service.VerifyAll();
    }

    [Test]
    [Description("Forwards a Discord rescheduling proposal with the linked identity and announcement schedule version.")]
    public async Task ProposeRescheduleFromDiscord_ValidRequest_ForwardsVersionAndIdentity()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var request = new CreateDiscordRescheduleProposalRequest(
            Guid.NewGuid(), "Board Member", DateTimeOffset.UtcNow.AddDays(2), 90, "Conflict", 4);
        var proposal = new BoardRescheduleProposalDto(Guid.NewGuid(), request.ProposedAtUtc, request.DurationMinutes,
            request.Reason, RescheduleProposalStatusDto.Pending, request.UserId, request.DisplayName, DateTimeOffset.UtcNow);
        _service.Setup(service => service.ProposeRescheduleAsync(
                meetingId,
                It.Is<CreateRescheduleProposalRequest>(value => value.ProposedAtUtc == request.ProposedAtUtc
                    && value.DurationMinutes == request.DurationMinutes && value.Reason == request.Reason),
                request.UserId,
                request.DisplayName,
                request.ScheduleVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BoardRescheduleProposalDto>.Success(proposal));

        // Act
        var result = await _controller.ProposeRescheduleFromDiscord(meetingId, request, CancellationToken.None);

        // Assert
        var created = result.Result as CreatedResult;
        Assert.That(created?.Value, Is.EqualTo(proposal));
        _service.VerifyAll();
    }

    [Test]
    [Description("Forwards a confirmed Discord proposal decision with the linked manager identity.")]
    public async Task DecideProposalFromDiscord_ConfirmedDecision_ForwardsIdentityAndChoice()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var proposalId = Guid.NewGuid();
        var request = new DecideDiscordRescheduleProposalRequest(Guid.NewGuid(), true);
        var meeting = new BoardMeetingDto(meetingId, "Board meeting", DateTimeOffset.UtcNow.AddDays(2), 90, null,
            BoardMeetingStatusDto.Scheduled, 2, [], [], []);
        _service.Setup(service => service.DecideProposalAsync(meetingId, proposalId, request.Accept, request.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BoardMeetingDto>.Success(meeting));

        // Act
        var result = await _controller.DecideProposalFromDiscord(meetingId, proposalId, request,
            CancellationToken.None);

        // Assert
        var ok = result.Result as OkObjectResult;
        Assert.That(ok?.Value, Is.EqualTo(meeting));
        _service.VerifyAll();
    }
}
