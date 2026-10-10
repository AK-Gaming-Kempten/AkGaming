using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
namespace AkGaming.Gamenight.Shared.Components;
public partial class SignupEditor
{
    [Parameter, EditorRequired] public SignupForm Form { get; set; } = default!;
    [Parameter] public bool Editing { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback Submit { get; set; }
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private ElementReference _formTop;
    private bool _scrollToForm;
    private int _step;
    private int StepCount => Form.Attendance == "Karaoke" ? 2 : 3;
    private int StepNumber => Form.Attendance == "Karaoke" && _step == 2 ? 2 : _step + 1;
    private List<string> _stepErrors = [];

    private void Next()
    {
        _stepErrors = _step == 0 ? ValidateContact() : ValidateEvening();
        if (_stepErrors.Count == 0)
        {
            _step = _step == 0 && Form.Attendance == "Karaoke" ? 2 : _step + 1;
            _scrollToForm = true;
        }
    }

    private List<string> ValidateContact()
    {
        var errors = new List<string>();
        if (!new EmailAddressAttribute().IsValid(Form.Email) || string.IsNullOrWhiteSpace(Form.Email))
            errors.Add("Bitte gib eine gültige E-Mail-Adresse an.");
        if (string.IsNullOrWhiteSpace(Form.FirstName) || Form.FirstName.Length > 100)
            errors.Add("Bitte gib deinen Vornamen an (maximal 100 Zeichen).");
        if (string.IsNullOrWhiteSpace(Form.LastName) || Form.LastName.Length > 100)
            errors.Add("Bitte gib deinen Nachnamen an (maximal 100 Zeichen).");
        if (Form.Attendance is not ("GameNight" or "Karaoke"))
            errors.Add("Bitte wähle aus, wofür du dich anmeldest.");
        return errors;
    }

    private List<string> ValidateEvening()
    {
        var errors = new List<string>();
        if (Form.Attendance == "GameNight")
        {
            if (Form.Sockets is not (0 or 1 or 2 or 4)) errors.Add("Bitte wähle deinen Steckdosenbedarf aus.");
            if (Form.WantsToOrder is null) errors.Add("Bitte gib an, ob du etwas bestellen möchtest.");
            if (Form.WantsToOrder == true && (Form.DonerQuantity ?? 0) + (Form.PizzaQuantity ?? 0) + (Form.IceCreamQuantity ?? 0) == 0)
                errors.Add("Bitte wähle mindestens eine Portion aus.");
            if (Form.PenAndPaper is null) errors.Add("Bitte beantworte die Frage zu Pen & Paper.");
        }
        return errors;
    }

    private void Back()
    {
        _stepErrors.Clear();
        _step = _step == 2 && Form.Attendance == "Karaoke" ? 0 : _step - 1;
        _scrollToForm = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_scrollToForm)
        {
            _scrollToForm = false;
            await JS.InvokeVoidAsync("gamenight.scrollToSignup", _formTop);
        }
    }

    private void ShowInvalidSubmit(EditContext context)
    {
        _stepErrors = context.GetValidationMessages().Distinct().ToList();
    }

    private void DismissErrors()
    {
        _stepErrors.Clear();
    }
    private void ClearStepErrors(ChangeEventArgs _) => _stepErrors.Clear();
    private void ToggleSource(string source, ChangeEventArgs e)
    {
        if (e.Value is true && !Form.DiscoverySources.Contains(source)) Form.DiscoverySources.Add(source);
        else Form.DiscoverySources.Remove(source);
    }
}
