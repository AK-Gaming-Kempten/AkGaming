import type { ReactNode } from "react";

type PartnerGroupProps = {
    title: string;
    children: ReactNode;
};

export default function PartnerGroup({ title, children }: PartnerGroupProps) {
    return (
        <section className="mdx-partner-group">
            <h3>{title}</h3>
            <div>{children}</div>
        </section>
    );
}
