import { notFound } from "next/navigation";
import ContentPostPage from "../../../../components/content/ContentPostPage";

type PostRouteProps = {
    params: Promise<{ postId: string }>;
};

export const dynamic = "force-dynamic";

export default async function PostRoute({ params }: PostRouteProps) {
    const { postId } = await params;
    const page = await ContentPostPage({ id: postId, kind: "post" });
    return page ?? notFound();
}
