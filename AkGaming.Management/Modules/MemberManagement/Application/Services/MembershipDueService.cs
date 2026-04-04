using AkGaming.Core.Common.Generics;
using AkGaming.Core.Common.Email;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MembershipDueService(
    IMembershipDueRepository dueRepository,
    IMembershipPaymentPeriodRepository paymentPeriodRepository,
    IMemberRepository memberRepository,
    IEmailSender? emailSender = null)
    : IMembershipDueService
{
    /// <inheritdoc />
    public async Task<Result<MembershipPaymentPeriodDto>> CreatePaymentPeriodAsync(MembershipPaymentPeriodCreateDto request, Guid? performedByUserId = null) {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<MembershipPaymentPeriodDto>.Failure("Payment period name is required.");

        var membersResult = await memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(membersResult.Error ?? "Members could not be loaded.");
        var members = membersResult.Value!;

        var paymentPeriod = request.ToMembershipPaymentPeriod();
        var addPaymentPeriodResult = paymentPeriodRepository.Add(paymentPeriod);
        if (!addPaymentPeriodResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(addPaymentPeriodResult.Error ?? "Payment period could not be created.");

        var activeMembers = members.Where(member => QualifiesForDue(member, paymentPeriod)).ToList();
        var dues = activeMembers.Select(member => new MembershipDue {
            MemberId = member.Id,
            PaymentPeriod = paymentPeriod,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = QualifiesForReducedDue(member, paymentPeriod) ? request.ReducedDueAmount : request.DefaultDueAmount,
            PaidAmount = null,
            DueDate = request.DueDate,
            SettledAt = null,
            SettlementReference = null
        }).ToList();

        if (dues.Count > 0) {
            var addDuesResult = dueRepository.AddRange(dues);
            if (!addDuesResult.IsSuccess)
                return Result<MembershipPaymentPeriodDto>.Failure(addDuesResult.Error ?? "Membership dues could not be created.");
        }

        var saveResult = await dueRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<MembershipPaymentPeriodDto>.Failure(saveResult.Error ?? "Changes could not be saved.");

        return Result<MembershipPaymentPeriodDto>.Success(paymentPeriod.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipPaymentPeriodDto>>> GetPaymentPeriodsAsync() {
        var paymentPeriodsResult = await paymentPeriodRepository.GetAllAsync();
        if (!paymentPeriodsResult.IsSuccess)
            return Result<ICollection<MembershipPaymentPeriodDto>>.Failure(paymentPeriodsResult.Error ?? "Payment periods could not be loaded.");
        var paymentPeriods = paymentPeriodsResult.Value!;

        return Result<ICollection<MembershipPaymentPeriodDto>>.Success(paymentPeriods.Select(x => x.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetCurrentPaymentPeriodDuesAsync() {
        var currentPeriodResult = await paymentPeriodRepository.GetCurrentAsync();
        if (!currentPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(currentPeriodResult.Error ?? "Current payment period not found.");
        var currentPeriod = currentPeriodResult.Value!;

        var duesResult = await dueRepository.GetByPaymentPeriodIdAsync(currentPeriod.Id);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetPaymentPeriodDuesAsync(int paymentPeriodId) {
        var duesResult = await dueRepository.GetByPaymentPeriodIdAsync(paymentPeriodId);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> AddMembersToPaymentPeriodAsync(int paymentPeriodId, ICollection<Guid> memberIds, Guid? performedByUserId = null) {
        if (memberIds.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Failure("At least one member id must be provided.");

        var paymentPeriodResult = await paymentPeriodRepository.GetByIdAsync(paymentPeriodId);
        if (!paymentPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(paymentPeriodResult.Error ?? "Payment period not found.");
        var paymentPeriod = paymentPeriodResult.Value!;

        var duesForPeriodResult = await dueRepository.GetByPaymentPeriodIdAsync(paymentPeriodId);
        if (!duesForPeriodResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesForPeriodResult.Error ?? "Dues could not be loaded.");
        var duesForPeriod = duesForPeriodResult.Value!;
        var existingMemberIds = duesForPeriod.Select(d => d.MemberId).ToHashSet();

        var requestedMemberIds = memberIds.Distinct().Where(id => !existingMemberIds.Contains(id)).ToList();
        if (requestedMemberIds.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Success(duesForPeriod.Select(d => d.ToDto()).ToList());

        var membersResult = await memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(membersResult.Error ?? "Members could not be loaded.");
        var members = membersResult.Value!;
        var validMembers = members.Where(m => requestedMemberIds.Contains(m.Id)).ToList();

        if (validMembers.Count == 0)
            return Result<ICollection<MembershipDueDto>>.Failure("No valid members were provided.");

        var duesToAdd = validMembers.Select(member => new MembershipDue {
            MemberId = member.Id,
            PaymentPeriodId = paymentPeriod.Id,
            Status = DomainEnums.MembershipDueStatus.Pending,
            DueAmount = QualifiesForReducedDue(member, paymentPeriod) ? paymentPeriod.ReducedDueAmount : paymentPeriod.DefaultDueAmount,
            PaidAmount = null,
            DueDate = paymentPeriod.DueDate,
            SettledAt = null,
            SettlementReference = null
        }).ToList();

        var addResult = dueRepository.AddRange(duesToAdd);
        if (!addResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(addResult.Error ?? "New dues could not be added.");

        var saveResult = await dueRepository.SaveChangesAsync();
        if (!saveResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(saveResult.Error ?? "Changes could not be saved.");

        var updatedDues = duesForPeriod.Concat(duesToAdd).Select(d => d.ToDto()).ToList();
        return Result<ICollection<MembershipDueDto>>.Success(updatedDues);
    }

    /// <inheritdoc />
    public async Task<Result<ICollection<MembershipDueDto>>> GetDuesForMemberAsync(Guid memberId) {
        var duesResult = await dueRepository.GetByMemberIdAsync(memberId);
        if (!duesResult.IsSuccess)
            return Result<ICollection<MembershipDueDto>>.Failure(duesResult.Error ?? "Dues not found.");
        var dues = duesResult.Value!;

        return Result<ICollection<MembershipDueDto>>.Success(dues.Select(d => d.ToDto()).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<MembershipDueEmailPreviewDto>> GetReminderEmailPreviewAsync(int dueId) {
        var reminderContextResult = await LoadReminderContextAsync(dueId);
        if (!reminderContextResult.IsSuccess)
            return Result<MembershipDueEmailPreviewDto>.Failure(reminderContextResult.Error ?? "Reminder context could not be loaded.");
        var reminderContext = reminderContextResult.Value!;

        var eligibility = EvaluateReminderEligibility(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        if (!eligibility.IsSendable)
            return Result<MembershipDueEmailPreviewDto>.Failure(eligibility.Reason ?? "Reminder email is not available.");

        var preview = MembershipDueReminderEmailComposer.Compose(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        return Result<MembershipDueEmailPreviewDto>.Success(preview);
    }

    /// <inheritdoc />
    public async Task<Result<MembershipDueReminderDispatchPreviewDto>> GetReminderDispatchPreviewForPaymentPeriodAsync(int paymentPeriodId) {
        var paymentPeriodResult = await paymentPeriodRepository.GetByIdAsync(paymentPeriodId);
        if (!paymentPeriodResult.IsSuccess)
            return Result<MembershipDueReminderDispatchPreviewDto>.Failure(paymentPeriodResult.Error ?? "Payment period not found.");
        var paymentPeriod = paymentPeriodResult.Value!;

        var membersResult = await memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<MembershipDueReminderDispatchPreviewDto>.Failure(membersResult.Error ?? "Members could not be loaded.");
        var members = membersResult.Value!;

        var duesResult = await dueRepository.GetByPaymentPeriodIdAsync(paymentPeriodId);
        if (!duesResult.IsSuccess)
            return Result<MembershipDueReminderDispatchPreviewDto>.Failure(duesResult.Error ?? "Dues could not be loaded.");
        var dues = duesResult.Value!;

        var duesByMemberId = dues
            .GroupBy(due => due.MemberId)
            .ToDictionary(group => group.Key, group => group.First());

        var recipients = new List<MembershipDueReminderRecipientDto>();
        var skippedMembers = new List<MembershipDueReminderSkipDto>();

        foreach (var member in members.OrderBy(BuildMemberDisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(member => member.Id)) {
            if (!duesByMemberId.TryGetValue(member.Id, out var due)) {
                skippedMembers.Add(new MembershipDueReminderSkipDto {
                    MemberId = member.Id,
                    MemberDisplayName = BuildMemberDisplayName(member),
                    Reason = "No due exists in this payment period."
                });
                continue;
            }

            var eligibility = EvaluateReminderEligibility(member, paymentPeriod, due);
            if (eligibility.IsSendable) {
                recipients.Add(new MembershipDueReminderRecipientDto {
                    DueId = due.Id,
                    MemberId = member.Id,
                    MemberDisplayName = BuildMemberDisplayName(member),
                    Email = member.Email!.Trim(),
                    DueAmount = due.DueAmount,
                    DueDate = due.DueDate
                });
                continue;
            }

            skippedMembers.Add(new MembershipDueReminderSkipDto {
                MemberId = member.Id,
                MemberDisplayName = BuildMemberDisplayName(member),
                Reason = eligibility.Reason ?? "Reminder email is not available."
            });
        }

        return Result<MembershipDueReminderDispatchPreviewDto>.Success(new MembershipDueReminderDispatchPreviewDto {
            PaymentPeriodId = paymentPeriod.Id,
            PaymentPeriodName = paymentPeriod.Name,
            Recipients = recipients,
            SkippedMembers = skippedMembers
        });
    }

    /// <inheritdoc />
    public async Task<Result<MembershipDueReminderDispatchPreviewDto>> GetReminderDispatchPreviewForDueAsync(int dueId) {
        var reminderContextResult = await LoadReminderContextAsync(dueId);
        if (!reminderContextResult.IsSuccess)
            return Result<MembershipDueReminderDispatchPreviewDto>.Failure(reminderContextResult.Error ?? "Reminder context could not be loaded.");
        var reminderContext = reminderContextResult.Value!;

        var eligibility = EvaluateReminderEligibility(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        var recipients = new List<MembershipDueReminderRecipientDto>();
        var skippedMembers = new List<MembershipDueReminderSkipDto>();

        if (eligibility.IsSendable) {
            recipients.Add(new MembershipDueReminderRecipientDto {
                DueId = reminderContext.Due.Id,
                MemberId = reminderContext.Member.Id,
                MemberDisplayName = BuildMemberDisplayName(reminderContext.Member),
                Email = reminderContext.Member.Email!.Trim(),
                DueAmount = reminderContext.Due.DueAmount,
                DueDate = reminderContext.Due.DueDate
            });
        }
        else {
            skippedMembers.Add(new MembershipDueReminderSkipDto {
                MemberId = reminderContext.Member.Id,
                MemberDisplayName = BuildMemberDisplayName(reminderContext.Member),
                Reason = eligibility.Reason ?? "Reminder email is not available."
            });
        }

        return Result<MembershipDueReminderDispatchPreviewDto>.Success(new MembershipDueReminderDispatchPreviewDto {
            PaymentPeriodId = reminderContext.PaymentPeriod.Id,
            PaymentPeriodName = reminderContext.PaymentPeriod.Name,
            Recipients = recipients,
            SkippedMembers = skippedMembers
        });
    }

    /// <inheritdoc />
    public async Task<Result> SendReminderEmailAsync(int dueId) {
        if (emailSender is null)
            return Result.Failure("Email sender is not configured.");

        var reminderContextResult = await LoadReminderContextAsync(dueId);
        if (!reminderContextResult.IsSuccess)
            return Result.Failure(reminderContextResult.Error ?? "Reminder context could not be loaded.");
        var reminderContext = reminderContextResult.Value!;

        var eligibility = EvaluateReminderEligibility(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        if (!eligibility.IsSendable)
            return Result.Failure(eligibility.Reason ?? "Reminder email cannot be sent.");

        var preview = MembershipDueReminderEmailComposer.Compose(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);

        try {
            await emailSender.SendAsync(
                preview.RecipientEmail,
                preview.Subject,
                preview.TextBody,
                preview.HtmlBody,
                CancellationToken.None);
            return Result.Success();
        }
        catch (Exception exception) {
            return Result.Failure($"Failed to send reminder email: {exception.Message}");
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> UpdateDueAsync(int dueId, MembershipDueDto due, Guid? performedByUserId = null) {
        var dueResult = await dueRepository.GetByIdAsync(dueId);
        if (!dueResult.IsSuccess)
            return dueResult;
        var existingDue = dueResult.Value!;

        existingDue.Status = (DomainEnums.MembershipDueStatus)due.Status;
        existingDue.DueAmount = due.DueAmount;
        existingDue.PaidAmount = due.PaidAmount;
        existingDue.DueDate = due.DueDate;
        existingDue.SettledAt = due.SettledAt;
        existingDue.SettlementReference = due.SettlementReference;

        var updateResult = dueRepository.Update(existingDue);
        if (!updateResult.IsSuccess)
            return updateResult;

        return await dueRepository.SaveChangesAsync();
    }

    private static bool QualifiesForDue(Member member, MembershipPaymentPeriod paymentPeriod) {
        if (QualifiesForReducedDue(member, paymentPeriod))
            return true;

        if (member.Status is DomainEnums.MembershipStatus.Member or DomainEnums.MembershipStatus.HonoraryMember)
            return true;

        if (member.Status != DomainEnums.MembershipStatus.InTrial)
            return false;

        var inTrialStart = member.StatusChanges
            .Where(sc => sc.NewStatus == DomainEnums.MembershipStatus.InTrial)
            .OrderBy(sc => sc.Timestamp)
            .FirstOrDefault();

        if (inTrialStart is null)
            return false;

        var trialEndDate = DateOnly.FromDateTime(inTrialStart.Timestamp.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays));
        return trialEndDate <= paymentPeriod.DueDate.AddMonths(3);
    }

    private static bool QualifiesForReducedDue(Member member, MembershipPaymentPeriod paymentPeriod)
    {
        return member.Status == DomainEnums.MembershipStatus.SupportingMember;
    }

    private async Task<Result<ReminderContext>> LoadReminderContextAsync(int dueId) {
        var dueResult = await dueRepository.GetByIdAsync(dueId);
        if (!dueResult.IsSuccess)
            return Result<ReminderContext>.Failure(dueResult.Error ?? "Due not found.");
        var due = dueResult.Value!;

        var memberResult = await memberRepository.GetByMemberIdAsync(due.MemberId);
        if (!memberResult.IsSuccess)
            return Result<ReminderContext>.Failure(memberResult.Error ?? "Member not found.");
        var member = memberResult.Value!;

        var paymentPeriodResult = await paymentPeriodRepository.GetByIdAsync(due.PaymentPeriodId);
        if (!paymentPeriodResult.IsSuccess)
            return Result<ReminderContext>.Failure(paymentPeriodResult.Error ?? "Payment period not found.");
        var paymentPeriod = paymentPeriodResult.Value!;

        return Result<ReminderContext>.Success(new ReminderContext(due, member, paymentPeriod));
    }

    private static ReminderEligibility EvaluateReminderEligibility(Member member, MembershipPaymentPeriod paymentPeriod, MembershipDue due) {
        if (due.PaymentPeriodId != paymentPeriod.Id)
            return ReminderEligibility.Skip("Due does not belong to the selected payment period.");

        if (due.Status != DomainEnums.MembershipDueStatus.Pending) {
            return ReminderEligibility.Skip(due.Status switch {
                DomainEnums.MembershipDueStatus.Paid => "Due is already paid.",
                DomainEnums.MembershipDueStatus.Waived => "Due has been waived.",
                DomainEnums.MembershipDueStatus.Cancelled => "Due has been cancelled.",
                _ => $"Due is not eligible because its status is {due.Status}."
            });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (due.DueDate >= today)
            return ReminderEligibility.Skip("Due date has not passed yet.");

        if (string.IsNullOrWhiteSpace(member.Email))
            return ReminderEligibility.Skip("Member has no email address.");

        return ReminderEligibility.Sendable();
    }

    private static string BuildMemberDisplayName(Member member) {
        var fullName = string.Join(" ", new[] { member.FirstName?.Trim(), member.LastName?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        if (!string.IsNullOrWhiteSpace(member.Email))
            return member.Email.Trim();

        return member.Id.ToString();
    }

    private sealed record ReminderContext(MembershipDue Due, Member Member, MembershipPaymentPeriod PaymentPeriod);

    private sealed record ReminderEligibility(bool IsSendable, string? Reason) {
        public static ReminderEligibility Sendable() => new(true, null);
        public static ReminderEligibility Skip(string reason) => new(false, reason);
    }
}
