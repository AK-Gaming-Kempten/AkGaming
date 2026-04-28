using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PlayerRankSelect : ComponentBase
{
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public int? Value { get; set; }
    [Parameter] public EventCallback<int?> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private int MinimumRating => PlayerRankFormatter.GetMinimumRating(GameId);
    private int SliderMaximumRating => PlayerRankFormatter.GetSliderMaximumRating(GameId);
    private int EffectiveValue => Math.Max(Value ?? MinimumRating, MinimumRating);
    private int SliderValue => Math.Clamp(EffectiveValue, MinimumRating, SliderMaximumRating);

    private Task HandleSliderChanged(ChangeEventArgs args)
    {
        return TryParseAndSet(args);
    }

    private Task HandleNumberChanged(ChangeEventArgs args)
    {
        return TryParseAndSet(args);
    }

    private Task TryParseAndSet(ChangeEventArgs args)
    {
        var value = args.Value?.ToString();
        return int.TryParse(value, out var rating)
            ? ValueChanged.InvokeAsync(Math.Max(rating, MinimumRating))
            : ValueChanged.InvokeAsync(null);
    }
}
