import type { ComponentPropsWithoutRef } from "react";
import Link from "next/link";

type SmartLinkProps = ComponentPropsWithoutRef<"a">;

export function isInternalHref(href?: string) {
    return href !== undefined && href.startsWith("/");
}

export default function SmartLink({ href, children, ...props }: SmartLinkProps) {
    if (isInternalHref(href)) {
        return (
            <Link href={href as string} className="mdx-link">
                {children}
            </Link>
        );
    }

    return (
        <a href={href} {...props} className="mdx-link">
            {children}
        </a>
    );
}
