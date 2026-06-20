import { mdxDocGroups } from "./mdxCatalog";
import ActiveLink from "../navigation/ActiveLink";
import "./MdxDocsNav.css";

export default function MdxDocsNav() {
    return (
        <aside className="mdx-docs-nav" aria-label="MDX Komponenten Navigation">
            <div className="mdx-docs-nav-header">
                <p className="mdx-docs-nav-eyebrow">Content System</p>
                <ActiveLink href="/mdx-docs" exact className="mdx-docs-nav-home">
                    MDX Komponenten
                </ActiveLink>
            </div>
            {mdxDocGroups.map(([category, docs]) => (
                <div key={category} className="mdx-docs-nav-group">
                    <p className="mdx-docs-nav-group-title">{category}</p>
                    <ul className="mdx-docs-nav-list">
                        {docs.map((doc) => (
                            <li key={doc.slug}>
                                <ActiveLink href={`/mdx-docs/${doc.slug}`} className="mdx-docs-nav-link">
                                    <doc.icon className="mdx-docs-nav-link-icon" aria-hidden="true" />
                                    {doc.name}
                                </ActiveLink>
                            </li>
                        ))}
                    </ul>
                </div>
            ))}
        </aside>
    );
}
