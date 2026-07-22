using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Notifications;

public sealed class MemberNotificationOutbox(
    MemberManagementDbContext dbContext,
    IOptions<MemberNotificationOptions> options) : IMemberNotificationOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MemberNotificationOptions _options = options.Value;

    public void EnqueueMembershipApplicationCreated(MembershipApplicationRequest request)
    {
        var data = new MembershipApplicationCreatedNotification(
            request.Id,
            GetDisplayName(request.FirstName, request.LastName, request.Email),
            request.Email,
            BuildRequestsUrl());
        Enqueue(NotificationEventTypes.MembershipApplicationCreated, request.IssuingUserId, data);
    }

    public void EnqueueMemberLinkingRequestCreated(MemberLinkingRequest request)
    {
        var data = new MemberLinkingRequestCreatedNotification(
            request.Id,
            GetDisplayName(request.FirstName, request.LastName, request.Email),
            request.Email,
            request.Reason.ToString(),
            BuildRequestsUrl());
        Enqueue(NotificationEventTypes.MemberLinkingRequestCreated, request.IssuingUserId, data);
    }

    private void Enqueue<T>(string type, Guid subjectUserId, T data)
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.UtcNow;
        var envelope = new NotificationEnvelope(eventId, type, "management", occurredAtUtc,
            subjectUserId, JsonSerializer.SerializeToElement(data, JsonOptions));
        dbContext.NotificationOutbox.Add(new MemberNotificationOutboxMessage
        {
            EventId = eventId,
            Type = type,
            PayloadJson = JsonSerializer.Serialize(envelope, JsonOptions),
            CreatedAtUtc = occurredAtUtc
        });
    }

    private string? BuildRequestsUrl()
    {
        return string.IsNullOrWhiteSpace(_options.ManagementBaseUrl)
            ? null
            : $"{_options.ManagementBaseUrl.TrimEnd('/')}/member-management/requests";
    }

    private static string GetDisplayName(string? firstName, string? lastName, string? email)
    {
        var name = string.Join(' ', new[] { firstName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return string.IsNullOrWhiteSpace(name) ? email ?? "Unknown applicant" : name;
    }
}
