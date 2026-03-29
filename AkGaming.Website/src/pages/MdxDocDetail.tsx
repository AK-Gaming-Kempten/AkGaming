import { Navigate, useParams } from "react-router-dom";
import MdxDocsNav from "../components/content/MdxDocsNav";
import { getMdxComponentDoc } from "../components/content/mdxCatalog";
import "./MdxDocs.css";

export default function MdxDocDetail() {
    const { componentSlug } = useParams();
    const doc = getMdxComponentDoc(componentSlug);

    if (doc === undefined) {
        return <Navigate to="/mdx-docs" replace />;
    }

    return (
        <div className="mdx-docs-shell">
            <MdxDocsNav />
            <main className="mdx-docs-content">
                <section className="mdx-docs-hero">
                    <p className="mdx-docs-eyebrow">{doc.category}</p>
                    <div className="mdx-docs-hero-title-row">
                        <span className="mdx-docs-hero-icon">
                            <doc.icon aria-hidden="true" />
                        </span>
                        <h1>{doc.name}</h1>
                    </div>
                    <p className="mdx-docs-hero-description">{doc.description}</p>
                </section>

                <article className="mdx-docs-entry">
                    <div className="mdx-docs-entry-head">
                        <h2>Syntax</h2>
                        <code>{doc.syntax}</code>
                    </div>
                    <div className="mdx-docs-panel mdx-docs-preview-panel">
                        <h3>Vorschau</h3>
                        <div className="mdx-docs-preview-inner">
                            {doc.preview}
                        </div>
                    </div>
                    <div className="mdx-docs-panel">
                        <h3>Beispiel</h3>
                        <pre>
                            <code>{doc.example}</code>
                        </pre>
                    </div>
                    <div className="mdx-docs-preview mdx-docs-props-section">
                        <h3>Props</h3>
                        <div className="mdx-docs-props">
                            {doc.props.map((prop) => (
                                <article key={prop.name} className="mdx-docs-prop-card">
                                    <div className="mdx-docs-prop-head">
                                        <div className="mdx-docs-prop-title-row">
                                            <span
                                                className={`mdx-docs-prop-status ${prop.required ? "required" : "optional"}`}
                                                role="img"
                                                aria-label={prop.required ? "Required prop" : "Optional prop"}
                                            >
                                                {prop.required ? "*" : "?"}
                                            </span>
                                            <code>{prop.name}</code>
                                        </div>
                                        <span className="mdx-docs-prop-type">{prop.type}</span>
                                    </div>
                                    <p className="mdx-docs-prop-meta">
                                        {prop.required ? "Required" : "Optional"}
                                        {prop.defaultValue !== undefined ? `, default ${prop.defaultValue}` : ""}
                                    </p>
                                    <p className="mdx-docs-prop-description">{prop.description}</p>
                                </article>
                            ))}
                        </div>
                    </div>
                </article>
            </main>
        </div>
    );
}
