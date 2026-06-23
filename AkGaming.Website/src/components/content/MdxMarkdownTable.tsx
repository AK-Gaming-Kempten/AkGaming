import type { ComponentProps } from "react";

export default function MdxMarkdownTable({ children, ...props }: ComponentProps<"table">) {
    return (
        <div className="mdx-table-wrap">
            <table className="mdx-table" {...props}>{children}</table>
        </div>
    );
}
