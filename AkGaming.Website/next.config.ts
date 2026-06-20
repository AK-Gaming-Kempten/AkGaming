import createMdx from "@next/mdx";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    pageExtensions: ["ts", "tsx", "md", "mdx"],
    output: "standalone",
};

const withMdx = createMdx({
    extension: /\.mdx?$/,
    options: {
        remarkPlugins: ["remark-frontmatter", "remark-mdx-frontmatter"],
    },
});

export default withMdx(nextConfig);
