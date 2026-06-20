import type { Metadata } from "next";
import "../styles/akgaming-base-theme.css";
import "../index.css";
import "../App.css";
import Footer from "../components/Footer";
import Header from "../components/Header";
import Navbar from "../components/Navbar";

export const metadata: Metadata = {
    title: "AK Gaming e.V.",
    description: "AK Gaming e.V. Kempten",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="de">
            <body>
                <div className="site-shell">
                    <div className="top-chrome">
                        <Header />
                        <Navbar />
                    </div>
                    <main>
                        <div className="container">{children}</div>
                    </main>
                    <Footer />
                </div>
            </body>
        </html>
    );
}
