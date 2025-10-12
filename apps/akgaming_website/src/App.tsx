import { Routes, Route } from "react-router-dom";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import Home from "./pages/Home";
import Events from "./pages/Events";
import Esports from "./pages/Esports";
import Impressum from "./pages/Impressum";

export default function App() {
    return (
        <>
            <Navbar />
            <div style={{ minHeight: "80vh", padding: "1rem" }}>
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/events" element={<Events />} />
                    <Route path="/esports" element={<Esports />} />
                    <Route path="/impressum" element={<Impressum />} />
                </Routes>
            </div>
            <Footer />
        </>
    );
}
