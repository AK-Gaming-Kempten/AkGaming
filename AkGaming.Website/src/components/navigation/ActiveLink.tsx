"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

type ActiveLinkProps = {
    href: string;
    children: ReactNode;
    className?: string;
    activeClassName?: string;
    exact?: boolean;
    onClick?: () => void;
};

export default function ActiveLink({
    href,
    children,
    className,
    activeClassName = "active",
    exact = false,
    onClick,
}: ActiveLinkProps) {
    const pathname = usePathname();
    const isActive = exact ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);
    const resolvedClassName = [className, isActive ? activeClassName : undefined].filter(Boolean).join(" ");

    return (
        <Link href={href} className={resolvedClassName || undefined} onClick={onClick}>
            {children}
        </Link>
    );
}
