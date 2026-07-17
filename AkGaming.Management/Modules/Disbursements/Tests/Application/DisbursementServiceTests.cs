using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Moq;

namespace AkGaming.Management.Modules.Disbursements.Tests.Application;

[TestFixture]
public sealed class DisbursementServiceTests
{
    private Mock<IDisbursementRepository> _repository = null!;
    private Mock<IReceiptFileStorage> _storage = null!;
    private Mock<IPaymentInformationService> _payments = null!;
    private DisbursementService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IDisbursementRepository>(MockBehavior.Strict);
        _storage = new Mock<IReceiptFileStorage>(MockBehavior.Strict);
        _payments = new Mock<IPaymentInformationService>(MockBehavior.Strict);
        _service = new DisbursementService(_repository.Object, _storage.Object, _payments.Object);
    }

    [Test]
    [Description("Rejects receipt files larger than the configured per-file limit before storing data.")]
    public async Task CreateReimbursement_WhenReceiptIsTooLarge_ReturnsFailure()
    {
        // Arrange
        var request = ValidReimbursementRequest();
        var files = new[] { new ReceiptUpload("receipt.pdf", "application/pdf", DisbursementService.MaximumReceiptSize + 1, Stream.Null) };

        // Act
        var result = await _service.CreateReimbursementAsync(Guid.NewGuid(), "User", request, files);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        _repository.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
        _payments.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Creates a reimbursement only after validating and snapshotting a payment method owned by the applicant.")]
    public async Task CreateReimbursement_WhenValid_SnapshotsOwnedPaymentMethod()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var request = ValidReimbursementRequest(paymentId);
        var upload = new ReceiptUpload("receipt.pdf", "application/pdf", 100, new MemoryStream([1, 2, 3]));
        _payments.Setup(service => service.GetForUserAsync(userId)).ReturnsAsync(Result<ICollection<PaymentInformationDto>>.Success([
            new PaymentInformationDto { Id = paymentId, Type = PaymentInformationType.PayPal, PayPalEmail = "pay@example.org" }
        ]));
        _storage.Setup(storage => storage.SaveAsync(It.IsAny<Guid>(), "receipt.pdf", upload.Content, It.IsAny<CancellationToken>())).ReturnsAsync("receipt.pdf");
        _repository.Setup(repository => repository.Add(It.IsAny<Reimbursement>()));
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateReimbursementAsync(userId, "User", request, [upload]);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.PaymentMethod.DisplayName, Is.EqualTo("PayPal · pay@example.org"));
        Assert.That(result.Value.PaymentMethod.PayPalEmail, Is.EqualTo("pay@example.org"));
        _repository.Verify(repository => repository.Add(It.Is<Reimbursement>(item => item.UserId == userId && item.Expenses.Count == 1)), Times.Once);
    }

    [Test]
    [Description("Recovers a legacy PayPal address from its unmasked display label when the dedicated snapshot field is empty.")]
    public async Task GetReimbursement_WhenLegacyPayPalSnapshot_ReturnsAddressForDetailsDialog()
    {
        // Arrange
        var reimbursement = new Reimbursement
        {
            PaymentMethod = new PaymentMethodSnapshot
            {
                Type = (int)PaymentInformationType.PayPal,
                DisplayName = "PayPal · legacy@example.org"
            }
        };
        _repository.Setup(repository => repository.GetReimbursementAsync(reimbursement.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reimbursement);

        // Act
        var result = await _service.GetReimbursementAsync(reimbursement.Id, null);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.PaymentMethod.PayPalEmail, Is.EqualTo("legacy@example.org"));
    }

    [Test]
    [Description("Allows a reimbursement owner to cancel a reimbursement that has not reached a terminal state.")]
    public async Task CancelReimbursement_WhenOwnedAndActive_MarksItCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reimbursement = new Reimbursement
        {
            UserId = userId,
            Status = (int)DisbursementStatus.UnderReview,
            PaymentMethod = new PaymentMethodSnapshot()
        };
        _repository.Setup(repository => repository.GetReimbursementAsync(reimbursement.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reimbursement);
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelReimbursementAsync(reimbursement.Id, userId);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(DisbursementStatus.Cancelled));
        Assert.That(reimbursement.Status, Is.EqualTo((int)DisbursementStatus.Cancelled));
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Description("Does not allow a user to cancel another user's reimbursement.")]
    public async Task CancelReimbursement_WhenOwnedByAnotherUser_ReturnsFailure()
    {
        // Arrange
        var reimbursement = new Reimbursement
        {
            UserId = Guid.NewGuid(),
            Status = (int)DisbursementStatus.Submitted
        };
        _repository.Setup(repository => repository.GetReimbursementAsync(reimbursement.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reimbursement);

        // Act
        var result = await _service.CancelReimbursementAsync(reimbursement.Id, Guid.NewGuid());

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo("Reimbursement not found."));
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Description("Does not allow an owner to cancel a reimbursement after it has been paid.")]
    public async Task CancelReimbursement_WhenAlreadyPaid_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reimbursement = new Reimbursement
        {
            UserId = userId,
            Status = (int)DisbursementStatus.Paid
        };
        _repository.Setup(repository => repository.GetReimbursementAsync(reimbursement.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reimbursement);

        // Act
        var result = await _service.CancelReimbursementAsync(reimbursement.Id, userId);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("no longer"));
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Description("Rejects an allocation claim when existing active applications leave insufficient funds.")]
    public async Task Apply_WhenClaimExceedsRemainingAmount_ReturnsFailure()
    {
        // Arrange
        var token = Guid.NewGuid();
        var allocation = new Allocation { Amount = 100, ShareToken = token, Applications = [new AllocationApplication { Amount = 70, Status = (int)AllocationApplicationStatus.Submitted }] };
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(allocation);

        // Act
        var result = await _service.ApplyAsync(token, Guid.NewGuid(), "User", new CreateAllocationApplicationRequest { Amount = 40, PaymentInformationId = Guid.NewGuid() });

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("remaining"));
        _payments.VerifyNoOtherCalls();
    }

    [Test]
    [Description("Prevents an applicant from approving their own allocation application.")]
    public async Task Decide_WhenApproverIsApplicant_ReturnsFailure()
    {
        // Arrange
        var token = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var application = new AllocationApplication { Id = Guid.NewGuid(), ApplicantUserId = userId };
        var allocation = new Allocation { ShareToken = token, Applications = [application] };
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(allocation);

        // Act
        var result = await _service.DecideAsync(token, application.Id, userId, "User", new DecideAllocationApplicationRequest { IsApproved = true });

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("own application"));
    }

    [Test]
    [Description("Explicitly adds a first-time allocation approval so persistence inserts it as a new entity.")]
    public async Task Decide_WhenApproverHasNotDecided_AddsNewApproval()
    {
        // Arrange
        var token = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var application = new AllocationApplication { Id = Guid.NewGuid(), ApplicantUserId = Guid.NewGuid() };
        var allocation = new Allocation { ShareToken = token, Applications = [application] };
        _repository.Setup(repository => repository.GetAllocationByTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(allocation);
        _repository.Setup(repository => repository.Add(It.IsAny<AllocationApproval>()));
        _repository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DecideAsync(token, application.Id, approverUserId, "Teammate", new DecideAllocationApplicationRequest { IsApproved = true });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Approvals, Has.Count.EqualTo(1));
        Assert.That(result.Value.Approvals.Single().IsApproved, Is.True);
        _repository.Verify(repository => repository.Add(It.Is<AllocationApproval>(approval => approval.ApproverUserId == approverUserId)), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateReimbursementRequest ValidReimbursementRequest(Guid? paymentId = null) => new()
    {
        Purpose = "Tournament supplies", PaymentInformationId = paymentId ?? Guid.NewGuid(),
        Expenses = [new CreateExpenseItemRequest { Description = "Cable", Amount = 12.50m, ReceiptIndexes = [0] }]
    };
}
