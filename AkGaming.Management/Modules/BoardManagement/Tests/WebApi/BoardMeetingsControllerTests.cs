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
}
