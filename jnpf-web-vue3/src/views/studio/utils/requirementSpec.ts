/** S2 正式交付物路径（PipelineDeliverableService 唯一源） */
export const REQUIREMENT_SPEC_PATH = '02-requirement-spec.md';

/** RequirementDocumentRenderer 封面标题 */
export const REQUIREMENT_SPEC_TITLE = '# 需求分析规格说明书';

/** RequirementDocumentRenderer 确认 CTA 固定文本 */
export const REQUIREMENT_SPEC_CTA = '请你确认需求分析说明书';

export type RequirementSpecPhase =
  | 'absent'
  | 'refining'
  | 'rendered'
  | 'confirmed'
  | 'pmreviewed'
  | 'finalized'
  | 'superseded';

export function isRequirementSpecPath(relativePath?: string | null): boolean {
  if (!relativePath) return false;
  const normalized = relativePath.replace(/\\/g, '/');
  return normalized === REQUIREMENT_SPEC_PATH || normalized.endsWith(`/${REQUIREMENT_SPEC_PATH}`);
}

/** 校验是否为正式渲染版（非 PM raw / 中间分析产物） */
export function isFormalRequirementSpec(text: string): boolean {
  return text.includes(REQUIREMENT_SPEC_TITLE) && text.includes(REQUIREMENT_SPEC_CTA);
}

/** 解包 defHttp RESTfulResult（{ code, data }）及业务 payload */
export function unwrapStudioApi<T>(res: unknown): T {
  const root = (res as any)?.data ?? res;
  if (root && typeof root === 'object' && ('code' in root || 'Code' in root) && ('data' in root || 'Data' in root)) {
    return (root.data ?? root.Data) as T;
  }
  return root as T;
}

export interface RequirementSpecContentPayload {
  markdown?: string;
  Markdown?: string;
  rendered?: boolean;
  Rendered?: boolean;
  relativePath?: string;
  RelativePath?: string;
  contentLength?: number;
  ContentLength?: number;
  phase?: string;
  Phase?: string;
  pipelineStage?: string;
  PipelineStage?: string;
  contentHash?: string;
  ContentHash?: string;
  canUserConfirm?: boolean;
  CanUserConfirm?: boolean;
  canUserFeedback?: boolean;
  CanUserFeedback?: boolean;
  awaitingUser?: boolean;
  AwaitingUser?: boolean;
}

export function pickRequirementSpecMarkdown(payload?: RequirementSpecContentPayload | Record<string, unknown> | null): string {
  const p = payload as RequirementSpecContentPayload | null | undefined;
  return (p?.markdown ?? p?.Markdown ?? '').trim();
}

export function pickRequirementSpecPhase(payload?: RequirementSpecContentPayload | Record<string, unknown> | null): RequirementSpecPhase | '' {
  const raw = (payload as RequirementSpecContentPayload | null | undefined)?.phase
    ?? (payload as RequirementSpecContentPayload | null | undefined)?.Phase
    ?? '';
  return String(raw).trim().toLowerCase() as RequirementSpecPhase | '';
}

export function isRequirementSpecRendered(payload?: RequirementSpecContentPayload | Record<string, unknown> | null): boolean {
  const phase = pickRequirementSpecPhase(payload);
  if (phase) {
    return ['rendered', 'confirmed', 'pmreviewed', 'finalized'].includes(phase);
  }
  const p = payload as RequirementSpecContentPayload | null | undefined;
  return p?.rendered === true || p?.Rendered === true;
}

export function pickRequirementSpecPath(payload?: RequirementSpecContentPayload | null): string {
  return payload?.relativePath ?? payload?.RelativePath ?? REQUIREMENT_SPEC_PATH;
}
