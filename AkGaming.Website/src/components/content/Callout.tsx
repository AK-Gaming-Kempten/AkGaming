import type { ReactNode } from "react";

type CalloutProps = {
    title?: string;
    tone?: "neutral" | "accent" | "success" | "warning";
    children: ReactNode;
};

export default function Callout({ title, tone = "neutral", children }: CalloutProps) {
    return (
        <aside className={`mdx-callout mdx-callout-${tone}`}>
            {title !== undefined ? <h3>{title}</h3> : null}
            <div>{children}</div>
        </aside>
    );
}
