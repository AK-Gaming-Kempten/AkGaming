type LinkedImageProps = {
    href: string;
    src: string;
    alt: string;
    caption?: string;
};

export default function LinkedImage({ href, src, alt, caption }: LinkedImageProps) {
    return (
        <a
            href={href}
            className="mdx-linked-image"
            target="_blank"
            rel="noreferrer"
        >
            <img src={src} alt={alt} />
            {caption !== undefined ? <span className="mdx-linked-image-caption">{caption}</span> : null}
        </a>
    );
}
