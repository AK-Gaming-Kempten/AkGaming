import { redirect } from "next/navigation";
import type { ContentKind } from "../../content/postStore";
import { getPublishedPost } from "../../content/postStore";
import { formatDateRange } from "../../utils/formatDateRange";
import RuntimeMdxContent from "./RuntimeMdxContent";

type ContentPostPageProps = {
    id: string;
    kind: ContentKind;
};

export default async function ContentPostPage({ id, kind }: ContentPostPageProps) {
    const post = await getPublishedPost(id);
    if (post === null)
        return null;

    if (post.type !== kind) {
        redirect(post.type === "event" ? `/events/${post.id}` : `/posts/${post.id}`);
    }

    const locationElement = post.type === "event" && post.locationUrl !== undefined ? (
        <a href={post.locationUrl} target="_blank" rel="noopener noreferrer">
            {post.location}
        </a>
    ) : post.type === "event" ? (
        <span>{post.location}</span>
    ) : null;

    return (
        <main className="post-page">
            <h1>{post.title}</h1>
            <p className="post-short">{post.shortDescription}</p>
            <div className="post-content">
                {post.type === "event" && (
                    <p className="post-meta">
                        📅 {formatDateRange(post.startDate ?? "", post.endDate)}<br />
                        <span>📍 {locationElement}</span>
                    </p>
                )}
                <div className="post-text">
                    <RuntimeMdxContent source={post.body} />
                </div>
            </div>
        </main>
    );
}
