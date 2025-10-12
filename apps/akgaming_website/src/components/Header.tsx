import logo from "../assets/akgaming_logo.png";
import "./Header.css";

export default function Header() {
    return (
        <header className="header">
            <div className="header-content">
                <img src={logo} alt="AK Gaming e.V. Logo" className="header-logo" />
                <h1 className="header-title">AK Gaming e.V.</h1>
            </div>
        </header>
    );
}
