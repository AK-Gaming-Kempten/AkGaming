using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using AkGaming.Management.Modules.GeneralMeetings.Application.Services;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Moq;

namespace AkGaming.Management.Modules.GeneralMeetings.Tests.Application;

[TestFixture]
public sealed class GeneralMeetingInvitationServiceTests
{
    private InMemoryMeetingRepository _repository = null!;
    private Mock<IMemberQueryService> _members = null!;
    private Mock<IEmailSender> _email = null!;
    private GeneralMeetingService _service = null!;
    private GeneralMeeting _meeting = null!;
    private List<MemberDto> _memberRecords = null!;

    [SetUp]
    public void SetUp()
    {
        _meeting = new GeneralMeeting
        {
            Title = "MV Oktober 2026",
            ScheduledAt = new DateTimeOffset(2026, 8, 26, 19, 0, 0, TimeSpan.Zero),
            Location = "Discord"
        };
        var root = new AgendaItem { Heading = "Begrüßung", Order = 0 };
        var child = new AgendaItem { Heading = "Feststellung der Beschlussfähigkeit", ParentId = root.Id, Order = 0 };
        _meeting.AgendaItems.Add(root);
        _meeting.AgendaItems.Add(child);

        _memberRecords =
        [
            Member("Anna", MembershipStatus.Member, "anna@example.test"),
            Member("Theo", MembershipStatus.InTrial, "theo@example.test"),
            Member("Berta", MembershipStatus.SupportingMember, null),
            Member("Carla", MembershipStatus.Applicant, "carla@example.test"),
            Member("Ignored", MembershipStatus.None, "ignored@example.test")
        ];
        _repository = new InMemoryMeetingRepository(_meeting);
        _members = new Mock<IMemberQueryService>(MockBehavior.Strict);
        _members.Setup(service => service.GetAllMembersAsync())
            .ReturnsAsync(Result<ICollection<MemberDto>>.Success(_memberRecords));
        _email = new Mock<IEmailSender>(MockBehavior.Strict);
        _service = new GeneralMeetingService(_repository, _members.Object, _email.Object, Mock.Of<IBallotCredentialProtector>());
    }

    [Test]
    [Description("Builds a branded invitation preview with editable intro text, nested agenda items, and all non-None members plus delivery reasons.")]
    public async Task PreviewInvitationAsync_ReturnsFormattedMailAndCompleteRecipientAssessment()
    {
        // Arrange
        var request = new DispatchInvitationRequest(false, "Bitte bringt <Ausweise> mit.");

        // Act
        var result = await _service.PreviewInvitationAsync(_meeting.Id, request, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.HtmlBody, Does.Contain("linear-gradient(145deg,#0f221e,#163328)"));
        Assert.That(result.Value.HtmlBody, Does.Contain("background-color:#0f221e"));
        Assert.That(result.Value.HtmlBody, Does.Contain(ClubConstants.Urls.LogoAsset));
        Assert.That(result.Value.HtmlBody, Does.Contain("Einladung zur Mitgliederversammlung"));
        Assert.That(result.Value.HtmlBody, Does.Contain("Tagesordnung"));
        Assert.That(result.Value.HtmlBody, Does.Contain("Wichtiger Hinweis zum Abstimmungssystem"));
        Assert.That(result.Value.HtmlBody, Does.Contain(ClubConstants.Urls.ManagementMembership));
        Assert.That(result.Value.TextBody, Does.Contain("Feststellung der Beschlussfähigkeit"));
        Assert.That(result.Value.TextBody, Does.Contain("Nur so könnt ihr das neue Abstimmungssystem während der Versammlung nutzen"));
        Assert.That(result.Value.TextBody, Does.Contain("bis spätestens eine Woche vor der Versammlung einzureichen"));
        Assert.That(result.Value.HtmlBody, Does.Contain("Bitte bringt &lt;Ausweise&gt; mit."));
        Assert.That(result.Value.HtmlBody, Does.Not.Contain("Persönliche Nachricht"));
        Assert.That(result.Value.InvitationText, Is.EqualTo("Bitte bringt <Ausweise> mit."));
        Assert.That(result.Value.TextBody, Does.Contain("Mittwoch, 26. August 2026 um 21:00 Uhr"));
        Assert.That(result.Value.Recipients, Has.Count.EqualTo(4));
        Assert.That(result.Value.Recipients.Single(recipient => recipient.DisplayName == "Anna").WillReceive, Is.True);
        Assert.That(result.Value.Recipients.Single(recipient => recipient.DisplayName == "Theo").WillReceive, Is.True);
        Assert.That(result.Value.Recipients.Single(recipient => recipient.DisplayName == "Berta").ExclusionReason, Does.Contain("Keine E-Mail-Adresse"));
        Assert.That(result.Value.Recipients.Single(recipient => recipient.DisplayName == "Carla").ExclusionReason, Does.Contain("Applicant"));
        Assert.That(result.Value.Recipients.Any(recipient => recipient.DisplayName == "Ignored"), Is.False);
    }

    [Test]
    [Description("Sends the exact branded preview content only to members who have an eligible status and an email address.")]
    public async Task DispatchInvitationsAsync_SendsComposedHtmlOnlyToEligibleRecipients()
    {
        // Arrange
        string? capturedSubject = null;
        string? capturedText = null;
        string? capturedHtml = null;
        var recipientEmails = new List<string>();
        _email.Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((recipient, subject, text, html, _) =>
            {
                recipientEmails.Add(recipient);
                capturedSubject = subject;
                capturedText = text;
                capturedHtml = html;
            })
            .Returns(Task.CompletedTask);
        var request = new DispatchInvitationRequest(false, "Eigener Einladungstext");

        // Act
        var result = await _service.DispatchInvitationsAsync(_meeting.Id, request, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedSubject, Does.Contain("Einladung: MV Oktober 2026"));
        Assert.That(capturedText, Does.Contain("Eigener Einladungstext"));
        Assert.That(capturedHtml, Does.Contain("Tagesordnung"));
        Assert.That(capturedHtml, Does.Contain("Eigener Einladungstext"));
        Assert.That(recipientEmails, Is.EquivalentTo(new[] { "anna@example.test", "theo@example.test" }));
        _email.Verify(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static MemberDto Member(string name, MembershipStatus status, string? email)
    {
        return new MemberDto { Id = Guid.NewGuid(), FirstName = name, Status = status, Email = email };
    }

    private sealed class InMemoryMeetingRepository(GeneralMeeting meeting) : IGeneralMeetingRepository
    {
        public Task<IReadOnlyList<GeneralMeeting>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GeneralMeeting>>([meeting]);
        public Task<GeneralMeeting?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == meeting.Id ? meeting : null);
        public Task<Ballot?> GetBallotAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Ballot?>(null);
        public Task<Guid?> GetMeetingIdForBallotAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
        public void Add<TEntity>(TEntity entity) where TEntity : class { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
