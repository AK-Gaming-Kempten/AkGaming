using System.Net;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentApiException : Exception
{
    public TournamentApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public TournamentApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpStatusCode? StatusCode { get; }
}
