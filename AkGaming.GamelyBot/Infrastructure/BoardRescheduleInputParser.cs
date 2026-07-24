using System.Globalization;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class BoardRescheduleInputParser(
    IOptions<DiscordInteractionOptions> options,
    BotText text)
{
    private static readonly string[] SupportedFormats = ["dd.MM.yyyy HH:mm", "d.M.yyyy H:mm", "yyyy-MM-dd HH:mm"];
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public BoardRescheduleInputParser(IOptions<DiscordInteractionOptions> options)
        : this(options, BotText.English)
    {
    }

    public BoardRescheduleInputResult Parse(string? proposedAt, string? duration)
    {
        if (!DateTime.TryParseExact(proposedAt, SupportedFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var localDateTime))
        {
            return BoardRescheduleInputResult.Failure(text["RescheduleInvalidDate"]);
        }

        localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        if (_timeZone.IsInvalidTime(localDateTime) || _timeZone.IsAmbiguousTime(localDateTime))
        {
            return BoardRescheduleInputResult.Failure(text["RescheduleAmbiguousDate"]);
        }

        if (!int.TryParse(duration, NumberStyles.None, CultureInfo.InvariantCulture, out var durationMinutes)
            || durationMinutes is < 15 or > 1440)
        {
            return BoardRescheduleInputResult.Failure(text["RescheduleInvalidDuration"]);
        }

        var proposedAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone));
        return BoardRescheduleInputResult.Success(proposedAtUtc, durationMinutes);
    }
}

public sealed record BoardRescheduleInputResult(bool IsSuccess, DateTimeOffset ProposedAtUtc, int DurationMinutes, string? Error)
{
    public static BoardRescheduleInputResult Success(DateTimeOffset proposedAtUtc, int durationMinutes)
    {
        return new BoardRescheduleInputResult(true, proposedAtUtc, durationMinutes, null);
    }

    public static BoardRescheduleInputResult Failure(string error)
    {
        return new BoardRescheduleInputResult(false, default, default, error);
    }
}
