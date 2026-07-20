using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.Disbursements.Application.Services;

public sealed class DisbursementService(
    IDisbursementRepository repository,
    IReceiptFileStorage fileStorage,
    IPaymentInformationService paymentInformationService,
    IDisbursementNotificationOutbox notificationOutbox) : IDisbursementService
{
    public const long MaximumReceiptSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/webp", "image/gif"
    };
    private static readonly Dictionary<string, HashSet<string>> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = new(StringComparer.OrdinalIgnoreCase) { ".pdf" },
        ["image/jpeg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
        ["image/png"] = new(StringComparer.OrdinalIgnoreCase) { ".png" },
        ["image/webp"] = new(StringComparer.OrdinalIgnoreCase) { ".webp" },
        ["image/gif"] = new(StringComparer.OrdinalIgnoreCase) { ".gif" }
    };

    public async Task<Result<IReadOnlyList<ReimbursementDto>>> GetReimbursementsAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var items = await repository.GetReimbursementsAsync(userId, cancellationToken);
        return Result<IReadOnlyList<ReimbursementDto>>.Success(items.Select(ToDto).ToList());
    }

    public async Task<Result<ReimbursementDto>> GetReimbursementAsync(Guid id, Guid? ownerUserId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetReimbursementAsync(id, cancellationToken);
        if (item is null || ownerUserId.HasValue && item.UserId != ownerUserId.Value)
            return Result<ReimbursementDto>.Failure("Reimbursement not found.");

        return Result<ReimbursementDto>.Success(ToDto(item));
    }

    public async Task<Result<ReimbursementDto>> CreateReimbursementAsync(Guid userId, string applicantName, CreateReimbursementRequest request, IReadOnlyList<ReceiptUpload> files, CancellationToken cancellationToken = default)
    {
        var validation = ValidateReimbursement(request, files);
        if (!validation.IsSuccess)
            return Result<ReimbursementDto>.Failure(validation.Error!);

        var paymentResult = await GetOwnedPaymentMethodAsync(userId, request.PaymentInformationId);
        if (!paymentResult.IsSuccess)
            return Result<ReimbursementDto>.Failure(paymentResult.Error!);

        var now = DateTimeOffset.UtcNow;
        var entity = new Reimbursement
        {
            UserId = userId,
            ApplicantName = applicantName,
            Purpose = request.Purpose.Trim(),
            Note = Clean(request.Note),
            Status = (int)DisbursementStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now,
            PaymentMethod = Snapshot(paymentResult.Value!)
        };

        for (var expenseIndex = 0; expenseIndex < request.Expenses.Count; expenseIndex++)
        {
            var source = request.Expenses[expenseIndex];
            var expense = new ExpenseItem
            {
                Description = source.Description.Trim(),
                Amount = source.Amount,
                IncurredOn = source.IncurredOn
            };

            foreach (var fileIndex in source.ReceiptIndexes.Distinct())
            {
                var upload = files[fileIndex];
                var receipt = new Receipt
                {
                    FileName = Path.GetFileName(upload.FileName),
                    ContentType = upload.ContentType,
                    Size = upload.Size
                };
                receipt.StorageKey = await fileStorage.SaveAsync(receipt.Id, receipt.FileName, upload.Content, cancellationToken);
                expense.Receipts.Add(receipt);
            }

            entity.Expenses.Add(expense);
        }

        repository.Add(entity);
        notificationOutbox.EnqueueSubmitted(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<ReimbursementDto>.Success(ToDto(entity));
    }

    public async Task<Result<ReimbursementDto>> UpdateReimbursementStatusAsync(Guid id, UpdateReimbursementStatusRequest request, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetReimbursementAsync(id, cancellationToken);
        if (item is null)
            return Result<ReimbursementDto>.Failure("Reimbursement not found.");

        var previousStatus = (DisbursementStatus)item.Status;
        item.Status = (int)request.Status;
        item.AdministrativeNote = Clean(request.AdministrativeNote);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        if (previousStatus != request.Status)
            notificationOutbox.EnqueueStatusChanged(item, previousStatus);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<ReimbursementDto>.Success(ToDto(item));
    }

    public async Task<Result<ReimbursementDto>> CancelReimbursementAsync(Guid id, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetReimbursementAsync(id, cancellationToken);
        if (item is null || item.UserId != ownerUserId)
            return Result<ReimbursementDto>.Failure("Reimbursement not found.");

        var status = (DisbursementStatus)item.Status;
        if (status is DisbursementStatus.Paid or DisbursementStatus.Rejected or DisbursementStatus.Cancelled)
            return Result<ReimbursementDto>.Failure("This reimbursement can no longer be cancelled.");

        var previousStatus = status;
        item.Status = (int)DisbursementStatus.Cancelled;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        notificationOutbox.EnqueueStatusChanged(item, previousStatus);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<ReimbursementDto>.Success(ToDto(item));
    }

    public async Task<Result<ReceiptDownload>> GetReceiptAsync(Guid receiptId, Guid? ownerUserId, CancellationToken cancellationToken = default)
    {
        var receipt = await repository.GetReceiptAsync(receiptId, cancellationToken);
        if (receipt?.ExpenseItem?.Reimbursement is null || ownerUserId.HasValue && receipt.ExpenseItem.Reimbursement.UserId != ownerUserId.Value)
            return Result<ReceiptDownload>.Failure("Receipt not found.");

        var stream = await fileStorage.OpenReadAsync(receipt.StorageKey, cancellationToken);
        return stream is null
            ? Result<ReceiptDownload>.Failure("Receipt file not found.")
            : Result<ReceiptDownload>.Success(new ReceiptDownload(receipt.FileName, receipt.ContentType, stream));
    }

    public async Task<Result<IReadOnlyList<DisbursementEventDto>>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = await repository.GetEventsAsync(cancellationToken);
        return Result<IReadOnlyList<DisbursementEventDto>>.Success(events.Select(item => ToDto(item, true)).ToList());
    }

    public async Task<Result<DisbursementEventDto>> GetEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetEventAsync(id, cancellationToken);
        return item is null
            ? Result<DisbursementEventDto>.Failure("Disbursement event not found.")
            : Result<DisbursementEventDto>.Success(ToDto(item, true));
    }

    public async Task<Result<DisbursementEventDto>> CreateEventAsync(SaveDisbursementEventRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<DisbursementEventDto>.Failure("Event name is required.");

        var entity = new DisbursementEvent
        {
            Name = request.Name.Trim(), Description = Clean(request.Description), OccurredOn = request.OccurredOn, CreatedAt = DateTimeOffset.UtcNow
        };
        repository.Add(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<DisbursementEventDto>.Success(ToDto(entity, true));
    }

    public async Task<Result<AllocationDto>> CreateAllocationAsync(Guid eventId, SaveAllocationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Amount <= 0)
            return Result<AllocationDto>.Failure("Allocation name and a positive amount are required.");

        var disbursementEvent = await repository.GetEventAsync(eventId, cancellationToken);
        if (disbursementEvent is null)
            return Result<AllocationDto>.Failure("Disbursement event not found.");

        var entity = new Allocation { EventId = eventId, Event = disbursementEvent, Name = request.Name.Trim(), Description = Clean(request.Description), Amount = request.Amount };
        repository.Add(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<AllocationDto>.Success(ToDto(entity));
    }

    public async Task<Result<AllocationDto>> GetAllocationByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        var allocation = await repository.GetAllocationByTokenAsync(token, cancellationToken);
        return allocation is null
            ? Result<AllocationDto>.Failure("Allocation not found.")
            : Result<AllocationDto>.Success(ToDto(allocation));
    }

    public async Task<Result<IReadOnlyList<AllocationDto>>> GetAllocationsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var allocations = await repository.GetAllocationsForUserAsync(userId, cancellationToken);
        return Result<IReadOnlyList<AllocationDto>>.Success(allocations.Select(allocation => ToDto(allocation)).ToList());
    }

    public async Task<Result<AllocationApplicationDto>> ApplyAsync(Guid token, Guid userId, string applicantName, CreateAllocationApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var allocation = await repository.GetAllocationByTokenAsync(token, cancellationToken);
        if (allocation is null)
            return Result<AllocationApplicationDto>.Failure("Allocation not found.");
        if (request.Amount <= 0 || request.Amount > allocation.Amount)
            return Result<AllocationApplicationDto>.Failure("The requested amount must be positive and cannot exceed the allocation amount.");
        if (allocation.Applications.Any(item => item.ApplicantUserId == userId && (AllocationApplicationStatus)item.Status != AllocationApplicationStatus.Rejected))
            return Result<AllocationApplicationDto>.Failure("You already have an active application for this allocation.");
        var committed = allocation.Applications.Where(item => (AllocationApplicationStatus)item.Status != AllocationApplicationStatus.Rejected).Sum(item => item.Amount);
        if (committed + request.Amount > allocation.Amount)
            return Result<AllocationApplicationDto>.Failure("The requested amount exceeds the remaining allocation.");

        var paymentResult = await GetOwnedPaymentMethodAsync(userId, request.PaymentInformationId);
        if (!paymentResult.IsSuccess)
            return Result<AllocationApplicationDto>.Failure(paymentResult.Error!);

        var application = new AllocationApplication
        {
            AllocationId = allocation.Id, Allocation = allocation, ApplicantUserId = userId, ApplicantName = applicantName,
            Amount = request.Amount, Note = Clean(request.Note), Status = (int)AllocationApplicationStatus.Submitted,
            CreatedAt = DateTimeOffset.UtcNow, PaymentMethod = Snapshot(paymentResult.Value!)
        };
        var wasReserved = await repository.TryAddAllocationApplicationAsync(application, allocation.Amount, (int)AllocationApplicationStatus.Rejected, cancellationToken);
        if (!wasReserved)
            return Result<AllocationApplicationDto>.Failure("The available amount changed while the application was submitted. Refresh the allocation and try again.");
        return Result<AllocationApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<AllocationApplicationDto>> DecideAsync(Guid token, Guid applicationId, Guid userId, string approverName, DecideAllocationApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var allocation = await repository.GetAllocationByTokenAsync(token, cancellationToken);
        var application = allocation?.Applications.FirstOrDefault(item => item.Id == applicationId);
        if (application is null)
            return Result<AllocationApplicationDto>.Failure("Application not found.");
        if (application.ApplicantUserId == userId)
            return Result<AllocationApplicationDto>.Failure("Applicants cannot approve their own application.");

        var decision = application.Approvals.FirstOrDefault(item => item.ApproverUserId == userId);
        if (decision is null)
        {
            decision = new AllocationApproval { ApproverUserId = userId, ApproverName = approverName, CreatedAt = DateTimeOffset.UtcNow };
            application.Approvals.Add(decision);
            repository.Add(decision);
        }
        decision.IsApproved = request.IsApproved;
        decision.ApproverName = approverName;
        decision.CreatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return Result<AllocationApplicationDto>.Success(ToDto(application));
    }

    public async Task<Result<AllocationApplicationDto>> UpdateApplicationStatusAsync(Guid applicationId, UpdateAllocationApplicationStatusRequest request, CancellationToken cancellationToken = default)
    {
        var application = await repository.GetApplicationAsync(applicationId, cancellationToken);
        if (application is null)
            return Result<AllocationApplicationDto>.Failure("Application not found.");
        if (application.Allocation is null)
            return Result<AllocationApplicationDto>.Failure("Allocation not found.");
        var wasUpdated = await repository.TryUpdateAllocationApplicationStatusAsync(application, (int)request.Status, application.Allocation.Amount, (int)AllocationApplicationStatus.Rejected, cancellationToken);
        if (!wasUpdated)
            return Result<AllocationApplicationDto>.Failure("The status change would exceed the allocation amount. Refresh and try again.");
        return Result<AllocationApplicationDto>.Success(ToDto(application));
    }

    private async Task<Result<PaymentInformationDto>> GetOwnedPaymentMethodAsync(Guid userId, Guid paymentInformationId)
    {
        var result = await paymentInformationService.GetForUserAsync(userId);
        if (!result.IsSuccess)
            return Result<PaymentInformationDto>.Failure("Add a payment method to your profile before requesting a payout.");
        var method = result.Value!.FirstOrDefault(item => item.Id == paymentInformationId);
        return method is null
            ? Result<PaymentInformationDto>.Failure("The selected payment method does not belong to your profile.")
            : Result<PaymentInformationDto>.Success(method);
    }

    private static Result ValidateReimbursement(CreateReimbursementRequest request, IReadOnlyList<ReceiptUpload> files)
    {
        if (string.IsNullOrWhiteSpace(request.Purpose)) return Result.Failure("Purpose is required.");
        if (request.PaymentInformationId == Guid.Empty) return Result.Failure("A payment method is required.");
        if (request.Expenses.Count == 0) return Result.Failure("At least one expense is required.");
        if (request.Expenses.Any(item => string.IsNullOrWhiteSpace(item.Description) || item.Amount <= 0)) return Result.Failure("Each expense needs a description and positive amount.");
        if (request.Expenses.SelectMany(item => item.ReceiptIndexes).Any(index => index < 0 || index >= files.Count)) return Result.Failure("A receipt reference is invalid.");
        if (files.Any(file => file.Size <= 0 || file.Size > MaximumReceiptSize)) return Result.Failure($"Each receipt must be between 1 byte and {MaximumReceiptSize / 1024 / 1024} MB.");
        if (files.Any(file => !AllowedContentTypes.Contains(file.ContentType))) return Result.Failure("Receipts must be PDF, JPEG, PNG, WebP, or GIF files.");
        if (files.Any(file => !AllowedExtensions.TryGetValue(file.ContentType, out var extensions) || !extensions.Contains(Path.GetExtension(file.FileName)))) return Result.Failure("A receipt file extension does not match its declared format.");
        if (request.Expenses.Any(item => item.ReceiptIndexes.Count == 0)) return Result.Failure("Each expense needs at least one receipt.");
        return Result.Success();
    }

    private static PaymentMethodSnapshot Snapshot(PaymentInformationDto source)
    {
        var displayName = source.Type == PaymentInformationType.PayPal
            ? $"PayPal · {source.PayPalEmail}"
            : $"Bank account · {source.AccountHolder} · ••••{LastFour(source.Iban)}";
        return new PaymentMethodSnapshot
        {
            PaymentInformationId = source.Id,
            Type = (int)source.Type,
            DisplayName = displayName,
            PayPalEmail = source.PayPalEmail,
            AccountHolder = source.AccountHolder,
            Iban = source.Iban,
            Bic = source.Bic
        };
    }

    private static string LastFour(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Length <= 4 ? value : value[^4..];
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ReimbursementDto ToDto(Reimbursement item) => new()
    {
        Id = item.Id, UserId = item.UserId, ApplicantName = item.ApplicantName, Purpose = item.Purpose, Note = item.Note,
        AdministrativeNote = item.AdministrativeNote, Status = (DisbursementStatus)item.Status, CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt,
        PaymentMethod = ToDto(item.PaymentMethod), Expenses = item.Expenses.Select(ToDto).ToList()
    };
    private static ExpenseItemDto ToDto(ExpenseItem item) => new() { Id = item.Id, Description = item.Description, Amount = item.Amount, IncurredOn = item.IncurredOn, Receipts = item.Receipts.Select(ToDto).ToList() };
    private static ReceiptDto ToDto(Receipt item) => new() { Id = item.Id, FileName = item.FileName, ContentType = item.ContentType, Size = item.Size };
    private static DisbursementEventDto ToDto(DisbursementEvent item, bool includePaymentMethods) => new() { Id = item.Id, Name = item.Name, Description = item.Description, OccurredOn = item.OccurredOn, CreatedAt = item.CreatedAt, Allocations = item.Allocations.Select(allocation => ToDto(allocation, includePaymentMethods)).ToList() };
    private static AllocationDto ToDto(Allocation item, bool includePaymentMethods = false) => new() { Id = item.Id, EventId = item.EventId, EventName = item.Event?.Name ?? string.Empty, Name = item.Name, Description = item.Description, Amount = item.Amount, ShareToken = item.ShareToken, Applications = item.Applications.Select(application => ToDto(application, includePaymentMethods)).ToList() };
    private static AllocationApplicationDto ToDto(AllocationApplication item, bool includePaymentMethod = true) => new() { Id = item.Id, AllocationId = item.AllocationId, ApplicantUserId = item.ApplicantUserId, ApplicantName = item.ApplicantName, Amount = item.Amount, Note = item.Note, Status = (AllocationApplicationStatus)item.Status, CreatedAt = item.CreatedAt, PaymentMethod = includePaymentMethod ? ToDto(item.PaymentMethod) : new PaymentMethodSnapshotDto(), Approvals = item.Approvals.Select(ToDto).ToList() };
    private static AllocationApprovalDto ToDto(AllocationApproval item) => new() { Id = item.Id, ApproverUserId = item.ApproverUserId, ApproverName = item.ApproverName, IsApproved = item.IsApproved, CreatedAt = item.CreatedAt };
    private static PaymentMethodSnapshotDto ToDto(PaymentMethodSnapshot item) => new() { PaymentInformationId = item.PaymentInformationId, Type = (PaymentInformationType)item.Type, DisplayName = item.DisplayName, PayPalEmail = GetPayPalEmail(item), AccountHolder = item.AccountHolder, Iban = item.Iban, Bic = item.Bic };

    private static string? GetPayPalEmail(PaymentMethodSnapshot item)
    {
        if (!string.IsNullOrWhiteSpace(item.PayPalEmail))
            return item.PayPalEmail;
        if ((PaymentInformationType)item.Type != PaymentInformationType.PayPal)
            return null;

        var separatorIndex = item.DisplayName.IndexOf('·');
        return separatorIndex < 0 ? null : Clean(item.DisplayName[(separatorIndex + 1)..]);
    }
}
