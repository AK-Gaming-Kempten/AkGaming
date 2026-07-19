using System.Security.Cryptography;
using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Security;

public sealed class DataProtectionBallotCredentialProtector(IDataProtectionProvider provider) : IBallotCredentialProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("AkGaming.GeneralMeetings.AnonymousBallotCredential.v1");
    public string Create(Guid ballotId)
    {
        var payload = $"{ballotId:N}:{Convert.ToHexString(RandomNumberGenerator.GetBytes(32))}";
        return _protector.Protect(payload);
    }
    public bool IsValid(Guid ballotId, string credential)
    {
        if (string.IsNullOrWhiteSpace(credential)) return false;
        try { return _protector.Unprotect(credential).StartsWith($"{ballotId:N}:", StringComparison.Ordinal); }
        catch (CryptographicException) { return false; }
    }
}
