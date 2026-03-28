import type { ReactNode } from "react";

type PriceCardProps = {
    title: string;
    price: string;
    note?: string;
    children?: ReactNode;
};

export default function PriceCard({ title, price, note, children }: PriceCardProps) {
    return (
        <article className="mdx-price-card">
            <p className="mdx-price-card-title">{title}</p>
            <p className="mdx-price-card-price">{price}</p>
            {note !== undefined ? <p className="mdx-price-card-note">{note}</p> : null}
            {children !== undefined ? <div className="mdx-price-card-body">{children}</div> : null}
        </article>
    );
}
