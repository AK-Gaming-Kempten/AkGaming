using System.Text.Json;
using System.Text.Json.Serialization;

namespace AkGaming.Tournaments.Frontend.Api;

internal static class TournamentApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
