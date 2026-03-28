import type { ReactNode } from "react";

type PriceGridProps = {
    children: ReactNode;
};

export default function PriceGrid({ children }: PriceGridProps) {
    return <div className="mdx-price-grid">{children}</div>;
}
