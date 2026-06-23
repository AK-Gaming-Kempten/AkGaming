using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace AkGaming.Core.Components.Authentication;

/// <summary>
/// Stores protected authentication tickets server-side so the browser only carries an opaque session key.
/// </summary>
public sealed class DistributedCacheTicketStore(IDistributedCache cache, string keyPrefix) : ITicketStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{keyPrefix}:{Guid.NewGuid():N}";
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(DefaultLifetime)
        };

        return cache.SetAsync(key, TicketSerializer.Default.Serialize(ticket), options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var value = await cache.GetAsync(key);
        return value is null ? null : TicketSerializer.Default.Deserialize(value);
    }

    public Task RemoveAsync(string key)
    {
        return cache.RemoveAsync(key);
    }
}
