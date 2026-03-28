type EmbedProps = {
    src: string;
    title: string;
};

export default function Embed({ src, title }: EmbedProps) {
    return (
        <div className="mdx-embed">
            <iframe
                src={src}
                title={title}
                loading="lazy"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                allowFullScreen
            />
        </div>
    );
}
