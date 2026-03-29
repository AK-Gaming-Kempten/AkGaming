import type { PostContentComponent } from "../../data/types";
import { mdxComponents } from "./mdxCatalog";
import "./MdxContent.css";

type MdxContentProps = {
    Content: PostContentComponent;
};

export default function MdxContent({ Content }: MdxContentProps) {
    return (
        <div className="mdx-content">
            <Content components={mdxComponents} />
        </div>
    );
}
