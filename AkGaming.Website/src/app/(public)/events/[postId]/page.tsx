import { notFound } from "next/navigation";
import ContentPostPage from "../../../../components/content/ContentPostPage";

type EventRouteProps = {
    params: Promise<{ postId: string }>;
};

export const dynamic = "force-dynamic";

export default async function EventRoute({ params }: EventRouteProps) {
    const { postId } = await params;
    const page = await ContentPostPage({ id: postId, kind: "event" });
    return page ?? notFound();
}
