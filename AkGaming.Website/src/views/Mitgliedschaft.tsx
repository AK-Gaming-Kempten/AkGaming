import "./Mitgliedschaft.css";

export default function Mitgliedschaft() {
    return (
        <main className="mitgliedschaft-page">
            <section className="mitgliedschaft-hero">
                <p className="mitgliedschaft-eyebrow">AK Gaming e.V.</p>
                <h1>Mitgliedschaft</h1>
                <p>
                    Werde Teil unserer Community in Kempten und bring dich in Events,
                    Vereinsleben und E-Sports ein. Hier findest du die wichtigsten Punkte
                    zu Aufnahme, Rechten, Pflichten und Mitgliedsbeitrag.
                </p>
                <div className="mitgliedschaft-hero-actions">
                    <a
                        href="https://management.akgaming.de/membership"
                        target="_blank"
                        rel="noreferrer"
                        className="mitgliedschaft-btn mitgliedschaft-btn-primary"
                    >
                        Jetzt Mitglied werden
                    </a>
                </div>
            </section>

            <section className="mitgliedschaft-grid">
                <article className="mitgliedschaft-card">
                    <h2>Was Mitglieder bei uns tun</h2>
                    <ul>
                        <li>Teilnahme an Vereins- und Community-Events vor Ort und online.</li>
                        <li>Mitwirkung in Teams, Turnieren und vereinsbezogenen Projekten.</li>
                        <li>Unterstützung des Vereinszwecks gemäß Satzung (Jugendhilfe, Teamgeist, Fairness, Vernetzung).</li>
                    </ul>
                </article>

                <article className="mitgliedschaft-card">
                    <h2>Was Mitglieder erhalten</h2>
                    <ul>
                        <li>Zugang zu Vereinsressourcen und vereinsinternen Angeboten.</li>
                        <li>Vergünstigter Zugang zu unseren Events.</li>
                        <li>Möglichkeit, das Vereinsleben und unsere Events aktiv mitzugestalten.</li>
                    </ul>
                </article>

                <article className="mitgliedschaft-card">
                    <h2>Aufnahme und Mitgliedsstatus</h2>
                    <ul>
                        <li>Mitglied kann jede natürliche oder juristische Person ab 18 Jahren werden.</li>
                        <li>Natürliche Personen durchlaufen zunächst eine 180-tägige Probezeit.</li>
                        <li>Die Probezeit endet sofort nach aktiver Beteiligung an zwei Events.</li>
                    </ul>
                </article>

                <article className="mitgliedschaft-card">
                    <h2>Rechte und Pflichten</h2>
                    <ul>
                        <li>Nutzung der Vereinseinrichtungen im Rahmen der Satzung.</li>
                        <li>Respektvoller Umgang, Einhaltung von Regeln und Schutz sensibler Vereinsdaten.</li>
                        <li>Pflicht zur Beitragszahlung nach den Regelungen der Beitragsordnung.</li>
                    </ul>
                </article>

                <article className="mitgliedschaft-card">
                    <h2>Mitgliedsbeitrag</h2>
                    <ul>
                        <li>Grundbeitrag: 15 EUR je Semester oder 30 EUR jährlich.</li>
                        <li>In der Probezeit besteht keine Beitragspflicht.</li>
                        <li>Ermäßigung/Befreiung in sozialen oder finanziellen Härtefällen oder in Form einer Fördermitgliedschaft möglich.</li>
                    </ul>
                    <br />
                    <a href="/mitgliedschaft/mitgliedsbeitrag" className="mitgliedschaft-btn mitgliedschaft-btn-secondary">
                        Details zum Beitrag
                    </a>
                </article>

                <article className="mitgliedschaft-card">
                    <h2>Verbindliche Dokumente</h2>
                    <p>Maßgeblich sind ausschließlich die jeweils gültigen Originalfassungen:</p>
                    <br />
                    <p>
                        <a href="/Vereinssatzung-AK-Gaming-e.V..pdf" target="_blank" rel="noreferrer">Satzung (PDF)</a><br />
                        <br />
                        <a href="/Beitragsordnung-AK-Gaming-e.V..pdf" target="_blank" rel="noreferrer">Beitragsordnung (PDF)</a><br />
                    </p>
                </article>
            </section>
        </main>
    );
}

