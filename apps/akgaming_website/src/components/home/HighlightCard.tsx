import "./HighlightCard.css";
import { Link } from "react-router-dom";

interface HighlightCardProps {
    title: string;
    description: string;
    mediaSrc?: string;
    mediaType?: "image" | "video";
    postId?: string;
}

export default function HighlightCard({
                                          title,
                                          description,
                                          mediaSrc,
                                          mediaType = "image",
                                          postId,
                                      }: HighlightCardProps) {
    const content = (
        <div className="highlight-card">
            <div className="highlight-media">
                {mediaSrc ? (
                    mediaType === "video" ? (
                        <video
                            src={mediaSrc}
                            className="media-content"
                            autoPlay
                            loop
                            muted
                            playsInline
                        />
                    ) : (
                        <img src={mediaSrc} alt={title} className="media-content" />
                    )
                ) : (
                    <div className="media-placeholder"></div>
                )}
            </div>

            <div className="text">
                <h3>{title}</h3>
                <p>{description}</p>
            </div>
        </div>
    );

    return postId ? ( <Link to={`/posts/${postId}`}>{content}</Link> ): content;
}

