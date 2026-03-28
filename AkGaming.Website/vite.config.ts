import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import mdx from "@mdx-js/rollup";
import remarkFrontmatter from "remark-frontmatter";
import remarkMdxFrontmatter from "remark-mdx-frontmatter";

export default defineConfig({
    plugins: [
        mdx({
            remarkPlugins: [remarkFrontmatter, remarkMdxFrontmatter],
        }),
        react(),
    ],
    server: {
        fs: {
            allow: [".."],
        },
    },
    define: {
        global: "window", // for libs assuming global=Node
    },
    resolve: {
        alias: {
            buffer: "buffer",
        },
    },
});
