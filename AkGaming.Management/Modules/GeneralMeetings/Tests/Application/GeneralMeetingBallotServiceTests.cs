using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
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
public sealed class GeneralMeetingBallotServiceTests
{
    private InMemoryMeetingRepository _repository = null!;
    private Mock<IMemberQueryService> _members = null!;
    private Mock<IEmailSender> _email = null!;
    private TestCredentialProtector _credentials = null!;
    private GeneralMeetingService _service = null!;
    private GeneralMeeting _meeting = null!;
    private Ballot _firstBallot = null!;
    private Ballot _secondBallot = null!;
    private List<MemberDto> _memberRecords = null!;

    [SetUp]
    public void SetUp()
    {
        _memberRecords =
        [
            Member(MembershipStatus.SupportingMember),
            Member(MembershipStatus.Suspended),
            Member(MembershipStatus.InTrial)
        ];
        _firstBallot = Ballot("First vote");
        _secondBallot = Ballot("Second vote");
        var agenda = new AgendaItem { Heading = "Membership changes", Ballots = [_firstBallot, _secondBallot] };
        _meeting = new GeneralMeeting { Status = MeetingStatus.InProgress, AgendaItems = [agenda] };
        foreach (var member in _memberRecords) _meeting.Attendees.Add(new Attendance { MeetingId = _meeting.Id, MemberId = member.Id, UserId = member.UserId, DisplayName = member.FirstName!, CheckedInAt = DateTimeOffset.UtcNow });
        _repository = new InMemoryMeetingRepository(_meeting);
        _members = new Mock<IMemberQueryService>(MockBehavior.Strict);
        _members.Setup(x => x.GetAllMembersAsync()).ReturnsAsync(() => Result<ICollection<MemberDto>>.Success(_memberRecords));
        _email = new Mock<IEmailSender>(MockBehavior.Strict);
        _credentials = new TestCredentialProtector();
        _service = new GeneralMeetingService(_repository, _members.Object, _email.Object, _credentials);
    }

    [Test]
    [Description("Snapshots supporting and suspended members as eligible while excluding trial members when a ballot opens.")]
    public async Task OpenBallot_UsesCurrentVotingMembershipStatuses()
    {
        // Arrange
        var expectedMemberIds = _memberRecords.Take(2).Select(x => x.Id).ToArray();

        // Act
        var result = await _service.OpenBallotAsync(_meeting.Id, _firstBallot.Id, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_firstBallot.Entitlements.Select(x => x.MemberId), Is.EquivalentTo(expectedMemberIds));
        Assert.That(_firstBallot.Credentials, Is.Empty, "Issuance must not create a database-correlatable credential row.");
    }

    [Test]
    [Description("Re-evaluates changed member statuses for each new ballot without altering an earlier eligibility snapshot.")]
    public async Task OpenBallot_AfterMembershipChange_UsesNewStatusOnlyForLaterBallot()
    {
        // Arrange
        await _service.OpenBallotAsync(_meeting.Id, _firstBallot.Id, Guid.NewGuid(), CancellationToken.None);
        _memberRecords[2].Status = MembershipStatus.Member;

        // Act
        var result = await _service.OpenBallotAsync(_meeting.Id, _secondBallot.Id, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_firstBallot.Entitlements, Has.Count.EqualTo(2));
        Assert.That(_secondBallot.Entitlements, Has.Count.EqualTo(3));
    }

    [Test]
    [Description("Consumes an anonymous credential once and stores the selection without a member identifier.")]
    public async Task CastVote_WithIssuedCredential_StoresAnonymousSingleUseVote()
    {
        // Arrange
        await _service.OpenBallotAsync(_meeting.Id, _firstBallot.Id, Guid.NewGuid(), CancellationToken.None);
        var memberId = _memberRecords[0].Id;
        var issued = await _service.IssueCredentialAsync(_firstBallot.Id, memberId, CancellationToken.None);
        var optionId = _firstBallot.Options.First().Id;

        // Act
        var first = await _service.CastVoteAsync(_firstBallot.Id, new CastVoteRequest(issued.Value!.Credential, [optionId]), CancellationToken.None);
        var repeated = await _service.CastVoteAsync(_firstBallot.Id, new CastVoteRequest(issued.Value.Credential, [optionId]), CancellationToken.None);

        // Assert
        Assert.That(first.IsSuccess, Is.True);
        Assert.That(first.Value, Is.EqualTo(_meeting.Id));
        Assert.That(repeated.IsSuccess, Is.False);
        Assert.That(_firstBallot.Votes, Has.Count.EqualTo(1));
        Assert.That(typeof(AnonymousVote).GetProperties().Select(x => x.Name), Does.Not.Contain("MemberId"));
        Assert.That(typeof(AnonymousCredential).GetProperties().Select(x => x.Name), Does.Not.Contain("MemberId"));
    }

    [Test]
    [Description("Creates a new draft ballot when no existing ballot identifier is supplied.")]
    public async Task SaveBallotAsync_WithoutExistingBallot_CreatesDraft()
    {
        // Arrange
        var agendaItem = _meeting.AgendaItems.Single();
        var request = new SaveBallotRequest("Approve the proposal?", BallotTypeDto.YesNoAbstain, [], 1, false);

        // Act
        var result = await _service.SaveBallotAsync(_meeting.Id, agendaItem.Id, null, request, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(BallotStatusDto.Draft));
        Assert.That(result.Value.Options.Select(x => x.Text), Is.EqualTo(new[] { "Yes", "No", "Abstain" }));
    }

    private static MemberDto Member(MembershipStatus status) => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), FirstName = status.ToString(), Status = status };
    private static Ballot Ballot(string question)
    {
        var ballot = new Ballot { Question = question, Type = BallotType.YesNoAbstain, MaximumSelections = 1 };
        ballot.Options.Add(new BallotOption { BallotId = ballot.Id, Text = "Yes", Order = 0 });
        ballot.Options.Add(new BallotOption { BallotId = ballot.Id, Text = "No", Order = 1 });
        ballot.Options.Add(new BallotOption { BallotId = ballot.Id, Text = "Abstain", Order = 2 });
        return ballot;
    }

    private sealed class InMemoryMeetingRepository(GeneralMeeting meeting) : IGeneralMeetingRepository
    {
        public Task<IReadOnlyList<GeneralMeeting>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GeneralMeeting>>([meeting]);
        public Task<GeneralMeeting?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == meeting.Id ? meeting : null);
        public Task<Ballot?> GetBallotAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(meeting.AgendaItems.SelectMany(x => x.Ballots).SingleOrDefault(x => x.Id == id));
        public Task<Guid?> GetMeetingIdForBallotAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Guid?>(meeting.AgendaItems.SelectMany(x => x.Ballots).Any(x => x.Id == id) ? meeting.Id : null);
        public void Add<TEntity>(TEntity entity) where TEntity : class { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestCredentialProtector : IBallotCredentialProtector
    {
        private readonly HashSet<string> _valid = [];
        public string Create(Guid ballotId) { var value = $"{ballotId:N}:{Guid.NewGuid():N}"; _valid.Add(value); return value; }
        public bool IsValid(Guid ballotId, string credential) => credential.StartsWith($"{ballotId:N}:", StringComparison.Ordinal) && _valid.Contains(credential);
    }
}
