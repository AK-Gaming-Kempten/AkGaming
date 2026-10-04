using System.ComponentModel;
using System.Security.Claims;
using AkGaming.Identity.Api.Controllers;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Identity.Api.IntegrationTests.WebApi;

public sealed class EmailChangeControllerTests
{
    private Mock<IEmailChangeService> Service { get; } = new();
    private Guid UserId { get; } = Guid.NewGuid();
    private EmailChangeController Controller { get; }

    public EmailChangeControllerTests()
    {
        Controller = new EmailChangeController(Service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", UserId.ToString())], "test"))
                }
            }
        };
    }

    [Fact]
    [Description("The request endpoint uses the authenticated subject and returns accepted after issuing a confirmation email.")]
    public async Task Request_UsesAuthenticatedSubject()
    {
        // Arrange
        var request = new RequestEmailChangeRequest("new@example.com", "Password123");

        // Act
        var result = await Controller.RequestChange(request, CancellationToken.None);

        // Assert
        Assert.IsType<AcceptedResult>(result);
        Service.Verify(x => x.RequestAsync(UserId, request.NewEmail, request.CurrentPassword, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    [Description("The pending-request endpoint refuses a principal without a valid user ID.")]
    public async Task GetPending_RejectsMissingSubject()
    {
        // Arrange
        Controller.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await Controller.GetPending(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        Service.VerifyNoOtherCalls();
    }

    [Fact]
    [Description("Email-change conflicts retain their application status code in the HTTP response.")]
    public async Task Confirm_MapsConflict()
    {
        // Arrange
        Service.Setup(x => x.ConfirmAsync("token", null, CancellationToken.None)).ThrowsAsync(new AuthException(409, "Email unavailable."));

        // Act
        var result = await Controller.Confirm(new VerifyEmailRequest("token"), CancellationToken.None);

        // Assert
        Assert.Equal(409, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    [Description("Cancellation targets the authenticated account and returns no content.")]
    public async Task Cancel_UsesAuthenticatedSubject()
    {
        // Arrange
        var expectedUserId = UserId;

        // Act
        var result = await Controller.Cancel(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Service.Verify(x => x.CancelAsync(expectedUserId, null, CancellationToken.None), Times.Once);
    }
}
