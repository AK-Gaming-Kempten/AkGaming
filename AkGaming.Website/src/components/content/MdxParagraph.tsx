import type { ReactNode } from "react";

type MdxParagraphProps = {
    children: ReactNode;
};

export default function MdxParagraph({ children }: MdxParagraphProps) {
    return <span className="mdx-paragraph">{children}</span>;
}
