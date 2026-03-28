import type { ReactNode } from "react";

type CardGridProps = {
    columns?: 2 | 3;
    children: ReactNode;
};

export default function CardGrid({ columns = 2, children }: CardGridProps) {
    return (
        <div className={`mdx-card-grid mdx-card-grid-${columns}`}>
            {children}
        </div>
    );
}
