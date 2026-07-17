using System.Text.Json;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class PaymentInformationPanel : ComponentBase {
    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient Api { get; set; } = default!;

    private readonly List<PaymentInformationDto> _items = [];
    private PaymentInformationDto _newItem = new();
    private bool _loading = true;
    private bool _showCreate;
    private PaymentInformationDto? _editingItem;
    private string? _error;

    protected override async Task OnInitializedAsync() {
        var result = await Api.GetPaymentInformationAsync();
        if (result.IsSuccess)
            _items.AddRange(result.Value ?? []);
        else
            _error = result.Error;
        _loading = false;
    }

    private void ShowCreate() {
        _newItem = new PaymentInformationDto();
        _showCreate = true;
        _error = null;
    }

    private void CancelCreate() => _showCreate = false;

    private void SelectPaymentType(PaymentInformationType type) {
        _newItem.Type = type;
    }

    private async Task CreateAsync() {
        _error = null;
        var result = await Api.CreatePaymentInformationAsync(_newItem);
        if (!result.IsSuccess) {
            _error = result.Error;
            return;
        }
        _items.Add(result.Value!);
        _showCreate = false;
    }

    private bool IsEditing(PaymentInformationDto item) => _editingItem?.Id == item.Id;

    private void StartEditing(PaymentInformationDto item) {
        _editingItem = Clone(item);
        _error = null;
    }

    private void CancelEditing() {
        _editingItem = null;
        _error = null;
    }

    private async Task SaveEditingAsync() {
        if (_editingItem is null) {
            return;
        }

        _error = null;
        var result = await Api.UpdatePaymentInformationAsync(_editingItem);
        if (!result.IsSuccess) {
            _error = result.Error;
            return;
        }

        var index = _items.FindIndex(item => item.Id == _editingItem.Id);
        if (index >= 0) {
            _items[index] = Clone(_editingItem);
        }
        _editingItem = null;
    }

    private async Task DeleteAsync(Guid id) {
        _error = null;
        var result = await Api.DeletePaymentInformationAsync(id);
        if (!result.IsSuccess) {
            _error = result.Error;
            return;
        }
        _items.RemoveAll(x => x.Id == id);
        if (_editingItem?.Id == id) {
            _editingItem = null;
        }
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static PaymentInformationDto Clone(PaymentInformationDto item) =>
        JsonSerializer.Deserialize<PaymentInformationDto>(JsonSerializer.Serialize(item)) ?? new PaymentInformationDto();
}
