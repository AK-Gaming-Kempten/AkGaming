import type { ReactNode } from "react";

type ColumnsProps = {
    columns?: 2 | 3;
    children: ReactNode;
};

export default function Columns({ columns = 2, children }: ColumnsProps) {
    return <div className={`mdx-columns mdx-columns-${columns}`}>{children}</div>;
}
