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
    // Vite's glob must be static
    const modules = import.meta.glob<{ default: string }>(
        "/public/media/**/*.{jpg,jpeg,png,webp,avif,gif}",
        { eager: true }
    );

    const prefix = `/public/media/${folder}/`;

    // 🔹 Object.entries returns [path, module], where module is typed
    const selected = Object.entries(modules)
        .filter(([path]) => path.startsWith(prefix))
        .map(([, mod]) => mod.default);

    // Now `selected` is string[]
    const images: LoadedImage[] = selected.map((src) => ({ src }));

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
