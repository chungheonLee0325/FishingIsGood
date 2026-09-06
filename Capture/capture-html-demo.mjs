import { createServer } from 'node:http';
import { mkdir, readFile, rename, rm, stat, writeFile } from 'node:fs/promises';
import { dirname, extname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const here = dirname(fileURLToPath(import.meta.url));
const repository = resolve(here, '..');
const htmlFile = join(repository, 'HTML', 'index.html');
const outputDirectory = resolve(process.argv[2] || join(repository, '.validation', 'demo-video'));
const outputVideo = join(outputDirectory, 'FishingIsGood_HTML_Demo.webm');
const outputReport = join(outputDirectory, 'FishingIsGood_HTML_Demo.txt');
const browserDirectory = process.env.PLAYWRIGHT_BROWSERS_PATH || '(Playwright default)';
const consoleErrors = [];

await mkdir(outputDirectory, { recursive: true });
await rm(outputVideo, { force: true });

const server = createServer(async (request, response) => {
  if (request.url !== '/' && request.url !== '/index.html') {
    response.writeHead(404);
    response.end('Not found');
    return;
  }

  const body = await readFile(htmlFile);
  response.writeHead(200, {
    'Content-Type': 'text/html; charset=utf-8',
    'Content-Length': body.length,
    'Cache-Control': 'no-store'
  });
  response.end(body);
});

await new Promise((resolveListen, rejectListen) => {
  server.once('error', rejectListen);
  server.listen(0, '127.0.0.1', resolveListen);
});

const address = server.address();
const url = `http://127.0.0.1:${address.port}/`;
const startedAt = Date.now();
let browser;
let context;
let page;
let temporaryVideo;
let status = 'failed';
let tallyBefore = '';
let tallyAfter = '';

try {
  browser = await chromium.launch({ headless: true });
  context = await browser.newContext({
    viewport: { width: 1600, height: 900 },
    recordVideo: {
      dir: outputDirectory,
      size: { width: 1600, height: 900 }
    }
  });
  page = await context.newPage();
  page.on('console', message => {
    if (message.type() === 'error') consoleErrors.push(message.text());
  });
  page.on('pageerror', error => consoleErrors.push(error.message));

  await page.goto(url, { waitUntil: 'load' });
  await page.locator('#cv').waitFor({ state: 'visible' });
  await page.waitForTimeout(1800);

  // Compare the visible before, rejected, and adopted movement settings.
  await page.locator('#pbar button').nth(0).click();
  await page.waitForTimeout(4200);
  await page.locator('#pbar button').nth(2).click();
  await page.waitForTimeout(4200);
  await page.locator('#pbar button').nth(4).click();
  await page.waitForTimeout(5200);

  tallyBefore = (await page.locator('#tally').innerText()).replace(/\s+/g, ' ').trim();
  const canvas = await page.locator('#cv').boundingBox();
  if (!canvas) throw new Error('The gameplay canvas has no visible bounds.');

  // A real pointer event places the bobber. Fish response and any catch remain runtime behavior.
  await page.mouse.click(canvas.x + canvas.width * 0.53, canvas.y + canvas.height * 0.56);
  await page.waitForTimeout(12000);
  tallyAfter = (await page.locator('#tally').innerText()).replace(/\s+/g, ' ').trim();
  status = consoleErrors.length === 0 ? 'passed' : 'failed';

  const video = page.video();
  await context.close();
  context = null;
  temporaryVideo = await video.path();
  if (resolve(temporaryVideo) !== resolve(outputVideo)) await rename(temporaryVideo, outputVideo);
} finally {
  if (context) await context.close().catch(() => {});
  if (browser) await browser.close().catch(() => {});
  await new Promise(resolveClose => server.close(resolveClose));

  let bytes = 0;
  try { bytes = (await stat(outputVideo)).size; } catch {}
  const report = [
    'FishingIsGood HTML comparison demo capture',
    `capturedAt=${new Date().toISOString()}`,
    `source=${htmlFile.replaceAll('\\', '/')}`,
    `url=${url}`,
    'resolution=1600x900',
    'sequence=preset 1 BEFORE -> preset 3 BUG -> preset 5 BEST -> canvas cast',
    `durationSeconds=${((Date.now() - startedAt) / 1000).toFixed(3)}`,
    `tallyBefore=${tallyBefore}`,
    `tallyAfter=${tallyAfter}`,
    `playwrightBrowsers=${browserDirectory.replaceAll('\\', '/')}`,
    `video=${outputVideo.replaceAll('\\', '/')};bytes=${bytes}`,
    `consoleErrors=${consoleErrors.length}`,
    ...consoleErrors.map(error => `consoleError=${error.replaceAll('\n', ' ')}`),
    `passed=${status === 'passed' && bytes > 0}`
  ];
  await writeFile(outputReport, report.join('\n') + '\n', 'utf8');
}

if (status !== 'passed') process.exitCode = 1;
