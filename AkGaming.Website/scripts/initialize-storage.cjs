const fs = require('node:fs/promises');
const path = require('node:path');

async function initializeDirectory(source, destination) {
    if (!destination) throw new Error('Persistent storage path is not configured.');
    const target = path.resolve(destination);
    await fs.mkdir(target, { recursive: true });
    const entries = await fs.readdir(target);
    if (entries.length > 0) {
        console.log(`Keeping existing CMS storage: ${target}`);
        return;
    }
    // Exclusive copies protect existing files even if another process creates them.
    for (const name of await fs.readdir(source)) {
        await fs.cp(path.join(source, name), path.join(target, name), { recursive: true, force: false, errorOnExist: true });
    }
    console.log(`Initialized empty CMS storage: ${target}`);
}

async function initializeStorage() {
    await initializeDirectory(path.join(__dirname, 'seed', 'content'), process.env.AKG_WEBSITE_CONTENT_ROOT);
    await initializeDirectory(path.join(__dirname, 'seed', 'media'), process.env.AKG_WEBSITE_MEDIA_ROOT);
}

module.exports = { initializeDirectory };
if (require.main === module) {
    initializeStorage().catch(error => {
        console.error(`CMS storage initialization failed: ${error.message}`);
        process.exitCode = 1;
    });
}
