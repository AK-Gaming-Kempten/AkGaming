import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { loadPosts } from "../data/loadPosts";
import { Post } from "../data/types";
import "./PostPage.css";

export default function PostPage() {
    const { postId } = useParams();
    const [post, setPost] = useState<Post | null>(null);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.find((p) => p.id === postId);
            setPost(found ?? null);
        });
    }, [postId]);

    if (!post) return <p>Loading...</p>;

    return (
        <main className="post-page">
            <h1>{post.title}</h1>
            <p className="post-short">{post.shortDescription}</p>
            <div className="post-content">
                <div className="post-text" dangerouslySetInnerHTML={{ __html: post.text }} />
            </div>
        </main>
    );
}
