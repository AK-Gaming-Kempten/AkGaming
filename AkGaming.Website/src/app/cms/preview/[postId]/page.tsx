import { notFound } from "next/navigation";
import { CmsPermissions, hasCmsPermission } from "../../../../content/cmsAuthorization";
import { getEditablePost } from "../../../../content/postStore";
import RuntimeMdxContent from "../../../../components/content/RuntimeMdxContent";

type PreviewPageProps = { params: Promise<{ postId: string }> };

export default async function PreviewPage({ params }: PreviewPageProps) {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return notFound();

    const { postId } = await params;
    const post = await getEditablePost(postId);
    if (post === null)
        return notFound();

    return <main className="post-page"><h1>{post.title}</h1><p className="post-short">{post.shortDescription}</p><div className="post-content"><div className="post-text"><RuntimeMdxContent source={post.body} /></div></div></main>;
}
