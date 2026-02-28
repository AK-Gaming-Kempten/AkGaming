import { useParams, Navigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { loadPosts } from "../data/loadPosts";
import { Post, Event } from "../data/types";
import "./PostPage.css";

export default function PostPage() {
    const { postId } = useParams();
    const [post, setPost] = useState<Post | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.find((p) => p.id === postId) ?? null;
            setPost(found);
            setLoading(false);
        });
    }, [postId]);

    if (loading) return <p>Loading...</p>;
    if (!post) return <p>Post not found.</p>;

    // ✅ Check if it's an Event (using instanceof)
    if (post instanceof Event) {
        return <Navigate to={`/events/${post.id}`} replace />;
    }

    // ✅ Otherwise, render as a normal Post
    return (
        <main className="post-page">
            <h1>{post.title}</h1>
            <p className="post-short">{post.shortDescription}</p>
            <div className="post-content">
                <div
                    className="post-text"
                    dangerouslySetInnerHTML={{ __html: post.text }}
                />
            </div>
        </main>
    );
}
