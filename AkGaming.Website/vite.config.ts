import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    server: {
        fs: {
            allow: [".."],
        },
    },
    define: {
        global: "window", // for libs assuming global=Node
    },
    resolve: {
        alias: {
            buffer: "buffer",
        },
    },
});
