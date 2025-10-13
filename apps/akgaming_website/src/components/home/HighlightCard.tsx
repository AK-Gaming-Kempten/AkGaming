import "./HighlightCard.css";

interface HighlightCardProps {
    title: string;
    description: string;
    mediaSrc?: string;   // URL to image/video/gif
    mediaType?: "image" | "video"; // type hint for rendering
}

export default function HighlightCard({
                                          title,
                                          description,
                                          mediaSrc,
                                          mediaType = "image",
                                      }: HighlightCardProps) {
    return (
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
}
