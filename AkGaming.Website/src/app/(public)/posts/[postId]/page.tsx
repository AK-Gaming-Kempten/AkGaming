import PostPage from "../../../../views/PostPage";

type PostRouteProps = {
    params: Promise<{ postId: string }>;
};

export default async function PostRoute({ params }: PostRouteProps) {
    const { postId } = await params;
    return <PostPage postId={postId} />;
}
