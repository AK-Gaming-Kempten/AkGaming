using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Frontend.Components.DevTools;

public partial class Debug {
    [Inject(Key = "ManagementApi")] private HttpClient Api { get; set; } = default!;

    private static Task TriggerClientErrorAsync()
        => Task.FromException(new InvalidOperationException("Intentional debug-page test error."));
}
