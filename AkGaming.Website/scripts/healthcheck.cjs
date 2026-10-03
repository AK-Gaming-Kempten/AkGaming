const port = process.env.PORT || '3000';
const url = `http://127.0.0.1:${port}/health`;

async function checkHealth() {
    try {
        const response = await fetch(url, { signal: AbortSignal.timeout(4000) });
        if (response.status !== 200) {
            console.error(`Healthcheck failed: ${url} returned HTTP ${response.status}.`);
            process.exitCode = 1;
            return;
        }
        const body = await response.text();
        if (body.trim() !== 'Healthy') {
            console.error(`Healthcheck failed: ${url} returned an unexpected response.`);
            process.exitCode = 1;
        }
    } catch (error) {
        console.error(`Healthcheck failed: ${url}: ${error.message}`);
        process.exitCode = 1;
    }
}

checkHealth();
