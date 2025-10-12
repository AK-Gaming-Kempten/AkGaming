export function formatDateRange(start: string, end?: string): string {
    const startDate = new Date(start);
    const endDate = end ? new Date(end) : null;

    const options: Intl.DateTimeFormatOptions = {
        day: "2-digit",
        month: "long",
        year: "numeric",
    };

    const formatter = new Intl.DateTimeFormat("de-DE", options);
    const startParts = formatter.formatToParts(startDate);
    const endParts = endDate ? formatter.formatToParts(endDate) : [];

    const sDay = startParts.find((p) => p.type === "day")?.value;
    const sMonth = startParts.find((p) => p.type === "month")?.value;
    const sYear = startParts.find((p) => p.type === "year")?.value;

    if (!endDate) {
        // Single-day event
        return `${sDay}. ${sMonth} ${sYear}`;
    }

    const eDay = endParts.find((p) => p.type === "day")?.value;
    const eMonth = endParts.find((p) => p.type === "month")?.value;
    const eYear = endParts.find((p) => p.type === "year")?.value;

    // Same month & year → "01.–03. September 2023"
    if (sMonth === eMonth && sYear === eYear) {
        return `${sDay}.–${eDay}. ${sMonth} ${sYear}`;
    }

    // Same year, different months → "25. September – 05. Oktober 2023"
    if (sYear === eYear) {
        return `${sDay}. ${sMonth} – ${eDay}. ${eMonth} ${sYear}`;
    }

    // Different years → "25. Dezember 2023 – 05. Januar 2024"
    return `${sDay}. ${sMonth} ${sYear} – ${eDay}. ${eMonth} ${eYear}`;
}
