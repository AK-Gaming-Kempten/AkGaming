import { Routes, Route } from "react-router-dom";
import Header from "./components/Header";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import Home from "./pages/Home";
import Events from "./pages/Events";
import Esports from "./pages/Esports";
import Impressum from "./pages/Impressum";
import PostPage from "./pages/PostPage";
import EventPage from "./pages/EventPage.tsx";
import "./App.css";
import Haftung from "./pages/Haftung.tsx";
import Datenschutz from "./pages/Datenschutz.tsx";
import Mitgliedsbeitrag from "./pages/Mitgliedsbeitrag.tsx";
import Mitgliedschaft from "./pages/Mitgliedschaft.tsx";

export default function App() {
    return (
        <>
            <div className="top-chrome">
                <Header />
                <Navbar />
            </div>
            <main>
                <div className="container">
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/events" element={<Events />} />
                        <Route path="/esports" element={<Esports />} />
                        <Route path="/mitgliedschaft" element={<Mitgliedschaft />} />
                        <Route path="/haftung" element={<Haftung />} />
                        <Route path="/datenschutz" element={<Datenschutz/>} />
                        <Route path="/mitgliedschaft/mitgliedsbeitrag" element={<Mitgliedsbeitrag />} />
                        <Route path="/impressum" element={<Impressum />} />
                        <Route path="/posts/:postId" element={<PostPage />} />
                        <Route path="/events/:postId" element={<EventPage />} />
                    </Routes>
                </div>
            </main>
            <Footer />
        </>
    );
}
