export function formatDateRange(start: string, end?: string): string {
    const startDate = new Date(start);
    const endDate = end ? new Date(end) : null;

    const hasTime = start.length > 10 || (end && end?.length > 10);

    // --- base date options ---
    const dateOptions: Intl.DateTimeFormatOptions = {
        day: "2-digit",
        month: "long",
        year: "numeric",
    };

    // --- if we have a time, add hours & minutes ---
    const timeOptions: Intl.DateTimeFormatOptions = hasTime
        ? { hour: "2-digit", minute: "2-digit" }
        : {};

    const fullOptions: Intl.DateTimeFormatOptions = {
        ...dateOptions,
        ...timeOptions,
    };

    const fmt = new Intl.DateTimeFormat("de-DE", fullOptions);

    // Single date
    if (!endDate) {
        return fmt.format(startDate);
    }

    // Same month & year → "01.–03. September 2023"
    if (sameMonthAndYear(startDate, endDate) && !hasTime) {
        return `${formatDay(startDate)}.–${formatDay(endDate)}. ${formatMonth(startDate)} ${startDate.getFullYear()}`;
    }

    // Same year, different months → "25. September – 05. Oktober 2023"
    if (sameYear(startDate, endDate) && !hasTime) {
        return `${formatDay(startDate)}. ${formatMonth(startDate)} – ${formatDay(endDate)}. ${formatMonth(endDate)} ${startDate.getFullYear()}`;
    }

    // Different years (still no times)
    if (!hasTime) {
        return `${formatDay(startDate)}. ${formatMonth(startDate)} ${startDate.getFullYear()} – ${formatDay(endDate)}. ${formatMonth(endDate)} ${endDate.getFullYear()}`;
    }

    // If times are present, show full date & time for both
    return `${fmt.format(startDate)} – ${fmt.format(endDate)}`;
}

// --- helpers ---
function sameYear(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear();
}

function sameMonthAndYear(a: Date, b: Date): boolean {
    return sameYear(a, b) && a.getMonth() === b.getMonth();
}

function formatDay(d: Date): string {
    return d.getDate().toString().padStart(2, "0");
}

function formatMonth(d: Date): string {
    return d.toLocaleDateString("de-DE", { month: "long" });
}
