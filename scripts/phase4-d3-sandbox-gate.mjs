#!/usr/bin/env node
/**
 * D3-GATE — leave-simple 渲染产物 sandbox dotnet build
 * 等价于: cd backend/tests/JNPF.Tests.PhaseB && dotnet run -- sandbox-gate
 */
import { buildPhaseB, runPhaseBCli } from './lib/dotnet-build.mjs';

const NO_BUILD = process.argv.includes('--no-build');

if (!NO_BUILD) {
  console.log('[D3-GATE] building PhaseB test host...');
  const build = buildPhaseB({ inherit: true, retries: 1 });
  if (!build.pass) process.exit(build.exitCode ?? 1);
} else {
  console.log('[D3-GATE] skip build (--no-build, caller already built PhaseB)');
}

console.log('[D3-GATE] running sandbox-gate...');
const gate = runPhaseBCli(['sandbox-gate'], { inherit: true });

if (gate.status === 0) {
  console.log('[D3-GATE] PASS — leave-simple sandbox dotnet build exit 0');
}
process.exit(gate.status ?? 1);
