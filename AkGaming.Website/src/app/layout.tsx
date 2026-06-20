import type { Metadata } from "next";
import "../styles/akgaming-base-theme.css";
import "../index.css";
import "../App.css";

export const metadata: Metadata = {
    title: "AK Gaming e.V.",
    description: "AK Gaming e.V. Kempten",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="de">
            <body>{children}</body>
        </html>
    );
}
