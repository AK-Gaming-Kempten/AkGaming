using AkGaming.InvoiceGenerator.Core.Models;
using AkGaming.InvoiceGenerator.Core.Rendering;

namespace AkGaming.Management.Modules.MemberManagement.Tests;

[TestFixture]
public class NoticePdfRendererTests {
    [Test]
    [Description("Renders a notice document as a valid PDF payload.")]
    public void Render_ReturnsPdfDocument() {
        // Arrange
        var renderer = new NoticePdfRenderer();
        var notice = new NoticeDocument {
            DocumentType = "Mitgliedsbeitrag",
            Title = "Zahlungserinnerung",
            RecipientName = "Max Mustermann",
            RecipientEmail = "max@example.com",
            RecipientAddressLines = ["Musterstraße 1", "87435 Kempten"],
            Greeting = "Hi Max!",
            HeroText = "Dein Mitgliedsbeitrag ist noch offen.",
            SummaryRows = [new NoticeSummaryRow("Aktuell offen", "15 €")],
            IntroParagraphs = ["Bitte prüfe die folgenden Optionen."],
            Sections = [new NoticeSection {
                Title = "Mitgliedsbeitrag bezahlen",
                Paragraphs = ["Überweise den offenen Betrag an das Vereinskonto."]
            }],
            HighlightTitle = "Wichtiger Hinweis",
            HighlightText = "Bitte melde dich beim Vorstand.",
            Closing = "Liebe Grüße\nVorstand AK Gaming e.V."
        };

        // Act
        var pdf = renderer.Render(notice);

        // Assert
        using (Assert.EnterMultipleScope()) {
            Assert.That(pdf, Is.Not.Empty);
            Assert.That(System.Text.Encoding.ASCII.GetString(pdf, 0, 4), Is.EqualTo("%PDF"));
        }
    }
}
