import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { getRepoRoot, loadManifest } from './toolchain-lib.mjs';

const repoRoot = getRepoRoot(path.dirname(fileURLToPath(import.meta.url)));

let _manifest;
function manifest() {
  if (!_manifest) _manifest = loadManifest(repoRoot);
  return _manifest;
}

/** episodic-memory project filter — from `.cursor/toolchain.manifest.json` or EPISODIC_PROJECT_ID. */
export function getEpisodicProjectId() {
  return manifest().episodic_project_id;
}

/** @deprecated use getEpisodicProjectId() */
export const EPISODIC_PROJECT_ID = getEpisodicProjectId();

function findLatestVersion(basePath) {
  if (!fs.existsSync(basePath)) return null;
  const versions = fs.readdirSync(basePath)
    .filter(dir => /^\d+\.\d+\.\d+$/.test(dir))
    .sort((a, b) => {
      const pa = a.split('.').map(Number);
      const pb = b.split('.').map(Number);
      return (pb[0] - pa[0]) || (pb[1] - pa[1]) || (pb[2] - pa[2]);
    });
  return versions.length > 0 ? versions[0] : null;
}

const episodicBase = path.join(
  process.env.USERPROFILE || process.env.HOME || '',
  '.claude', 'plugins', 'cache', 'superpowers-marketplace', 'episodic-memory',
);
const latestVersion = findLatestVersion(episodicBase);

/** Plugin CLI (sync/stats/index). Override with EPISODIC_MEMORY_CLI env. */
export const EPISODIC_CLI =
  process.env.EPISODIC_MEMORY_CLI ||
  (latestVersion ? path.join(episodicBase, latestVersion, 'cli', 'episodic-memory.js') : null);

export const SYNC_STATUS_PATH = path.join(repoRoot, '.cursor', 'episodic', 'sync-status.json');
export const SYNC_LOG_DIR = path.join(repoRoot, '.cursor', 'logs');
export const SEARCH_TEMPLATES_PATH = path.join(repoRoot, '.cursor', 'episodic', 'search-templates.yaml');

export const REPO_ROOT = repoRoot;
export { manifest };
