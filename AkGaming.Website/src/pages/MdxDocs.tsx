import { Link } from "react-router-dom";
import MdxDocsNav from "../components/content/MdxDocsNav";
import { mdxDocGroups } from "../components/content/mdxCatalog";
import "./MdxDocs.css";

export default function MdxDocs() {
    return (
        <div className="mdx-docs-shell">
            <MdxDocsNav />
            <main className="mdx-docs-content">
                <section className="mdx-docs-hero">
                    <p className="mdx-docs-eyebrow">Content System</p>
                    <h1>MDX Komponenten</h1>
                    <p>
                        Diese Seite dokumentiert die verfugbaren MDX-Komponenten fur Posts und
                        Eventseiten. Wahle links oder mobil im Zusatzmenue eine Komponente fur die
                        Detailansicht.
                    </p>
                </section>

                {mdxDocGroups.map(([category, docs]) => (
                    <section key={category} className="mdx-docs-category">
                        <div className="mdx-docs-category-head">
                            <p className="mdx-docs-category-eyebrow">Kategorie</p>
                            <h2>{category}</h2>
                        </div>
                        <div className="mdx-docs-index-grid">
                            {docs.map((doc) => (
                                <Link key={doc.slug} to={`/mdx-docs/${doc.slug}`} className="mdx-docs-index-card">
                                    <div className="mdx-docs-index-head">
                                        <doc.icon className="mdx-docs-index-icon" aria-hidden="true" />
                                        <p className="mdx-docs-index-name">{doc.name}</p>
                                    </div>
                                    <p className="mdx-docs-index-description">{doc.description}</p>
                                </Link>
                            ))}
                        </div>
                    </section>
                ))}
            </main>
        </div>
    );
}
