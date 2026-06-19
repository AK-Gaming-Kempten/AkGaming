using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Moq;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

public class PaymentInformationServiceTests {
    [Test]
    [Description("Creates normalized bank account information for the authenticated user's profile.")]
    public async Task CreateAsync_CreatesBankAccountForUserProfile() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var paymentRepository = new Mock<IPaymentInformationRepository>();
        var service = new PaymentInformationService(memberRepository.Object, paymentRepository.Object);
        var userId = Guid.NewGuid();
        var member = new Member { Id = Guid.NewGuid(), UserId = userId };
        PaymentInformation? created = null;
        memberRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(Result<Member>.Success(member));
        paymentRepository.Setup(x => x.Add(It.IsAny<PaymentInformation>()))
            .Callback<PaymentInformation>(item => created = item)
            .Returns(Result.Success());
        paymentRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(Result.Success());
        var request = new PaymentInformationDto {
            Type = PaymentInformationType.BankAccount,
            AccountHolder = "  Max Mustermann  ",
            Iban = "de02 1203 0000 0000 2020 51",
            Bic = "byladem1001"
        };

        // Act
        var result = await service.CreateAsync(userId, request);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(created, Is.Not.Null);
        Assert.Multiple(() => {
            Assert.That(created!.MemberId, Is.EqualTo(member.Id));
            Assert.That(created.AccountHolder, Is.EqualTo("Max Mustermann"));
            Assert.That(created.Iban, Is.EqualTo("DE02120300000000202051"));
            Assert.That(created.Bic, Is.EqualTo("BYLADEM1001"));
        });
    }

    [Test]
    [Description("Rejects PayPal payment information without an email address.")]
    public async Task CreateAsync_RejectsPayPalWithoutEmail() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var paymentRepository = new Mock<IPaymentInformationRepository>();
        var service = new PaymentInformationService(memberRepository.Object, paymentRepository.Object);

        // Act
        var result = await service.CreateAsync(Guid.NewGuid(), new PaymentInformationDto { Type = PaymentInformationType.PayPal });

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        paymentRepository.Verify(x => x.Add(It.IsAny<PaymentInformation>()), Times.Never);
    }

    [Test]
    [Description("Prevents a user from deleting payment information owned by another profile.")]
    public async Task DeleteAsync_RejectsPaymentInformationOwnedByAnotherUser() {
        // Arrange
        var memberRepository = new Mock<IMemberRepository>();
        var paymentRepository = new Mock<IPaymentInformationRepository>();
        var service = new PaymentInformationService(memberRepository.Object, paymentRepository.Object);
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        memberRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(Result<Member>.Success(new Member { Id = Guid.NewGuid(), UserId = userId }));
        paymentRepository.Setup(x => x.GetByIdAsync(itemId))
            .ReturnsAsync(Result<PaymentInformation>.Success(new PaymentInformation { Id = itemId, MemberId = Guid.NewGuid() }));

        // Act
        var result = await service.DeleteAsync(userId, itemId);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        paymentRepository.Verify(x => x.Delete(It.IsAny<PaymentInformation>()), Times.Never);
    }
}
