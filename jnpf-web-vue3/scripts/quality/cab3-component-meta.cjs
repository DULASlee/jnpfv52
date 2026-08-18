/**
 * Component cabinet — vue-component-meta for ALL src .vue (default), plus Knip unused .vue list.
 *
 * Usage:
 *   node scripts/quality/cab3-component-meta.cjs
 *   node scripts/quality/cab3-component-meta.cjs --sample
 *   node scripts/quality/cab3-component-meta.cjs path/to/File.vue
 */
const fs = require('fs');
const path = require('path');
const { createChecker } = require('vue-component-meta');

const root = path.resolve(__dirname, '../..');
const tsconfig = path.join(root, 'tsconfig.json');
const outDir = path.resolve(root, '../.claude/evidence/frontend-ct');
fs.mkdirSync(outDir, { recursive: true });

const sampleDefaults = [
  'src/views/studio/components/AiChatPanel.vue',
  'src/components/Jnpf/InputTable/src/InputTable.vue',
  'src/components/Form/src/BasicForm.vue',
];

function walkVue(dir, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      if (ent.name === 'node_modules' || ent.name === '__tests__' || ent.name === 'compiler' || ent.name === 'e2e') {
        continue;
      }
      walkVue(full, acc);
    } else if (ent.name.endsWith('.vue')) {
      acc.push(full);
    }
  }
  return acc;
}

const argv = process.argv.slice(2);
const sampleOnly = argv.includes('--sample');
const explicit = argv.filter((a) => !a.startsWith('--'));

let targets;
if (explicit.length) {
  targets = explicit.map((f) => (path.isAbsolute(f) ? f : path.join(root, f)));
} else if (sampleOnly) {
  targets = sampleDefaults.map((f) => path.join(root, f));
} else {
  targets = walkVue(path.join(root, 'src'));
}

console.log(JSON.stringify({ mode: explicit.length ? 'explicit' : sampleOnly ? 'sample' : 'full-src', vueFiles: targets.length }));

const checker = createChecker(tsconfig, {
  forceUseTs: true,
  schema: { ignore: ['attributes'] },
  printer: { newLine: 1 },
});

const report = {
  generatedAt: new Date().toISOString(),
  mode: explicit.length ? 'explicit' : sampleOnly ? 'sample' : 'full-src',
  tsconfig,
  total: targets.length,
  ok: 0,
  failed: 0,
  components: [],
};

for (let i = 0; i < targets.length; i++) {
  const file = targets[i];
  if ((i + 1) % 50 === 0 || i === 0 || i === targets.length - 1) {
    process.stdout.write(`meta ${i + 1}/${targets.length}\n`);
  }
  if (!fs.existsSync(file)) {
    report.failed++;
    report.components.push({ file: path.relative(root, file).replace(/\\/g, '/'), error: 'not found' });
    continue;
  }
  try {
    const meta = checker.getComponentMeta(file);
    const props = (meta.props || []).map((p) => ({
      name: p.name,
      required: !!p.required,
      type: typeof p.type === 'string' ? p.type.slice(0, 200) : String(p.type).slice(0, 200),
    }));
    const events = (meta.events || []).map((e) => ({
      name: e.name,
      type: typeof e.type === 'string' ? e.type.slice(0, 120) : String(e.type).slice(0, 120),
    }));
    const slots = (meta.slots || []).map((s) => s.name);
    const exposed = (meta.exposed || []).map((e) => e.name);
    report.ok++;
    report.components.push({
      file: path.relative(root, file).replace(/\\/g, '/'),
      propsCount: props.length,
      eventsCount: events.length,
      slotsCount: slots.length,
      exposedCount: exposed.length,
      props,
      events,
      slots,
      exposed,
    });
  } catch (err) {
    report.failed++;
    report.components.push({
      file: path.relative(root, file).replace(/\\/g, '/'),
      error: String(err && err.message ? err.message : err).slice(0, 300),
    });
  }
}

const outFile = path.join(outDir, 'cab3-component-meta.json');
fs.writeFileSync(outFile, JSON.stringify(report), 'utf8');

const ranked = report.components
  .filter((c) => !c.error)
  .map((c) => ({
    file: c.file,
    propsCount: c.propsCount,
    eventsCount: c.eventsCount,
    slotsCount: c.slotsCount,
  }))
  .sort((a, b) => b.propsCount - a.propsCount);

const summary = {
  generatedAt: report.generatedAt,
  mode: report.mode,
  total: report.total,
  ok: report.ok,
  failed: report.failed,
  topProps50: ranked.slice(0, 50),
  failedFiles: report.components.filter((c) => c.error).map((c) => ({ file: c.file, error: c.error })),
};
fs.writeFileSync(path.join(outDir, 'cab3-component-meta-summary.json'), JSON.stringify(summary, null, 2));

// Full unused .vue list from knip
const knipPath = path.join(outDir, 'cab1-knip.txt');
let unusedVue = [];
if (fs.existsSync(knipPath)) {
  unusedVue = fs
    .readFileSync(knipPath, 'utf8')
    .split(/\r?\n/)
    .filter((l) => l.includes('.vue:'))
    .map((l) => l.split(':')[0].trim())
    .filter(Boolean);
  fs.writeFileSync(
    path.join(outDir, 'cab3-unused-vue-full.json'),
    JSON.stringify(
      {
        note: 'From Knip unused files; many are dynamic-route false positives — verify before delete',
        count: unusedVue.length,
        files: unusedVue,
      },
      null,
      2,
    ),
    'utf8',
  );
  // keep sample alias for older docs
  fs.writeFileSync(
    path.join(outDir, 'cab3-unused-vue-sample.json'),
    JSON.stringify({ note: 'see cab3-unused-vue-full.json', sampleCount: Math.min(50, unusedVue.length), sample: unusedVue.slice(0, 50) }, null, 2),
  );
}

console.log(
  JSON.stringify(
    {
      outFile,
      total: report.total,
      ok: report.ok,
      failed: report.failed,
      unusedVue: unusedVue.length,
      topProps5: ranked.slice(0, 5),
    },
    null,
    2,
  ),
);
