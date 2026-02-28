import "./SocialLinks.css";
import SocialLink from "./SocialLink";
import {FaDiscord, FaFacebook, FaInstagram, FaLinkedin, FaTwitch, FaYoutube} from "react-icons/fa";

export default function SocialLinks() {
    const links = [
        {
            color: "#5865F2",
            icon: FaDiscord,
            url: "https://discord.gg/5J5uJKJAhT",
            label: "Discord",
        },
        {
            color: "#E1306C",
            icon: FaInstagram,
            url: "https://www.instagram.com/akgamingkempten/",
            label: "Instagram",
        },
        {
            color: "#FF0000",
            icon: FaYoutube,
            url: "https://youtube.com/@akgamingkempten",
            label: "YouTube",
        },
        {
            color: "#9146FF",
            icon: FaTwitch,
            url: "https://twitch.tv/akgamingkempten",
            label: "Twitch",
        },
        {
            color: "#0077B6",
            icon: FaLinkedin,
            url: "https://www.linkedin.com/company/akgaming",
            label: "YouTube",
        },
        {
            color: "#1877F2",
            icon: FaFacebook,
            url: "https://www.facebook.com/AKGamingKempten",
            label: "YouTube",
        },
    ];

    return (
        <div className="social-links-grid">
            {links.map((link) => (
                <SocialLink key={link.label} {...link} />
            ))}
        </div>
    );
}
