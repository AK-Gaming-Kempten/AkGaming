import type { ReactNode } from "react";

type LeadProps = {
    children: ReactNode;
};

export default function Lead({ children }: LeadProps) {
    return <p className="mdx-lead">{children}</p>;
}
