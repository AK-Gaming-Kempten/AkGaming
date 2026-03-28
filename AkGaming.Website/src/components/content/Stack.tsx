import type { ReactNode } from "react";

type StackProps = {
    children: ReactNode;
};

export default function Stack({ children }: StackProps) {
    return <div className="mdx-stack">{children}</div>;
}
