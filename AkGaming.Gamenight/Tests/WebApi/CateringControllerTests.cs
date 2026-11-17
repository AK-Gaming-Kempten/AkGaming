using System.Security.Claims;
using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.WebApi;
public sealed class CateringControllerTests
{
    private Mock<ICateringService> Service { get; set; } = default!;
    private CateringController Controller { get; set; } = default!;
    [SetUp]
    public void Setup()
    {
        Service = new Mock<ICateringService>();
        Controller = new(Service.Object) { ControllerContext = new() { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "visitor"), new Claim("email", "visitor@example.com"), new Claim("email_verified", "true")], "test"))
        } } };
    }
    [Test, Description("The self-service endpoint returns the service's owned catering accounts using authenticated identity.")]
    public async Task MineUsesAuthenticatedActor()
    {
        // Arrange
        Service.Setup(s => s.OrdersAsync(It.IsAny<Actor>(), true, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        // Act
        var result = await Controller.Mine(default);
        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        Service.Verify(s => s.OrdersAsync(It.Is<Actor>(a => a.Id == "visitor" && a.VerifiedEmail == "visitor@example.com"), true, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Test, Description("The placement endpoint forwards menu and account versions and returns the created order view.")]
    public async Task PlacementReturnsServiceResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new PlaceCateringOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [new(Guid.NewGuid(), 1)]);
        var view = new CateringView(id, Guid.NewGuid(), "Alex", "visitor@example.com", false, true, null, []);
        Service.Setup(s => s.PlaceAsync(id, request, It.IsAny<Actor>(), It.IsAny<CancellationToken>())).ReturnsAsync(view);
        // Act
        var result = await Controller.Place(id, request, default);
        // Assert
        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(view));
    }
    [Test, Description("Combined visitor settlement returns no content after awaiting the service operation.")]
    public async Task SettlementReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new SettleVisitor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // Act
        var result = await Controller.Settle(id, request, default);
        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
        Service.Verify(s => s.SettleAsync(id, request, It.IsAny<Actor>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
