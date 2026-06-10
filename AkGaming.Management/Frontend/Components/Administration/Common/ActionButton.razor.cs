using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AkGaming.Management.Frontend.Components.Administration.Common;

public partial class ActionButton : ComponentBase
{
    [Parameter, EditorRequired] public ActionButtonKind Kind { get; set; }
    [Parameter] public ActionButtonVariant Variant { get; set; } = ActionButtonVariant.Secondary;
    [Parameter] public ActionButtonSize Size { get; set; } = ActionButtonSize.Default;
    [Parameter] public string ButtonType { get; set; } = "button";
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    private string EffectiveLabel => Label ?? Kind switch
    {
        ActionButtonKind.Add => "Add",
        ActionButtonKind.Approve => "Approve",
        ActionButtonKind.Back => "Back",
        ActionButtonKind.Cancel => "Cancel",
        ActionButtonKind.Close => "Close",
        ActionButtonKind.Create => "Create",
        ActionButtonKind.Delete => "Delete",
        ActionButtonKind.DownloadHtml => "Download HTML",
        ActionButtonKind.DownloadPdf => "Download PDF",
        ActionButtonKind.Edit => "Edit",
        ActionButtonKind.Filter => "Filter",
        ActionButtonKind.Link => "Link",
        ActionButtonKind.More => "More actions",
        ActionButtonKind.Next => "Next",
        ActionButtonKind.Previous => "Previous",
        ActionButtonKind.Preview => "Preview",
        ActionButtonKind.Refresh => "Refresh",
        ActionButtonKind.Reject => "Reject",
        ActionButtonKind.Remove => "Remove",
        ActionButtonKind.Save => "Save",
        ActionButtonKind.Search => "Search",
        ActionButtonKind.Send => "Send",
        ActionButtonKind.Update => "Update",
        _ => Kind.ToString()
    };

    private string IconClass => Kind switch
    {
        ActionButtonKind.Add or ActionButtonKind.Create => "bi-plus-lg",
        ActionButtonKind.Approve => "bi-check-lg",
        ActionButtonKind.Back => "bi-arrow-left",
        ActionButtonKind.Cancel or ActionButtonKind.Close => "bi-x-lg",
        ActionButtonKind.Delete => "bi-trash",
        ActionButtonKind.DownloadHtml => "bi-filetype-html",
        ActionButtonKind.DownloadPdf => "bi-file-earmark-pdf",
        ActionButtonKind.Edit => "bi-pencil",
        ActionButtonKind.Filter => "bi-funnel",
        ActionButtonKind.Link => "bi-link-45deg",
        ActionButtonKind.More => "bi-three-dots",
        ActionButtonKind.Next => "bi-chevron-right",
        ActionButtonKind.Previous => "bi-chevron-left",
        ActionButtonKind.Preview => "bi-eye",
        ActionButtonKind.Refresh => "bi-arrow-clockwise",
        ActionButtonKind.Reject => "bi-x-circle",
        ActionButtonKind.Remove => "bi-dash-lg",
        ActionButtonKind.Save => "bi-floppy",
        ActionButtonKind.Search => "bi-search",
        ActionButtonKind.Send => "bi-send",
        ActionButtonKind.Update => "bi-arrow-repeat",
        _ => "bi-circle"
    };

    private string VariantClass => Variant switch
    {
        ActionButtonVariant.Primary => "btn-primary",
        ActionButtonVariant.Danger => "btn-danger",
        ActionButtonVariant.Outline => "btn-outline-secondary",
        _ => "btn-secondary"
    };

    private string SizeClass => Size == ActionButtonSize.Small ? "btn-sm action-button-sm" : string.Empty;
}

public enum ActionButtonKind
{
    Add,
    Approve,
    Back,
    Cancel,
    Close,
    Create,
    Delete,
    DownloadHtml,
    DownloadPdf,
    Edit,
    Filter,
    Link,
    More,
    Next,
    Previous,
    Preview,
    Refresh,
    Reject,
    Remove,
    Save,
    Search,
    Send,
    Update
}

public enum ActionButtonVariant
{
    Secondary,
    Primary,
    Danger,
    Outline
}

public enum ActionButtonSize
{
    Default,
    Small
}
