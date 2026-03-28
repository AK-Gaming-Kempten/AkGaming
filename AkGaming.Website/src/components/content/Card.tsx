import type { ReactNode } from "react";

type CardProps = {
    title?: string;
    eyebrow?: string;
    children: ReactNode;
};

export default function Card({ title, eyebrow, children }: CardProps) {
    return (
        <article className="mdx-card">
            {eyebrow !== undefined ? <p className="mdx-card-eyebrow">{eyebrow}</p> : null}
            {title !== undefined ? <h3>{title}</h3> : null}
            <div>{children}</div>
        </article>
    );
}
