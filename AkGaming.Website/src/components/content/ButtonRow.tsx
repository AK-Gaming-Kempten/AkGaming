import type { ReactNode } from "react";

type ButtonRowProps = {
    children: ReactNode;
};

export default function ButtonRow({ children }: ButtonRowProps) {
    return <div className="mdx-button-row">{children}</div>;
}
