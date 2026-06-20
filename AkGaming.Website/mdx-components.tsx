import type { MDXComponents } from "mdx/types";
import { mdxComponents } from "./src/components/content/mdxCatalog";

export function useMDXComponents(components: MDXComponents): MDXComponents {
    return {
        ...mdxComponents,
        ...components,
    };
}
