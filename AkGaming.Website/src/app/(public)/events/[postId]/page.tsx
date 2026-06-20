import EventPage from "../../../../views/EventPage";

type EventRouteProps = {
    params: Promise<{ postId: string }>;
};

export default async function EventRoute({ params }: EventRouteProps) {
    const { postId } = await params;
    return <EventPage postId={postId} />;
}
