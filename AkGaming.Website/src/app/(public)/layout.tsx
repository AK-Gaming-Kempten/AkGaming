import Footer from "../../components/Footer";
import TopChrome from "../../components/TopChrome";

export default function PublicLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <div className="site-shell">
            <TopChrome />
            <main className="public-main">
                {children}
            </main>
            <Footer />
        </div>
    );
}
