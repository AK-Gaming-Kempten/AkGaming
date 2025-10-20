import "./Impressum.css"

export default function Impressum() {
    return (
        <main className="impressum">
            <h1>Impressum</h1>
            <div className="impressum-content">
                <div>
                    <h2>Satzung</h2>
                    <p>
                        <div>AK Gaming e.V.</div>
                        <div>VR 201431 Amtsgericht Kempten (Allgäu)</div>
                        <a href="public/Vereinssatzung-AK-Gaming-e.V..pdf" target="_blank" rel="noopener noreferrer">Satzung vom 28.07.2021</a>
                    </p>
                </div>
                <div>
                    <h2>Kontakt</h2>
                    <p>
                        <div>AK Gaming e.V.</div>
                        <div>Bahnhofstraße 61</div>
                        <div>87435 Kempten (Allgäu)</div>
                        <a href="mailto:info@akgaming.de">info@akgaming.de</a>
                    </p>
                </div>
                <div>
                    <h2>Vorstand</h2>
                    <p>
                        <div>Johannes Mehler</div>
                        <div>Colin Gaiser</div>
                        <div>Moritz Rösler</div>
                        <a href="mailto:vorstand@akgaming.de">vorstand@akgaming.de</a>
                    </p>
                </div>
                <div>
                    <h2>Ansprechpartner Abteilung ESports</h2>
                    <p>
                        <div>Hannah Martin</div>
                        <a href="mailto:esport@akgaming.de">esport@akgaming.de</a>
                    </p>
                </div>
                <div>
                    <h2>Ansprechpartner Social Media</h2>
                    <p>
                        <div>Lennart Hartmann</div>
                        <a href="mailto:socialmedia@akgaming.de">socialmedia@akgaming.de</a>
                    </p>
                </div>
            </div>
        </main>
    );
}