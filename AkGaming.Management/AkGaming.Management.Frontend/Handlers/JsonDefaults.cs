using System.Text.Json;
using System.Text.Json.Serialization;

namespace AkGaming.Management.Frontend.Handlers;

public static class JsonDefaults {
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };
}