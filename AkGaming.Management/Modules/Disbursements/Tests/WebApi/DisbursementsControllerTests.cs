using System.Security.Claims;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Api.Controllers;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.WebApi;

[TestFixture]
public sealed class DisbursementsControllerTests
{
    private Mock<IDisbursementService> _service = null!;
    private DisbursementsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IDisbursementService>(MockBehavior.Strict);
        _controller = new DisbursementsController(_service.Object) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }

    [Test]
    [Description("Scopes the reimbursement list query to the authenticated subject identifier.")]
    public async Task GetMyReimbursements_WhenAuthenticated_QueriesCurrentUserOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _controller.HttpContext.User = Principal(userId);
        _service.Setup(service => service.GetReimbursementsAsync(userId, CancellationToken.None)).ReturnsAsync(Result<IReadOnlyList<ReimbursementDto>>.Success([]));

        // Act
        var response = await _controller.GetMyReimbursements(CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        _service.Verify(service => service.GetReimbursementsAsync(userId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Forbids user-scoped reimbursement access when the token has no valid subject identifier.")]
    public async Task GetMyReimbursements_WhenSubjectIsMissing_ReturnsForbid()
    {
        // Arrange
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var response = await _controller.GetMyReimbursements(CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<ForbidResult>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Passes the authenticated subject identifier to the owner-scoped reimbursement cancellation service.")]
    public async Task CancelMyReimbursement_WhenAuthenticated_CancelsForCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reimbursementId = Guid.NewGuid();
        var reimbursement = new ReimbursementDto { Id = reimbursementId };
        _controller.HttpContext.User = Principal(userId);
        _service.Setup(service => service.CancelReimbursementAsync(reimbursementId, userId, CancellationToken.None))
            .ReturnsAsync(Result<ReimbursementDto>.Success(reimbursement));

        // Act
        var response = await _controller.CancelMyReimbursement(reimbursementId, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        _service.Verify(service => service.CancelReimbursementAsync(reimbursementId, userId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Forbids reimbursement cancellation when the token has no valid subject identifier.")]
    public async Task CancelMyReimbursement_WhenSubjectIsMissing_ReturnsForbid()
    {
        // Arrange
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var response = await _controller.CancelMyReimbursement(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<ForbidResult>());
        _service.VerifyNoOtherCalls();
    }

    private static ClaimsPrincipal Principal(Guid userId) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
}
