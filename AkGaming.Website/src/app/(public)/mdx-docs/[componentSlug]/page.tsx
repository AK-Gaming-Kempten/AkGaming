import MdxDocDetail from "../../../../views/MdxDocDetail";

type MdxDocsDetailRouteProps = {
    params: Promise<{ componentSlug: string }>;
};

export default async function MdxDocsDetailRoute({ params }: MdxDocsDetailRouteProps) {
    const { componentSlug } = await params;
    return <MdxDocDetail componentSlug={componentSlug} />;
}
