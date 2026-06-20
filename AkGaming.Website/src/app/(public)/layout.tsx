import Footer from "../../components/Footer";
import Header from "../../components/Header";
import Navbar from "../../components/Navbar";

export default function PublicLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
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
    );
}
