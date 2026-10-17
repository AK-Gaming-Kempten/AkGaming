using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.WebApi;
public sealed class RegistrationControllerTests
{
    private Mock<IGamenightService> Service { get; set; } = default!;
    private GamenightController Controller { get; set; } = default!;
    [SetUp]
    public void Setup()
    {
        Service = new Mock<IGamenightService>();
        Controller = new(Service.Object, Mock.Of<IMembershipClient>()) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
    }
    [Test, Description("Guest submission returns a generic accepted receipt without registration IDs or private tokens.")]
    public async Task SubmitReturnsGenericReceipt()
    {
        // Arrange
        var request = new SubmitSignup(Guid.NewGuid(), new());
        // Act
        var result = await Controller.Submit(request, default);
        // Assert
        Assert.That(result.Result, Is.TypeOf<AcceptedResult>());
        Service.Verify(s => s.SubmitAsync(request, It.Is<Actor>(a => a.Id == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}

