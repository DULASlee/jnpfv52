/**
 * Fuite scenario for JNPF PC — custom idle (no networkidle hang on Vite SPA).
 * Prefer URL: http://127.0.0.1:3100/index.html
 */
async function setup(page) {
  page.setDefaultNavigationTimeout(120000);
  page.setDefaultTimeout(120000);
}

async function waitForIdle() {
  await new Promise((r) => setTimeout(r, 1500));
}

async function iteration(page) {
  await page.reload({ waitUntil: 'domcontentloaded', timeout: 120000 });
  await waitForIdle();
}

module.exports = { setup, iteration, waitForIdle };
