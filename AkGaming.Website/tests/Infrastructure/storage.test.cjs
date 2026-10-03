const { beforeEach, afterEach, test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { initializeDirectory } = require('../../scripts/initialize-storage.cjs');

let root;
let source;
let destination;
beforeEach(async () => {
    root = await fs.mkdtemp(path.join(os.tmpdir(), 'akgaming-storage-'));
    source = path.join(root, 'seed');
    destination = path.join(root, 'persistent');
    await fs.mkdir(path.join(source, 'posts'), { recursive: true });
    await fs.writeFile(path.join(source, 'posts', 'event.mdx'), 'Bundled event');
});
afterEach(async () => {
    await fs.rm(root, { recursive: true, force: true });
});

test('Initial startup seeds an empty persistent directory', async () => {
    // Arrange
    await fs.mkdir(destination);
    // Act
    await initializeDirectory(source, destination);
    // Assert
    assert.equal(await fs.readFile(path.join(destination, 'posts', 'event.mdx'), 'utf8'), 'Bundled event');
});

test('A redeployment preserves CMS edits, additions, and deletions', async () => {
    // Arrange
    await initializeDirectory(source, destination);
    await fs.writeFile(path.join(destination, 'posts', 'event.mdx'), 'CMS edit');
    await fs.writeFile(path.join(destination, 'posts', 'new.mdx'), 'CMS addition');
    await fs.writeFile(path.join(source, 'new-default.yaml'), 'New bundled catalog');
    // Act
    await initializeDirectory(source, destination);
    await fs.unlink(path.join(destination, 'posts', 'event.mdx'));
    await initializeDirectory(source, destination);
    // Assert
    assert.equal(await fs.readFile(path.join(destination, 'posts', 'new.mdx'), 'utf8'), 'CMS addition');
    await assert.rejects(fs.access(path.join(destination, 'posts', 'event.mdx')), { code: 'ENOENT' });
    await assert.rejects(fs.access(path.join(destination, 'new-default.yaml')), { code: 'ENOENT' });
});

test('Recovered storage is preserved without importing bundled defaults', async () => {
    // Arrange
    await fs.mkdir(destination);
    await fs.writeFile(path.join(destination, 'recovered.webp'), 'Recovered upload');
    // Act
    await initializeDirectory(source, destination);
    // Assert
    assert.deepEqual(await fs.readdir(destination), ['recovered.webp']);
    assert.equal(await fs.readFile(path.join(destination, 'recovered.webp'), 'utf8'), 'Recovered upload');
});
