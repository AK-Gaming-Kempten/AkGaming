using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;

public sealed class DisbursementNotificationOutbox(
    DisbursementsDbContext dbContext,
    IOptions<DisbursementNotificationOptions> options) : IDisbursementNotificationOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DisbursementNotificationOptions _options = options.Value;

    public void EnqueueSubmitted(Reimbursement reimbursement)
    {
        var data = new ReimbursementSubmittedNotification(
            reimbursement.Id,
            reimbursement.ApplicantName,
            reimbursement.Purpose,
            reimbursement.Expenses.Sum(expense => expense.Amount),
            ((DisbursementStatus)reimbursement.Status).ToString(),
            BuildManagementUrl(reimbursement.Id));
        Enqueue(NotificationEventTypes.ReimbursementSubmitted, reimbursement.UserId, data);
    }

    public void EnqueueStatusChanged(Reimbursement reimbursement, DisbursementStatus previousStatus)
    {
        var data = new ReimbursementStatusChangedNotification(
            reimbursement.Id,
            reimbursement.ApplicantName,
            reimbursement.Purpose,
            reimbursement.Expenses.Sum(expense => expense.Amount),
            previousStatus.ToString(),
            ((DisbursementStatus)reimbursement.Status).ToString(),
            reimbursement.AdministrativeNote,
            BuildManagementUrl(reimbursement.Id));
        Enqueue(NotificationEventTypes.ReimbursementStatusChanged, reimbursement.UserId, data);
    }

    public void EnqueueAllocationClaimChanged(AllocationApplication application)
    {
        var allocation = application.Allocation
            ?? throw new InvalidOperationException("Allocation claim notifications require the allocation.");
        if (string.IsNullOrWhiteSpace(allocation.DiscordChannelId)
            || string.IsNullOrWhiteSpace(allocation.DiscordRoleId))
            return;

        var data = new AllocationClaimChangedNotification(
            application.Id,
            allocation.Event?.Name ?? string.Empty,
            allocation.Name,
            application.ApplicantName,
            application.Amount,
            application.Note,
            ((AllocationApplicationStatus)application.Status).ToString(),
            application.Approvals.Where(item => item.IsApproved).Select(item => item.ApproverName).Order().ToList(),
            application.Approvals.Where(item => !item.IsApproved).Select(item => item.ApproverName).Order().ToList(),
            BuildAllocationUrl(allocation.ShareToken),
            allocation.DiscordChannelId,
            allocation.DiscordRoleId);
        Enqueue(NotificationEventTypes.AllocationClaimChanged, application.ApplicantUserId, data);
    }

    public void EnqueueAllocationAvailable(Allocation allocation)
    {
        if (string.IsNullOrWhiteSpace(allocation.DiscordChannelId)
            || string.IsNullOrWhiteSpace(allocation.DiscordRoleId))
            throw new InvalidOperationException("Allocation announcements require a Discord channel and role.");

        var data = new AllocationAvailableNotification(
            allocation.Id,
            allocation.Event?.Name ?? string.Empty,
            allocation.Name,
            allocation.Description,
            allocation.Amount,
            BuildAllocationUrl(allocation.ShareToken),
            BuildGuideUrl(),
            allocation.DiscordChannelId,
            allocation.DiscordRoleId);
        Enqueue(NotificationEventTypes.AllocationAvailable, null, data);
    }

    private void Enqueue<T>(string type, Guid? subjectUserId, T data)
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.UtcNow;
        var envelope = new NotificationEnvelope(
            eventId,
            type,
            "management",
            occurredAtUtc,
            subjectUserId,
            JsonSerializer.SerializeToElement(data, JsonOptions));
        dbContext.NotificationOutbox.Add(new NotificationOutboxMessage
        {
            EventId = eventId,
            Type = type,
            PayloadJson = JsonSerializer.Serialize(envelope, JsonOptions),
            CreatedAtUtc = occurredAtUtc
        });
    }

    private string? BuildManagementUrl(Guid reimbursementId)
    {
        var frontendBaseUrl = NotificationUrlBuilder.ManagementFrontendBaseUrl(
            _options.ManagementFrontendBaseUrl, _options.ManagementBaseUrl);
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            return null;
        return $"{frontendBaseUrl}/disbursements/reimbursements/my?reimbursement={reimbursementId}";
    }

    private string? BuildAllocationUrl(Guid shareToken)
    {
        var frontendBaseUrl = NotificationUrlBuilder.ManagementFrontendBaseUrl(
            _options.ManagementFrontendBaseUrl, _options.ManagementBaseUrl);
        return string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? null
            : $"{frontendBaseUrl}/disbursements/claim/{shareToken}";
    }

    private string? BuildGuideUrl()
    {
        var frontendBaseUrl = NotificationUrlBuilder.ManagementFrontendBaseUrl(
            _options.ManagementFrontendBaseUrl, _options.ManagementBaseUrl);
        return string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? null
            : $"{frontendBaseUrl}/guides/disbursement-claim-guide-de.png";
    }
}
