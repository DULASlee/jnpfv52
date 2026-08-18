/**
 * 重进会话时把 DB 精简消息 + 附件/交付物 水合成主聊天可见结构。
 * 目标：用户看到的主对话与离开前一致（门控文案、附件状态、交付物下载），而非原始 JSON / 空白。
 */
import {
  buildAttachmentsReadyMarkdown,
  buildGateFailedMarkdown,
  buildGatePassedMarkdown,
  gateFailedActions,
  normalizeSemanticFitness,
} from './gateStreamFormatter';
import type { AttachmentsReadyPayload, ChatStreamAction } from '../types/gate';

export type HydratedChatMessage = {
  id: string | number;
  role: string;
  content: string;
  thinking: string;
  thinkingCollapsed: boolean;
  strategies: any[];
  document: null | {
    name: string;
    previewUrl?: string;
    downloadPdfUrl?: string;
    downloadWordUrl?: string;
    relativePath?: string;
  };
  deliverableLinks: Array<{ name: string; relativePath: string }>;
  ir: any;
  actions: ChatStreamAction[];
  stageConfirmable: boolean;
  stageConfirmed: boolean;
  clarification: any;
  amendmentProposal?: any;
};

function pickName(d: any): string {
  return String(d?.fileName ?? d?.FileName ?? d?.relativePath ?? d?.RelativePath ?? '交付物');
}

function pickPath(d: any): string {
  return String(d?.relativePath ?? d?.RelativePath ?? d?.fileName ?? d?.FileName ?? '');
}

function preferDeliverables(items: any[]): Array<{ name: string; relativePath: string }> {
  const ranked = [...items]
    .map(d => ({ name: pickName(d), relativePath: pickPath(d) }))
    .filter(d => d.relativePath);
  const order = ['02-requirement-spec', '01-skeleton', '00-merged-requirement', '00-gate-report'];
  ranked.sort((a, b) => {
    const ai = order.findIndex(k => a.relativePath.includes(k) || a.name.includes(k));
    const bi = order.findIndex(k => b.relativePath.includes(k) || b.name.includes(k));
    return (ai === -1 ? 99 : ai) - (bi === -1 ? 99 : bi);
  });
  // 主聊天最多挂 4 个，避免刷屏
  return ranked.slice(0, 4);
}

function buildAttachmentThinking(attachments: any[]): string {
  if (!attachments?.length) return '';
  const payload: AttachmentsReadyPayload = {
    processed: attachments.filter(a => Number(a.processStatus ?? a.ProcessStatus) === 2).length,
    failed: attachments.filter(a => Number(a.processStatus ?? a.ProcessStatus) === 3).length,
    items: attachments.map(a => ({
      fileName: a.fileName ?? a.FileName,
      processStatus: Number(a.processStatus ?? a.ProcessStatus ?? 0),
      extractedLength: Number(a.extractedLength ?? a.ExtractedLength ?? 0),
      error: a.processError ?? a.ProcessError ?? a.error,
    })),
  };
  return buildAttachmentsReadyMarkdown(payload);
}

function mapBase(m: any): HydratedChatMessage {
  return {
    id: m.id ?? m.Id ?? `${Date.now()}-${Math.random()}`,
    role: m.role ?? m.Role ?? 'system',
    content: m.content ?? m.Content ?? '',
    thinking: m.thinking ?? m.Thinking ?? '',
    thinkingCollapsed: true,
    strategies: m.strategies ?? m.Strategies ?? [],
    document: m.document ?? m.Document ?? null,
    deliverableLinks: [],
    ir: m.ir ?? m.Ir ?? null,
    actions: m.actions ?? m.Actions ?? [],
    stageConfirmable: m.stageConfirmable ?? m.StageConfirmable ?? false,
    stageConfirmed: m.stageConfirmed ?? m.StageConfirmed ?? false,
    clarification: m.clarification ?? m.Clarification ?? null,
  };
}

/** 将 gate 阶段的 system JSON 还原为可读门控消息 */
function hydrateGateSystemMessage(msg: HydratedChatMessage, stage: string | undefined): HydratedChatMessage {
  if (msg.role !== 'system' || stage !== 'gate') return msg;
  const raw = (msg.content || '').trim();
  if (!raw.startsWith('{')) return msg;
  try {
    const parsed = JSON.parse(raw);
    const passed = parsed.Passed === true || parsed.passed === true;
    const failed = parsed.Passed === false || parsed.passed === false;
    const sf =
      normalizeSemanticFitness(parsed) ??
      ({
        passed,
        score: Number(parsed.Score ?? parsed.score ?? 0) || 0,
        level: passed ? 'sufficient' : 'insufficient',
        identified: [],
        missing: [],
      } as const);
    if (passed) {
      return {
        ...msg,
        role: 'assistant',
        content: buildGatePassedMarkdown(parsed, sf as any),
        thinkingCollapsed: true,
      };
    }
    if (failed) {
      return {
        ...msg,
        role: 'assistant',
        content: buildGateFailedMarkdown(parsed, sf as any),
        actions: gateFailedActions(),
        thinkingCollapsed: true,
      };
    }
  } catch {
    /* 非 JSON 则原样 */
  }
  return msg;
}

export function hydrateChatSession(opts: {
  rawMessages: any[];
  attachments: any[];
  deliverables: any[];
}): { messages: HydratedChatMessage[]; stats: Record<string, number | boolean> } {
  const attachmentThinking = buildAttachmentThinking(opts.attachments);
  const links = preferDeliverables(opts.deliverables);

  let gateTransformed = 0;
  const messages = opts.rawMessages.map(raw => {
    const stage = raw.stage ?? raw.Stage;
    let msg = mapBase(raw);
    const beforeRole = msg.role;
    msg = hydrateGateSystemMessage(msg, stage);
    if (beforeRole === 'system' && msg.role === 'assistant') gateTransformed += 1;
    return msg;
  });

  // 附件摘要挂到首条 assistant 的 thinking（可折叠）
  const firstAssistant = messages.find(m => m.role === 'assistant');
  if (firstAssistant && attachmentThinking) {
    firstAssistant.thinking = [attachmentThinking, firstAssistant.thinking].filter(Boolean).join('\n');
    firstAssistant.thinkingCollapsed = true;
  }

  // 交付物下载挂到最后一条 assistant
  const lastAssistant = [...messages].reverse().find(m => m.role === 'assistant');
  if (lastAssistant && links.length) {
    lastAssistant.deliverableLinks = links;
    const primary = links[0];
    lastAssistant.document = {
      name: primary.name,
      relativePath: primary.relativePath,
    };
  }

  return {
    messages,
    stats: {
      rawCount: opts.rawMessages.length,
      outCount: messages.length,
      gateTransformed,
      attachmentCount: opts.attachments.length,
      deliverableLinkCount: links.length,
      hasAttachmentThinking: !!attachmentThinking,
    },
  };
}
