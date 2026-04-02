using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AkGaming.Management.Frontend.Components.DevTools;

public partial class Debug {
    [Inject(Key = "ManagementApi")] private HttpClient Api { get; set; } = default!;
}
