using System.Globalization;
using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Domain;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Application;

public sealed class NotificationRoutingOptions
{
    public const string SectionName = "NotificationRouting";
    public string? TreasurerRoleId { get; set; }
}

public sealed class NotificationRenderer(IOptions<NotificationRoutingOptions> options) : INotificationRenderer
{
    private readonly NotificationRoutingOptions _options = options.Value;

    public RenderedNotification Render(NotificationInboxItem notification)
    {
        return notification.Type switch
        {
            NotificationEventTypes.ReimbursementSubmitted => RenderSubmitted(notification),
            NotificationEventTypes.ReimbursementStatusChanged => RenderStatusChanged(notification),
            _ => throw new InvalidOperationException($"Unsupported notification type '{notification.Type}'.")
        };
    }

    private RenderedNotification RenderSubmitted(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<ReimbursementSubmittedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The reimbursement submission payload is invalid.");
        var amount = data.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("de-DE"));
        var body = $"{data.ApplicantName} submitted **{data.Purpose}** for **{amount} EUR**.";
        var channel = new RenderedMessage("New expense reimbursement", body, data.ManagementUrl, _options.TreasurerRoleId);
        var direct = new RenderedMessage(
            "Reimbursement submitted",
            $"Your reimbursement **{data.Purpose}** for **{amount} EUR** was submitted successfully. We will notify you when its status changes.",
            data.ManagementUrl);
        return new RenderedNotification(channel, direct);
    }

    private static RenderedNotification RenderStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<ReimbursementStatusChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The reimbursement status payload is invalid.");
        var amount = data.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("de-DE"));
        var note = string.IsNullOrWhiteSpace(data.AdministrativeNote) ? string.Empty : $"\n\nNote: {data.AdministrativeNote}";
        var direct = new RenderedMessage(
            "Reimbursement status updated",
            $"Your reimbursement **{data.Purpose}** for **{amount} EUR** changed from **{data.PreviousStatus}** to **{data.Status}**.{note}",
            data.ManagementUrl);
        return new RenderedNotification(null, direct);
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
