import Header from "./components/Header";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import { Routes, Route } from "react-router-dom";
import Home from "./pages/Home";
import Events from "./pages/Events";
import Esports from "./pages/Esports";
import Impressum from "./pages/Impressum";
import "./App.css";

export default function App() {
    return (
        <>
            <Header />
            <Navbar />
            <main>
                <div className="container">
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/events" element={<Events />} />
                        <Route path="/esports" element={<Esports />} />
                        <Route path="/impressum" element={<Impressum />} />
                    </Routes>
                </div>
            </main>
            <Footer />
        </>
    );
}
