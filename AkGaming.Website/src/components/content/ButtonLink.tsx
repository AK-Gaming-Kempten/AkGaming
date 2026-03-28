import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { isInternalHref } from "./SmartLink";

type ButtonLinkProps = {
    href: string;
    variant?: "primary" | "secondary";
    children: ReactNode;
};

export default function ButtonLink({ href, variant = "primary", children }: ButtonLinkProps) {
    const className = `mdx-button mdx-button-${variant}`;

    if (isInternalHref(href)) {
        return (
            <Link to={href} className={className}>
                {children}
            </Link>
        );
    }

    return (
        <a
            href={href}
            className={className}
            target="_blank"
            rel="noreferrer"
        >
            {children}
        </a>
    );
}
