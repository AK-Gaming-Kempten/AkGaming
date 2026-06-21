import type { ReactNode } from "react";

type LeadProps = {
    children: ReactNode;
};

export default function Lead({ children }: LeadProps) {
    return <div className="mdx-lead">{children}</div>;
}
