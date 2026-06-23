import Footer from "../../components/Footer";
import TopChrome from "../../components/TopChrome";

export default function PublicLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <div className="site-shell">
            <TopChrome />
            <main>
                <div className="container">{children}</div>
            </main>
            <Footer />
        </div>
    );
}
