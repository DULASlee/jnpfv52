#!/usr/bin/env node
import { apiRequest } from './lib/jnpf-auth.mjs';

const [basePipelineId, nextPipelineId] = process.argv.slice(2);

function usage() {
  console.log('用法: node scripts/req-evolution-accel.mjs <baselinePipelineId> <secondPipelineId>');
}

function unwrap(result) {
  const payload = result.json ?? {};
  return payload.data ?? payload;
}

function parsePayload(evt) {
  try {
    return typeof evt.payload === 'string' ? JSON.parse(evt.payload) : (evt.payload ?? {});
  } catch {
    return {};
  }
}

function summarize(pipelineId, events) {
  const pmReviews = events
    .filter(e => e.eventType === 'RequirementSpecPmReviewed')
    .map(e => parsePayload(e))
    .filter(p => typeof p.score === 'number');
  const latestReview = pmReviews.at(-1);
  const proposed = events.filter(e => e.eventType === 'RequirementAmendmentProposed').length;
  const applied = events.filter(e => e.eventType === 'RequirementAmendmentApplied').length;
  const forced = events
    .filter(e => e.eventType === 'StageConfirmed')
    .map(e => parsePayload(e))
    .some(p => p.forceConfirm === true);

  return {
    pipelineId: String(pipelineId),
    eventCount: events.length,
    amendProposedCount: proposed,
    amendAppliedCount: applied,
    latestPmScore: latestReview?.score ?? null,
    latestPmVerdict: latestReview?.verdict ?? null,
    forceConfirmed: forced,
  };
}

async function fetchEvents(pipelineId) {
  const result = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`);
  if (!result.ok) {
    throw new Error(`读取 pipeline ${pipelineId} events 失败: HTTP ${result.status} ${JSON.stringify(result.json ?? result.text)}`);
  }
  return unwrap(result);
}

async function main() {
  if (!basePipelineId || !nextPipelineId) {
    usage();
    process.exit(1);
  }

  const [baseEvents, nextEvents] = await Promise.all([
    fetchEvents(basePipelineId),
    fetchEvents(nextPipelineId),
  ]);
  const baseline = summarize(basePipelineId, baseEvents);
  const second = summarize(nextPipelineId, nextEvents);
  const comparison = {
    amendAppliedDelta: second.amendAppliedCount - baseline.amendAppliedCount,
    pmScoreDelta: second.latestPmScore != null && baseline.latestPmScore != null
      ? second.latestPmScore - baseline.latestPmScore
      : null,
  };

  console.log(JSON.stringify({ baseline, second, comparison }, null, 2));
}

main().catch(err => {
  console.error(err.stack || err.message || err);
  process.exit(1);
});
