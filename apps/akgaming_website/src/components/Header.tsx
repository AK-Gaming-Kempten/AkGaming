import logo from "../assets/akgaming_logo.png";
import "./Header.css";
import { useTheme } from "../utils/UseTheme";
import { LuSunMedium, LuMoonStar, LuMonitor } from "react-icons/lu";

export default function Header() {
    const { theme, toggleTheme } = useTheme();

    const getIcon = () => {
        switch (theme) {
            case "light":
                return <LuSunMedium />;
            case "dark":
                return <LuMoonStar />;
            default:
                return <LuMonitor />;
        }
    };

    return (
        <header className="header">
            <button className="theme-toggle" onClick={toggleTheme} title={`Theme: ${theme}`}>
                {getIcon()}
            </button>

            <div className="header-content">
                <img src={logo} alt="AK Gaming e.V. Logo" className="header-logo" />
                <h1 className="header-title">AK Gaming e.V.</h1>
            </div>
        </header>
    );
}
