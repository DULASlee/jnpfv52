import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));

/** Resolve repository root (parent of scripts/). */
export function getRepoRoot(fromDir = scriptsDir) {
  return path.resolve(fromDir, '..');
}

/** Load `.cursor/toolchain.manifest.json` with env overrides. */
export function loadManifest(repoRoot = getRepoRoot()) {
  const manifestPath = path.join(repoRoot, '.cursor', 'toolchain.manifest.json');
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Missing toolchain manifest: ${manifestPath}`);
  }
  let text = fs.readFileSync(manifestPath, 'utf8');
  if (text.charCodeAt(0) === 0xfeff) text = text.slice(1);
  const raw = JSON.parse(text);
  return {
    ...raw,
    episodic_project_id:
      process.env.EPISODIC_PROJECT_ID || raw.episodic_project_id,
    project_slug: process.env.TOOLCHAIN_PROJECT_SLUG || raw.project_slug,
    repoRoot,
    manifestPath,
  };
}

export function episodicSearchTemplatesPath(repoRoot) {
  return path.join(repoRoot, '.cursor', 'episodic', 'search-templates.yaml');
}
