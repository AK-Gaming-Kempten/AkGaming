using AkGaming.Management.Modules.Disbursements.Contracts.Enums;

namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class UpdateReimbursementStatusRequest
{
    public DisbursementStatus Status { get; set; }
    public string? AdministrativeNote { get; set; }
}
