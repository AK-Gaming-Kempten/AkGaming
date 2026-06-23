import { compileMDX } from "next-mdx-remote/rsc";
import remarkGfm from "remark-gfm";
import { mdxComponents } from "./mdxCatalog";

type RuntimeMdxContentProps = {
    source: string;
};

export default async function RuntimeMdxContent({ source }: RuntimeMdxContentProps) {
    const { content } = await compileMDX({
        source,
        components: mdxComponents,
        options: {
            // CMS MDX relies on data expressions for component props, for example
            // <Table headers={[...]} rows={[[...]]} />. Keep the library's
            // dangerous-global guard enabled while allowing those expressions.
            blockJS: false,
            blockDangerousJS: true,
            mdxOptions: {
                development: false,
                remarkPlugins: [remarkGfm],
            },
        },
    });

    return <div className="mdx-content">{content}</div>;
}
