import "./Impressum.css"

export default function Impressum() {
    return (
        <main className="impressum">
            <section className="impressum-hero">
                <h1>Impressum</h1>
                <p>Rechtliche Angaben und Kontaktinformationen des AK Gaming e.V.</p>
            </section>

            <section className="impressum-content">
                <article className="impressum-card">
                    <h2>Satzung</h2>
                    <p>AK Gaming e.V.</p>
                    <p>VR 201431 Amtsgericht Kempten (Allgäu)</p>
                    <p className="impressum-links">
                        <a href="/Vereinssatzung-AK-Gaming-e.V..pdf" target="_blank" rel="noopener noreferrer">Satzung vom 02.12.2025</a> <br />
                        <a href="/Beitragsordnung-AK-Gaming-e.V..pdf" target="_blank" rel="noopener noreferrer">Beitragsordnung vom 21.02.2026</a>
                    </p>
                </article>

                <article className="impressum-card">
                    <h2>Kontakt</h2>
                    <p>AK Gaming e.V.</p>
                    <p>Bahnhofstraße 61</p>
                    <p>87435 Kempten (Allgäu)</p>
                    <p>
                        <a href="mailto:info@akgaming.de">info@akgaming.de</a>
                    </p>
                </article>

                <article className="impressum-card">
                    <h2>Vorstand</h2>
                    <p>Kai Höft</p>
                    <p>Colin Gaiser</p>
                    <p>Stefan Oswald</p>
                    <p>
                        <a href="mailto:vorstand@akgaming.de">vorstand@akgaming.de</a>
                    </p>
                </article>

                <article className="impressum-card">
                    <h2>Ansprechpartner Abteilung ESports</h2>
                    <p>
                        Hannah Martin
                    </p>
                    <p>
                        <a href="mailto:esport@akgaming.de">esport@akgaming.de</a>
                    </p>
                </article>

                <article className="impressum-card">
                    <h2>Ansprechpartner Social Media</h2>
                    <p>
                        Lennart Hartmann
                    </p>
                    <p>
                        <a href="mailto:socialmedia@akgaming.de">socialmedia@akgaming.de</a>
                    </p>
                </article>
            </section>
        </main>
    );
}
