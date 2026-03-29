import type { ElementType, ReactNode } from "react";

type TextProps = {
    as?: ElementType;
    size?: "xs" | "sm" | "md" | "lg" | "xl";
    color?: "primary" | "secondary" | "highlight" | "special";
    weight?: "regular" | "medium" | "semibold" | "bold";
    children: ReactNode;
};

export default function Text({
    as: Tag = "p",
    size = "md",
    color = "primary",
    weight = "regular",
    children,
}: TextProps) {
    return (
        <Tag className={`mdx-text mdx-text-size-${size} mdx-text-color-${color} mdx-text-weight-${weight}`}>
            {children}
        </Tag>
    );
}
