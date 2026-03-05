import type { Event } from "../data/types";

const ONE_DAY_MS = 24 * 60 * 60 * 1000;
const ONE_WEEK_MS = 7 * ONE_DAY_MS;

type DatePrecision = "month" | "day" | "datetime";

type ParsedDate = {
    date: Date;
    precision: DatePrecision;
};

export type EventBounds = {
    startMs: number;
    endMs: number;
};

export function parseEventDate(value: string | Date | undefined): ParsedDate | null {
    if (!value) {
        return null;
    }

    if (value instanceof Date) {
        if (Number.isNaN(value.getTime())) {
            return null;
        }

        const hasTime =
            value.getHours() !== 0 ||
            value.getMinutes() !== 0 ||
            value.getSeconds() !== 0 ||
            value.getMilliseconds() !== 0;

        return {
            date: value,
            precision: hasTime ? "datetime" : "day",
        };
    }

    const trimmed = value.trim();
    if (!trimmed) {
        return null;
    }

    if (/^\d{4}-\d{2}$/.test(trimmed)) {
        const [year, month] = trimmed.split("-").map(Number);
        return {
            date: new Date(year, month - 1, 1),
            precision: "month",
        };
    }

    if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
        const [year, month, day] = trimmed.split("-").map(Number);
        return {
            date: new Date(year, month - 1, day),
            precision: "day",
        };
    }

    const parsed = new Date(trimmed);
    if (Number.isNaN(parsed.getTime())) {
        return null;
    }

    return {
        date: parsed,
        precision: /[T\s]\d{2}:\d{2}/.test(trimmed) ? "datetime" : "day",
    };
}

function startOfPrecision(parsed: ParsedDate): number {
    const { date, precision } = parsed;

    if (precision === "datetime") {
        return date.getTime();
    }

    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0).getTime();
}

function endOfPrecision(parsed: ParsedDate): number {
    const { date, precision } = parsed;

    if (precision === "datetime") {
        return date.getTime();
    }

    if (precision === "month") {
        return new Date(date.getFullYear(), date.getMonth() + 1, 0, 23, 59, 59, 999).getTime();
    }

    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999).getTime();
}

export function getEventBounds(event: Event): EventBounds | null {
    const start = parseEventDate(event.startDate);
    if (!start) {
        return null;
    }

    const end = parseEventDate(event.endDate);

    return {
        startMs: startOfPrecision(start),
        endMs: end ? endOfPrecision(end) : endOfPrecision(start),
    };
}

export function getVisibleUpcomingEvents(events: Event[], nowMs = Date.now()): Event[] {
    const thresholdMs = nowMs - ONE_WEEK_MS;

    return [...events]
        .map((event) => ({ event, bounds: getEventBounds(event) }))
        .filter((item) => item.bounds && item.bounds.endMs >= thresholdMs)
        .sort((a, b) => {
            if (a.bounds!.startMs !== b.bounds!.startMs) {
                return a.bounds!.startMs - b.bounds!.startMs;
            }

            return a.bounds!.endMs - b.bounds!.endMs;
        })
        .map((item) => item.event);
}
