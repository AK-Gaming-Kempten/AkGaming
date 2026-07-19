namespace AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;

public interface IBallotCredentialProtector
{
    string Create(Guid ballotId);
    bool IsValid(Guid ballotId, string credential);
}
