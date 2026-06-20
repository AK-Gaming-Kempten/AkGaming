export interface LoadedImage {
    src: string;
    width?: number;
    height?: number;
}

/**
 * Loads all images from /public/media/{folder}/ and optionally measures dimensions.
 */
export async function loadImages(
    folder: string,
    options: { includeDimensions?: boolean } = {}
): Promise<LoadedImage[]> {
    const response = await fetch(`/api/content/images?folder=${encodeURIComponent(folder)}`);
    if (!response.ok)
        throw new Error("Unable to load event images.");

    const sources = await response.json() as string[];
    const images: LoadedImage[] = sources.map((src) => ({ src }));

    if (options.includeDimensions) {
        const measured: LoadedImage[] = await Promise.all(
            images.map(
                (img) =>
                    new Promise<LoadedImage>((resolve) => {
                        const image = new Image();
                        image.onload = () =>
                            resolve({
                                src: img.src,
                                width: image.width,
                                height: image.height,
                            });
                        image.src = img.src;
                    })
            )
        );
        return measured;
    }

    return images;
}
