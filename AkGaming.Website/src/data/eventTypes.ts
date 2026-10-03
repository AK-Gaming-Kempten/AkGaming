export const EventType = {
    Gamenight: "gamenight",
    GameJam: "gamejam",
    BoardGames: "boardgames",
    Other: "other",
} as const;

export type EventType = typeof EventType[keyof typeof EventType];

export const eventTypeOptions = [
    { value: EventType.Gamenight, label: "Gamenight" },
    { value: EventType.GameJam, label: "Game Jam" },
    { value: EventType.BoardGames, label: "Brettspielabend" },
    { value: EventType.Other, label: "Other events" },
];

export function isEventType(value: unknown): value is EventType {
    return Object.values(EventType).includes(value as EventType);
}
