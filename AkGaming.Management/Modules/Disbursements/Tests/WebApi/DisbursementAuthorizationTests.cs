using AkGaming.Management.Modules.Disbursements.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace AkGaming.Management.Modules.Disbursements.Tests.WebApi;

[TestFixture]
public sealed class DisbursementAuthorizationTests
{
    [Test]
    [Description("Requires read permission for administrative queries and the stronger manage permission for status changes.")]
    public void AdministrationController_UsesScopedReadAndManagePolicies()
    {
        // Arrange
        var controllerPolicy = typeof(DisbursementAdministrationController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;
        var action = typeof(DisbursementAdministrationController).GetMethod(nameof(DisbursementAdministrationController.UpdateReimbursementStatus))!;
        var actionPolicy = action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy;

        // Act
        var policies = new[] { controllerPolicy, actionPolicy };

        // Assert
        Assert.That(policies, Is.EqualTo(new[] { "management.disbursements.read", "management.disbursements.manage" }));
    }
}
