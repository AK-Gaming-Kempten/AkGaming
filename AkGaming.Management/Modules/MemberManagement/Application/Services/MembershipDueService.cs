using System.Globalization;
using AkGaming.Core.Common.Generics;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.InvoiceGenerator.Core.Models;
using AkGaming.InvoiceGenerator.Core.Rendering;
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
    IEmailSender? emailSender = null,
    INoticePdfRenderer? noticePdfRenderer = null)
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
    public async Task<Result<MembershipDueEmailPreviewDto>> GetSuspensionEmailPreviewAsync(int dueId) {
        var reminderContextResult = await LoadReminderContextAsync(dueId);
        if (!reminderContextResult.IsSuccess)
            return Result<MembershipDueEmailPreviewDto>.Failure(reminderContextResult.Error ?? "Suspension context could not be loaded.");
        var reminderContext = reminderContextResult.Value!;

        var eligibility = EvaluateSuspensionEligibility(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        if (!eligibility.IsSendable)
            return Result<MembershipDueEmailPreviewDto>.Failure(eligibility.Reason ?? "Suspension email is not available.");

        var preview = MembershipDueSuspensionEmailComposer.Compose(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        return Result<MembershipDueEmailPreviewDto>.Success(preview);
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> RenderReminderPdfAsync(int dueId) {
        var contextResult = await LoadReminderContextAsync(dueId);
        if (!contextResult.IsSuccess)
            return Result<byte[]>.Failure(contextResult.Error ?? "Reminder context could not be loaded.");
        var context = contextResult.Value!;

        var eligibility = EvaluateReminderEligibility(context.Member, context.PaymentPeriod, context.Due);
        if (!eligibility.IsSendable)
            return Result<byte[]>.Failure(eligibility.Reason ?? "Reminder PDF is not available.");

        return RenderNoticePdf(BuildReminderNotice(context));
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> RenderSuspensionPdfAsync(int dueId) {
        var contextResult = await LoadReminderContextAsync(dueId);
        if (!contextResult.IsSuccess)
            return Result<byte[]>.Failure(contextResult.Error ?? "Suspension context could not be loaded.");
        var context = contextResult.Value!;

        var eligibility = EvaluateSuspensionEligibility(context.Member, context.PaymentPeriod, context.Due);
        if (!eligibility.IsSendable)
            return Result<byte[]>.Failure(eligibility.Reason ?? "Suspension PDF is not available.");

        return RenderNoticePdf(BuildSuspensionNotice(context));
    }

    private Result<byte[]> RenderNoticePdf(NoticeDocument notice) {
        if (noticePdfRenderer is null)
            return Result<byte[]>.Failure("PDF renderer is not configured.");

        var pdf = noticePdfRenderer.Render(notice);
        return Result<byte[]>.Success(pdf);
    }

    private static NoticeDocument BuildReminderNotice(ReminderContext context) {
        var (totalAmount, paidAmount, remainingAmount) = GetDueAmounts(context.Due);
        var isRegularDue = context.Due.DueAmount == context.PaymentPeriod.DefaultDueAmount;
        var explanation = isRegularDue
            ? $"Seit dem Beschluss unserer neuen Beitragsordnung zum 21.02.2026 sind von jedem Mitglied {totalAmount} je Semester als Mitgliedsbeitrag zu entrichten."
            : $"Für diesen Zahlungszeitraum ist für dich aktuell ein Mitgliedsbeitrag von {totalAmount} hinterlegt.";
        var paymentStatus = context.Due.PaidAmount > 0m
            ? $"Aktuell sind bereits {paidAmount} verbucht; offen sind noch {remainingAmount}."
            : $"Aktuell ist der volle Betrag von {remainingAmount} offen.";

        return CreateNoticeBase(context, "Mitgliedsbeitrag", "Mitgliedsbeitrag offen", "#286c3f", "#0f221e", "#61756d", "#f7fbf8", "#d6e8da", "#28433a", "#49645b") with {
            HeroText = $"Leider konnten wir für den aktuellen Zahlungszeitraum ({context.PaymentPeriod.Name} - Zahlung bis spätestens {FormatDate(context.Due.DueDate)}) noch keinen vollständigen Eingang deines Mitgliedsbeitrags verbuchen.",
            SummaryRows = BuildSummaryRows(context, totalAmount, paidAmount, remainingAmount),
            IntroParagraphs = [
                explanation,
                "Um unseren Vereinszweck zu unterstützen und aus Fairness allen anderen Mitgliedern gegenüber möchten wir auch dich bitten, dieser Pflicht nachzukommen.",
                paymentStatus
            ],
            Sections = [
                new NoticeSection {
                    Title = "Mitgliedsbeitrag bezahlen",
                    Paragraphs = [
                        $"Überweise schnellstmöglich den offenen Betrag von {remainingAmount} an das unten genannte Konto.",
                        $"{ClubConstants.Organization.LegalName}\nIBAN: {ClubConstants.BankAccount.Iban}\nBIC: {ClubConstants.BankAccount.Bic}\nVerwendungszweck: (Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)"
                    ]
                },
                new NoticeSection {
                    Title = "Fördermitglied werden",
                    Paragraphs = [$"Stelle einen formlosen Antrag per Mail an {ClubConstants.EmailAddresses.Board}, um Fördermitglied zu werden, und überweise den verringerten Beitrag für Fördermitglieder (frei wählbar zwischen 5 € und 15 €) an das oben genannte Konto."]
                },
                new NoticeSection {
                    Title = "Beitragsermäßigung bzw. -befreiung beantragen",
                    Paragraphs = [$"Wenn du dich aktuell in einer finanziell schwierigen Lage befindest, kannst du unter {ClubConstants.EmailAddresses.Board} eine Beitragsermäßigung oder -befreiung beantragen."]
                },
                new NoticeSection {
                    Title = $"Aus dem {ClubConstants.Organization.LegalName} austreten",
                    Paragraphs = [$"Eine formlose Austrittserklärung per Mail an {ClubConstants.EmailAddresses.Board} ist für alle Beteiligten einfacher als ein Suspendierungsverfahren."]
                }
            ],
            HighlightTitle = "Wichtiger Hinweis",
            HighlightText = "Sollten wir in den nächsten Tagen weder die Zahlung deines Beitrags verbuchen noch eine Kontaktaufnahme von dir erhalten, müssen wir nach §6.9 unserer Satzung deine Suspendierung beschließen, gefolgt von einer Abstimmung über deinen Ausschluss aus dem Verein in der nächsten Mitgliederversammlung."
        };
    }

    private static NoticeDocument BuildSuspensionNotice(ReminderContext context) {
        var (totalAmount, paidAmount, remainingAmount) = GetDueAmounts(context.Due);
        return CreateNoticeBase(context, "Suspendierung", "Mitgliedschaft suspendiert", "#9a3412", "#2c1613", "#7c5d54", "#fbf8f5", "#eadfd6", "#54322c", "#77483e") with {
            HeroText = "Der Vorstand hat beschlossen, deine Mitgliedschaft vorübergehend zu suspendieren.",
            SummaryRows = BuildSummaryRows(context, totalAmount, paidAmount, remainingAmount),
            IntroParagraphs = [
                "Grund ist, dass dein Mitgliedsbeitrag für den genannten Zahlungszeitraum trotz Fälligkeit weiterhin nicht vollständig eingegangen ist.",
                "Nach §4.4 unserer Satzung sind vollständige Mitglieder verpflichtet, den in der Beitragsordnung festgelegten Beitrag zu zahlen. Nach §6.9 kann der Vorstand eine vorübergehende Suspendierung beschließen.",
                "Während der Suspendierung bist du nach §6.9 b) von deinen Rechten und Pflichten nach §4 entbunden. Deine Rechte im Zusammenhang mit Mitgliederversammlungen bleiben nach §6.9 e) bestehen."
            ],
            Sections = [
                new NoticeSection {
                    Title = "Suspendierung beenden",
                    Paragraphs = [$"Überweise den offenen Betrag von {remainingAmount} an das Vereinskonto oder melde dich mit Zahlungsdatum und Verwendungszweck bei {ClubConstants.EmailAddresses.Board}, falls du bereits gezahlt hast. Wenn du den Beitrag aktuell nicht zahlen kannst, kontaktiere den Vorstand zur Prüfung einer Beitragsermäßigung oder -befreiung."]
                },
                new NoticeSection {
                    Title = "Vereinskonto",
                    Paragraphs = [$"{ClubConstants.Organization.LegalName}\nIBAN: {ClubConstants.BankAccount.Iban}\nBIC: {ClubConstants.BankAccount.Bic}\nVerwendungszweck: (Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)"]
                }
            ],
            HighlightTitle = "Nächste Schritte",
            HighlightText = "Die nächste Mitgliederversammlung stimmt nach §6.9 a) in Verbindung mit §6.5 über einen möglichen Ausschluss ab. Lehnt die Mitgliederversammlung den Ausschluss ab, ist die Suspendierung aufgehoben. Der Vorstand kann die Suspendierung nach §6.9 f) außerdem selbst aufheben, sobald der Grund entfallen ist."
        };
    }

    private static NoticeDocument CreateNoticeBase(
        ReminderContext context,
        string documentType,
        string title,
        string accentColor,
        string darkColor,
        string mutedColor,
        string lightColor,
        string borderColor,
        string summaryBackgroundColor,
        string summaryBorderColor)
    {
        var firstName = context.Member.FirstName?.Trim();
        return new NoticeDocument {
            DocumentType = documentType,
            Title = title,
            RecipientName = BuildMemberDisplayName(context.Member),
            RecipientEmail = context.Member.Email?.Trim() ?? string.Empty,
            RecipientAddressLines = BuildRecipientAddressLines(context.Member),
            Greeting = string.IsNullOrWhiteSpace(firstName) ? "Hallo!" : $"Hi {firstName}!",
            Closing = $"Liebe Grüße\nVorstand {ClubConstants.Organization.LegalName}",
            Links = [
                new NoticeLink("Mitgliedsbeitrag", ClubConstants.Urls.MembershipFees),
                new NoticeLink("Vereinssatzung", ClubConstants.Urls.ArticlesOfAssociation),
                new NoticeLink("Beitragsordnung", ClubConstants.Urls.MembershipFeeRegulations)
            ],
            AccentColor = accentColor,
            DarkColor = darkColor,
            MutedColor = mutedColor,
            LightColor = lightColor,
            BorderColor = borderColor,
            SummaryBackgroundColor = summaryBackgroundColor,
            SummaryBorderColor = summaryBorderColor
        };
    }

    private static IReadOnlyList<string> BuildRecipientAddressLines(Member member) {
        if (member.Address is null)
            return [];

        var cityLine = string.Join(" ", new[] { member.Address.ZipCode?.Trim(), member.Address.City?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return new[] { member.Address.Street?.Trim(), cityLine, member.Address.Country?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();
    }

    private static IReadOnlyList<NoticeSummaryRow> BuildSummaryRows(ReminderContext context, string totalAmount, string paidAmount, string remainingAmount) {
        var rows = new List<NoticeSummaryRow> {
            new("Zahlungszeitraum", context.PaymentPeriod.Name),
            new("Fällig bis", FormatDate(context.Due.DueDate)),
            new("Gesamtbeitrag", totalAmount)
        };
        if (context.Due.PaidAmount > 0m)
            rows.Add(new NoticeSummaryRow("Bereits verbucht", paidAmount));
        rows.Add(new NoticeSummaryRow("Aktuell offen", remainingAmount));
        return rows;
    }

    private static (string Total, string Paid, string Remaining) GetDueAmounts(MembershipDue due) {
        var paidAmount = due.PaidAmount ?? 0m;
        return (FormatCurrency(due.DueAmount), FormatCurrency(paidAmount), FormatCurrency(Math.Max(due.DueAmount - paidAmount, 0m)));
    }

    private static string FormatDate(DateOnly value) => value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));

    private static string FormatCurrency(decimal value) {
        var format = decimal.Truncate(value) == value ? "0" : "0.00";
        return $"{value.ToString(format, CultureInfo.GetCultureInfo("de-DE"))} €";
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
                    Reason = GetMissingDueReason(member, paymentPeriod)
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

            reminderContext.Due.LastReminderSentAt = DateTimeOffset.UtcNow;
            reminderContext.Due.LastReminderSendStatus = DomainEnums.MembershipDueReminderSendStatus.Sent;

            var saveReminderResult = await dueRepository.SaveChangesAsync();
            if (!saveReminderResult.IsSuccess)
                return Result.Failure($"Reminder email was sent, but the due could not be updated. {saveReminderResult.Error ?? "Changes could not be saved."}");

            return Result.Success();
        }
        catch (Exception exception) {
            reminderContext.Due.LastReminderSendStatus = DomainEnums.MembershipDueReminderSendStatus.Failed;

            var saveReminderFailureResult = await dueRepository.SaveChangesAsync();
            if (!saveReminderFailureResult.IsSuccess) {
                return Result.Failure(
                    $"Failed to send reminder email: {exception.Message}. Additionally, the failure status could not be saved. {saveReminderFailureResult.Error ?? "Changes could not be saved."}");
            }

            return Result.Failure($"Failed to send reminder email: {exception.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SendSuspensionEmailAsync(int dueId) {
        if (emailSender is null)
            return Result.Failure("Email sender is not configured.");

        var reminderContextResult = await LoadReminderContextAsync(dueId);
        if (!reminderContextResult.IsSuccess)
            return Result.Failure(reminderContextResult.Error ?? "Suspension context could not be loaded.");
        var reminderContext = reminderContextResult.Value!;

        var eligibility = EvaluateSuspensionEligibility(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);
        if (!eligibility.IsSendable)
            return Result.Failure(eligibility.Reason ?? "Suspension email cannot be sent.");

        var preview = MembershipDueSuspensionEmailComposer.Compose(reminderContext.Member, reminderContext.PaymentPeriod, reminderContext.Due);

        try {
            await emailSender.SendAsync(
                preview.RecipientEmail,
                preview.Subject,
                preview.TextBody,
                preview.HtmlBody,
                CancellationToken.None);
        }
        catch (Exception exception) {
            return Result.Failure($"Failed to send suspension email: {exception.Message}");
        }

        var statusChangeResult = reminderContext.Member.ChangeStatus(DomainEnums.MembershipStatus.Suspended);
        if (!statusChangeResult.IsSuccess)
            return Result.Failure(statusChangeResult.Error ?? "Suspension email was sent, but the member status could not be changed.");

        var updateMemberResult = memberRepository.Update(reminderContext.Member);
        if (!updateMemberResult.IsSuccess)
            return Result.Failure($"Suspension email was sent, but the member could not be updated. {updateMemberResult.Error ?? "Member update failed."}");

        var saveMemberResult = await memberRepository.SaveChangesAsync();
        if (!saveMemberResult.IsSuccess)
            return Result.Failure($"Suspension email was sent, but the member status could not be saved. {saveMemberResult.Error ?? "Changes could not be saved."}");

        return Result.Success();
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
            .OrderByDescending(sc => sc.Timestamp)
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

    private static ReminderEligibility EvaluateSuspensionEligibility(Member member, MembershipPaymentPeriod paymentPeriod, MembershipDue due) {
        var dueEligibility = EvaluateReminderEligibility(member, paymentPeriod, due);
        if (!dueEligibility.IsSendable)
            return dueEligibility;

        return member.Status switch {
            DomainEnums.MembershipStatus.Suspended => ReminderEligibility.Skip("Member is already suspended."),
            DomainEnums.MembershipStatus.Expelled => ReminderEligibility.Skip("Member has already been expelled."),
            DomainEnums.MembershipStatus.Withdrawn => ReminderEligibility.Skip("Member has already withdrawn."),
            DomainEnums.MembershipStatus.Member or DomainEnums.MembershipStatus.HonoraryMember or DomainEnums.MembershipStatus.SupportingMember => ReminderEligibility.Sendable(),
            _ => ReminderEligibility.Skip($"Member cannot be suspended from status {member.Status}.")
        };
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

    private static string GetMissingDueReason(Member member, MembershipPaymentPeriod paymentPeriod) {
        var trialWindowReason = GetTrialWindowSkipReason(member, paymentPeriod);
        if (trialWindowReason is not null)
            return trialWindowReason;

        return "No due exists in this payment period.";
    }

    private static string? GetTrialWindowSkipReason(Member member, MembershipPaymentPeriod paymentPeriod) {
        var latestTrialStart = member.StatusChanges
            .Where(statusChange => statusChange.NewStatus == DomainEnums.MembershipStatus.InTrial)
            .OrderByDescending(statusChange => statusChange.Timestamp)
            .FirstOrDefault();

        if (latestTrialStart is null)
            return null;

        var trialStartDate = DateOnly.FromDateTime(latestTrialStart.Timestamp);
        var trialEndDate = DateOnly.FromDateTime(latestTrialStart.Timestamp.AddDays(MemberManagementConstants.DefaultTrialPeriodInDays));
        var paymentPeriodTrialCutoff = paymentPeriod.DueDate.AddMonths(3);

        if (trialStartDate > paymentPeriodTrialCutoff)
            return null;

        if (trialEndDate <= paymentPeriodTrialCutoff)
            return null;

        return "Member was in trial for this payment period and therefore had no due.";
    }

    private sealed record ReminderContext(MembershipDue Due, Member Member, MembershipPaymentPeriod PaymentPeriod);

    private sealed record ReminderEligibility(bool IsSendable, string? Reason) {
        public static ReminderEligibility Sendable() => new(true, null);
        public static ReminderEligibility Skip(string reason) => new(false, reason);
    }
}
