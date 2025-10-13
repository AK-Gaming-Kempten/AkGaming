import "./SocialLink.css";
import type { IconType } from "react-icons";

interface SocialLinkProps {
    color: string;         // background color
    icon: IconType;        // imported icon component
    url: string;           // target link
    label?: string;        // accessible label (optional)
}

export default function SocialLink({ color, icon: Icon, url, label }: SocialLinkProps) {
    return (
        <a
            href={url}
            target="_blank"
            rel="noopener noreferrer"
            className="social-link"
            style={{ backgroundColor: color }}
            aria-label={label}
        >
            <Icon className="social-link-icon" />
        </a>
    );
}
