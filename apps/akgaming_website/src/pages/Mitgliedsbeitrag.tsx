import "./Mitgliedsbeitrag.css";

const faqs = [
    {
        question: "Wie hoch ist der Mitgliedsbeitrag?",
        answer: "Der Mitgliedsbeitrag beträgt regulär 15€ pro Semester bzw. 30€ pro Jahr. "
    },
    {
        question: "Wann ist der Beitrag fällig?",
        answer: "Die Fälligkeit des Beitrags richtet sich an den Semestern der Hochschule Kempten. Das entspricht im Regelfall dem 01.Oktober für das Wintersemester und dem 01. April für das Sommersemester. Der erste Beitrag ab der Einführung ist zum Sommersemester 2026 zu entrichten."
    },
    {
        question: "Wie kann ich meinen Beitrag bezahlen?",
        answer: "Die Zahlung erfolgt per Überweisung an:" +
            "<p><br>AK Gaming e.V. <br>DE59 7336 9920 0000 8872 85 <br>GENODEF1SFO </p> " +
            "Bitte achte darauf, den passenden Verwendungszweck anzugeben, damit wir deine Zahlung korrekt zuordnen können." +
            "<br><br><strong>Hinweis: </strong>Vermutlich wirst du beim Überweisen eine Warnung erhalten, dass der Zahlungsempfänger nicht mit den bei der Bank hinterlegten Daten übereinstimmt. Dies liegt daran, dass unser Vereinskonto aufgrund Regularien der Bank nicht auf den Namen des Vereins, sondern auf den einer natürlichen Person sein muss, was in diesem Fall der unseres 1. Vorstandes ist. Überprüfe daher die angegebene IBAN sorgfältig und gib trotzdem den Ak Gaming e.V. als Zahlungsempfänger an."
    },
    {
        question: "Welchen Verwendungszweck soll ich bei der Überweisung angeben?",
        answer: "Bitte nutze eines der folgenden Formate:<br><br><code>(Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)</code><br>oder<br><code>(MitgliederID), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)</code><br><br>Beispiele:<br><code>Mustermann, Max, Mitgliedsbeitrag SS 2026</code><br><code>Mustermann, Max, Mitgliedsbeitrag WS+SS 2026</code><br><code>hF5Z638JTUW6J7iOYUzVJA, Mitgliedsbeitrag WS 2026</code>"
    },
    {
        question: "Bis wann muss der Beitrag spätestens bezahlt sein?",
        answer: "Der Beitrag muss spätestens bis zum <strong>1. des zweiten Monats der Abrechnungsperiode</strong> bezahlt werden. Eine Abrechnungsperiode beginnt jeweils zu den Semesterstart-Terminen der Hochschule Kempten. Effektiv ist das der 01. Mai im Sommersemester bzw. der 01. november im Wintersemester"
    },
    {
        question: "Muss ich den Beitrag auch in der Probezeit zahlen?",
        answer: "Nein. Mitglieder in der Probezeit sind bis zu deren Ende von der Beitragspflicht befreit. Endet die Probezeit später als drei Monate nach Beginn einer Abrechnungsperiode, ist der erste Beitrag erst in der darauffolgenden Periode fällig."
    },
    {
        question: "Kann ich einen ermäßigten Beitrag beantragen?",
        answer: "Ja. Bei schwieriger sozialer und/oder finanzieller Situation kann ein schriftlicher Antrag auf Beitragsermäßigung gestellt werden. <br><br><strong>Benötigte Unterlagen:</strong><br>Du brauchst einen aktuellen Nachweis, aus dem deine Situation erkennbar ist (z.B. aktueller Bescheid oder aktuelle Bescheinigung zu BAFöG oder Bürgergeld). Nicht benötigte Angaben dürfen geschwärzt werden.<br><br><strong>Ablauf:</strong><br>1. Schriftlichen Antrag mit kurzer Begründung erstellen.<br>2. Nachweis beifügen.<br>3. Antrag per Mail an <a href=\"mailto:vorstand@akgaming.de\">vorstand@akgaming.de</a> senden.<br>4. Der Vorstand prüft den Antrag und entscheidet im Rahmen der Beitragsordnung.<br>5. Bei Genehmigung wählst du deine Beitragshöhe innerhalb des Ermäßigungsrahmens und überweist diesen ganz normal.<br><br>Wird dem Antrag stattgegeben, wird statt dem regulären Beitrag ein Beitrag <strong>5€ bis 15€ pro Semester</strong> bzw. <strong>10€ bis 30€ pro Jahr</strong> fällig. Über die genaue Höhe innerhalb dieses Rahmens entscheidet das Mitglied selbst."
    },
    {
        question: "Ist auch eine komplette Beitragsbefreiung möglich?",
        answer: "Ja, in besonderen sozialen Härtefällen ist auf Antrag eine teilweise oder vollständige Befreiung möglich. Die Entscheidung trifft der Vorstand nach pflichtgemäßem Ermessen. Auch hierfür stellt du deinen Antrag per Mail an den Vorstand"
    },
    {
        question: "Was ist eine Fördermitgliedschaft?",
        answer: "Wenn du dich temporär aus dem aktiven Vereinsleben zurückziehen willst, kannst du eine Fördermitgliedschaft beantragen. In dieser Zeit entfallen Ansprüche auf vereinsinterne Veranstaltungen und Mitglieder-Benefits. Der Beitrag richtet sich dann nach den Regelungen zur Beitragsermäßigung, also ein Betrag in beliebiger Höhe im Rahmen von <strong>5€ bis 15€ pro Semester</strong> bzw. <strong>10€ bis 30€ pro Jahr</strong>."
    },
    {
        question: "Wie lange gilt eine Beitragsermäßigung oder -befreiung?",
        answer: "Grundsätzlich für zwei Semester. Bei fortbestehenden Voraussetzungen kann ein neuer Antrag gestellt werden."
    },
    {
        question: "Wohin sende ich den Antrag auf Ermäßigung oder Befreiung?",
        answer: "Der Antrag kann schriftlich an den Vorstand gesendet werden, z.B. per E-Mail an <a href=\"mailto:vorstand@akgaming.de\">vorstand@akgaming.de</a>."
    }
];

export default function Mitgliedsbeitrag() {
    return (
        <main className="mitgliedsbeitrag">
            <div className="mitgliedsbeitrag-content">
                <h1>Mitgliedsbeitrag</h1>
                <p>
                    Im Frühjahr 2025 haben wir in unserer Mitgliederversammlung erstmals die Einführung einer Beitragsordnung zur Regelung des mitgliedsbeitrags beschlossen, um uns langfristig von unserem bisherigen Modell der kostenlosen Mitgliedschaft zu verabschieden. In dieser ersten Fassung war der Mitgliedsbeitrag auf 0€ festgelegt, um Zeit für ein faires Konzept für alle Mitglieder zu gestalten. In unserer Mitgliederversammlung am 21. Februar 2026 haben wir eine <a href="/Beitragsordnung-AK-Gaming-e.V..pdf" target="_blank" rel="noopener noreferrer">neue Fassung</a> der Beitragsordnung beschlossen, in der ein Mitgliedsbeitrag auch effektiv eingeführt wurde.
                </p>
                <p>
                    Der Mitgliedsbeitrag unterstützt die Vereinsarbeit des AK Gaming e.V. und hilft uns dabei,
                    Events, Community-Angebote sowie organisatorische Aufgaben langfristig und unabhängig zu finanzieren. Einige Erläuterungen zum Grund der Einführung der Mitgliedsbeiträge findest du in den <a href="/Mitgliederversammlung_9.pdf" target="_blank" rel="noopener noreferrer">slides zum Thema</a> .
                </p>
                <p>
                    Alle verbindlichen Details sind in der aktuellen Beitragsordnung geregelt. Sollten ihr neben den unten aufgelisteten Fragen noch weitere haben, meldet euch gerne via Discord bei einem der Vorstände oder per mail an <a href="mailto:vorstand@akgaming.de">vorstand@akgaming.de</a>.
                </p>
                <h2>FAQ</h2>
                <section className="mitgliedsbeitrag-faqs" aria-label="Häufige Fragen zum Mitgliedsbeitrag">
                    {faqs.map((faq) => (
                        <details className="mitgliedsbeitrag-faq" key={faq.question}>
                            <summary>{faq.question}</summary>
                            <p dangerouslySetInnerHTML={{ __html: faq.answer }} />
                        </details>
                    ))}
                </section>
            </div>
        </main>
    );
}
