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
        Enqueue(NotificationEventTypes.ReimbursementSubmitted, reimbursement, data);
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
        Enqueue(NotificationEventTypes.ReimbursementStatusChanged, reimbursement, data);
    }

    private void Enqueue<T>(string type, Reimbursement reimbursement, T data)
    {
        var eventId = Guid.NewGuid();
        var occurredAtUtc = DateTimeOffset.UtcNow;
        var envelope = new NotificationEnvelope(
            eventId,
            type,
            "management",
            occurredAtUtc,
            reimbursement.UserId,
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
}
