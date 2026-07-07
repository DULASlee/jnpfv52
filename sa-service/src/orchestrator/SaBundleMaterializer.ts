/**
 * SaBundleMaterializer — 将 C# SaNineViewCompiler 产出物化到 sa_* 九表（无 LLM）。
 * 三元组 tenant_id + project_id + pipeline_id 强制写入。
 */

import {
  ISADatabase, SAContext, ScopeOutput, DFDOutput, BPMOutput, DictOutput,
  PSpecOutput, DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
} from './orchestrator-types';
import { logStep } from '../lib/structuredLogger';

export interface SaMaterializeRequest {
  tenantId: string;
  projectId: number;
  pipelineId: number;
  bundleHash?: string;
  userId?: string;
  projectSteps: Record<string, unknown>;
  eventResults: Array<{
    eventId: string;
    eventName: string;
    complexity?: string;
    steps: Record<string, unknown>;
    error?: string;
  }>;
}

export interface SaMaterializeResult {
  scopeId: number;
  dictId: number;
  eventCount: number;
  durationMs: number;
}

function asScope(raw: unknown, eventResults: SaMaterializeRequest['eventResults']): ScopeOutput {
  const o = (raw ?? {}) as Record<string, unknown>;
  const boundary = (o.systemBoundary ?? { inScope: [], outOfScope: [] }) as ScopeOutput['systemBoundary'];
  const events = Array.isArray(o.businessEvents)
    ? (o.businessEvents as ScopeOutput['businessEvents'])
    : eventResults.map((e, i) => ({
        id: i + 1,
        irEventId: e.eventId,
        name: e.eventName,
        description: e.eventName,
        complexity: (e.complexity ?? 'simple') as 'simple' | 'medium' | 'complex',
      }));
  return {
    systemBoundary: boundary,
    externalEntities: (o.externalEntities as ScopeOutput['externalEntities']) ?? [],
    businessEvents: events,
    eventCount: typeof o.eventCount === 'number' ? o.eventCount : events.length,
  };
}

function hasPayload(step: unknown): boolean {
  if (step == null) return false;
  if (typeof step === 'object' && Object.keys(step as object).length === 0) return false;
  const note = (step as Record<string, unknown>).note;
  if (typeof note === 'string' && note.includes('无独立')) return false;
  const specs = (step as Record<string, unknown>).processSpecs;
  if (Array.isArray(specs) && specs.length === 0) return false;
  const tables = (step as Record<string, unknown>).tables;
  if (Array.isArray(tables) && tables.length === 0) return false;
  return true;
}

export async function materializeBundle(
  db: ISADatabase,
  req: SaMaterializeRequest,
): Promise<SaMaterializeResult> {
  const start = Date.now();
  const ctx: SAContext = {
    tenantId: req.tenantId,
    projectId: req.projectId,
    pipelineId: req.pipelineId,
    requirementId: 0,
    requirementText: '',
    assetLevel: 'PROJECT',
    kgPatterns: [],
    domainModel: { industry: 'general', standardFields: [], standardEntities: [], standardProcesses: [] },
    previousSteps: {},
    userId: req.userId ?? 'materialize-job',
    startTime: start,
  };

  const scopeRaw = req.projectSteps['DomainModel'] ?? req.projectSteps['scope'];
  const scope = asScope(scopeRaw, req.eventResults);
  const { id: scopeId } = await db.saveScope(scope, ctx);
  ctx.scopeId = scopeId;

  const dfd = (req.projectSteps['AggregateDesign'] ?? req.projectSteps['dfd'] ?? {}) as DFDOutput;
  const { id: dfdId } = await db.saveDFD(dfd, ctx, scopeId);
  ctx.dfdId = dfdId;

  const bpm = (req.projectSteps['EventCatalog'] ?? req.projectSteps['bpm'] ?? {}) as BPMOutput;
  const { id: bpmId } = await db.saveBPM(bpm, ctx, dfdId);
  ctx.bpmId = bpmId;

  const dict = (req.projectSteps['CommandQuery'] ?? req.projectSteps['dict'] ?? {}) as DictOutput;
  const { id: dictId } = await db.saveDict(dict, ctx, dfdId, bpmId);
  ctx.dictId = dictId;

  const er = (req.projectSteps['DataModel'] ?? req.projectSteps['er'] ?? {}) as EROutput;
  await db.saveER(er, ctx, dictId);

  const sm = (req.projectSteps['UISpec'] ?? req.projectSteps['stateMachine'] ?? {}) as StateMachineOutput;
  await db.saveStateMachine(sm, ctx, dictId, bpmId);

  for (let i = 0; i < req.eventResults.length; i++) {
    const evt = req.eventResults[i];
    ctx.currentEventId = i + 1;
    ctx.assetLevel = evt.complexity === 'complex' ? 'PROCESS' : 'EVENT';

    const pspecStep = evt.steps['IntegrationPoints'];
    if (hasPayload(pspecStep)) {
      await db.savePSpec(pspecStep as PSpecOutput, ctx, dictId, bpmId);
    }

    const dtStep = evt.steps['WorkflowSpec'];
    if (hasPayload(dtStep)) {
      await db.saveDecisionTable(dtStep as DecisionTableOutput, ctx, 0, dictId);
    }

    const uiStep = evt.steps['DeliveryChecklist'];
    if (hasPayload(uiStep)) {
      await db.saveUI(uiStep as UIOutput, ctx, bpmId, dictId);
    }
  }

  const durationMs = Date.now() - start;
  logStep({
    level: 'info',
    tenantId: req.tenantId,
    projectId: String(req.projectId),
    message: `SaBundleMaterializer OK pipeline=${req.pipelineId} scope=${scopeId} events=${req.eventResults.length} ${durationMs}ms hash=${req.bundleHash ?? '-'}`,
  });

  return { scopeId, dictId, eventCount: req.eventResults.length, durationMs };
}
