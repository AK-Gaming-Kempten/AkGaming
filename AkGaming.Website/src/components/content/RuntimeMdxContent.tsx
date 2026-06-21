import { compileMDX } from "next-mdx-remote/rsc";
import { mdxComponents } from "./mdxCatalog";

type RuntimeMdxContentProps = {
    source: string;
};

export default async function RuntimeMdxContent({ source }: RuntimeMdxContentProps) {
    const { content } = await compileMDX({
        source,
        components: mdxComponents,
        options: {
            mdxOptions: {
                development: false,
            },
        },
    });

    return <div className="mdx-content">{content}</div>;
}
