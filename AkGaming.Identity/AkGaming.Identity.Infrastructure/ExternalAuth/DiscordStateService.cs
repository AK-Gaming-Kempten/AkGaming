using System.Text.Json;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.ExternalAuth;
using Microsoft.AspNetCore.DataProtection;

namespace AkGaming.Identity.Infrastructure.ExternalAuth;

public sealed class DiscordStateService : IDiscordStateService
{
    private readonly IDataProtector _protector;

    public DiscordStateService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("AkGaming.Identity.DiscordState.v1");
    }

    public string CreateState(DiscordOAuthState state)
    {
        var payload = JsonSerializer.Serialize(state);
        return _protector.Protect(payload);
    }

    public DiscordOAuthState? ReadState(string protectedState)
    {
        try
        {
            var unprotected = _protector.Unprotect(protectedState);
            return JsonSerializer.Deserialize<DiscordOAuthState>(unprotected);
        }
        catch
        {
            return null;
        }
    }
}
