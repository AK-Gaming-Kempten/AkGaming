import { NavLink } from "react-router-dom";
import { mdxDocGroups } from "./mdxCatalog";
import "./MdxDocsNav.css";

export default function MdxDocsNav() {
    return (
        <aside className="mdx-docs-nav" aria-label="MDX Komponenten Navigation">
            <div className="mdx-docs-nav-header">
                <p className="mdx-docs-nav-eyebrow">Content System</p>
                <NavLink to="/mdx-docs" end className="mdx-docs-nav-home">
                    MDX Komponenten
                </NavLink>
            </div>
            {mdxDocGroups.map(([category, docs]) => (
                <div key={category} className="mdx-docs-nav-group">
                    <p className="mdx-docs-nav-group-title">{category}</p>
                    <ul className="mdx-docs-nav-list">
                        {docs.map((doc) => (
                            <li key={doc.slug}>
                                <NavLink
                                    to={`/mdx-docs/${doc.slug}`}
                                    className={({ isActive }) =>
                                        `mdx-docs-nav-link ${isActive ? "active" : ""}`
                                    }
                                >
                                    <doc.icon className="mdx-docs-nav-link-icon" aria-hidden="true" />
                                    {doc.name}
                                </NavLink>
                            </li>
                        ))}
                    </ul>
                </div>
            ))}
        </aside>
    );
}
