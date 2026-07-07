import type {
  AttachmentsReadyPayload,
  ChatStreamAction,
  GateErrorPayload,
  GateFailedPayload,
  GatePassedPayload,
  IdentifiedElement,
  MissingElement,
  SemanticFitnessResult,
} from '../types/gate';

function pick<T>(obj: any, ...keys: string[]): T | undefined {
  if (!obj) return undefined;
  for (const k of keys) {
    if (obj[k] !== undefined && obj[k] !== null) return obj[k];
  }
  return undefined;
}

export function normalizeIdentified(raw: any): IdentifiedElement {
  return {
    category: pick(raw, 'category', 'Category') ?? '',
    description: pick(raw, 'description', 'Description') ?? '',
    evidence: pick(raw, 'evidence', 'Evidence'),
  };
}

export function normalizeMissing(raw: any): MissingElement {
  return {
    category: pick(raw, 'category', 'Category') ?? '',
    description: pick(raw, 'description', 'Description') ?? '',
    severity: pick(raw, 'severity', 'Severity') ?? 'warning',
    howToFix: pick(raw, 'howToFix', 'HowToFix') ?? '',
  };
}

export function normalizeSemanticFitness(raw: any): SemanticFitnessResult | null {
  if (!raw) return null;
  const sf = raw.semanticFitness ?? raw.SemanticFitness ?? raw;
  const identified = (pick<any[]>(sf, 'identified', 'Identified') ?? []).map(normalizeIdentified);
  const missing = (pick<any[]>(sf, 'missing', 'Missing') ?? []).map(normalizeMissing);
  return {
    passed: pick(sf, 'passed', 'Passed') ?? false,
    score: pick(sf, 'score', 'Score') ?? 0,
    level: pick(sf, 'level', 'Level') ?? 'insufficient',
    identified,
    missing,
    nextStepGuidance: pick(sf, 'nextStepGuidance', 'NextStepGuidance'),
  };
}

export function parseGatePayload<T>(dataField: unknown): T | null {
  if (!dataField) return null;
  if (typeof dataField === 'string') {
    try {
      return JSON.parse(dataField) as T;
    } catch {
      return null;
    }
  }
  return dataField as T;
}

function escapeCell(text: string): string {
  return (text ?? '').replace(/\|/g, '\\|').replace(/\n/g, ' ');
}

function identifiedTable(items: IdentifiedElement[]): string {
  if (!items.length) return '';
  const rows = items.map(i => `| ${escapeCell(i.category)} | ${escapeCell(i.description)} | ${escapeCell(i.evidence ?? '—')} |`).join('\n');
  return `### ✅ 已识别的要素\n\n| 类别 | 描述 | 依据 |\n| --- | --- | --- |\n${rows}\n`;
}

function missingTable(items: MissingElement[]): string {
  if (!items.length) return '';
  const rows = items
    .map(m => {
      const sev = m.severity === 'critical' ? '**严重**' : '提示';
      return `| ${escapeCell(m.category)} | ${escapeCell(m.description)} | ${sev} | ${escapeCell(m.howToFix)} |`;
    })
    .join('\n');
  return `### ❌ 需要补充的关键要素\n\n| 类别 | 问题 | 严重程度 | 如何补充 |\n| --- | --- | --- | --- |\n${rows}\n`;
}

function levelLabel(level: string, score: number): string {
  if (level === 'partial') return '部分合格';
  if (level === 'sufficient') return '合格';
  return score >= 50 ? '接近合格' : '不合格';
}

export function buildGateFailedMarkdown(payload: GateFailedPayload, sf: SemanticFitnessResult): string {
  const lines: string[] = [
    '## ⚠️ 需求材料尚未达到进入流水线的标准',
    '',
    'SA 门控要求：原始需求必须能解析为**至少一个合格的业务事件**（非单纯增删改查），并具备可推断的**角色**与**数据实体**。',
    '',
  ];
  if (payload.reason) lines.push(`> ${payload.reason}`, '');
  if (payload.hint) lines.push(`> 💡 ${payload.hint}`, '');
  lines.push(`**评估评分：${sf.score}/100** · 等级：${levelLabel(sf.level, sf.score)}`, '');
  if (sf.identified.length) lines.push(identifiedTable(sf.identified), '');
  if (sf.missing.length) lines.push(missingTable(sf.missing), '');
  if (sf.nextStepGuidance) {
    lines.push('### 📌 下一步建议', '', sf.nextStepGuidance, '');
  }
  if (payload.warnings?.length) {
    lines.push('### 附件/格式提示', '', ...payload.warnings.map(w => `- ${w}`), '');
  }
  lines.push('---', '', '请在下方输入框**补充业务场景描述**后重新发送；也可点击消息中的快捷按钮填入参考范例。');
  return lines.join('\n');
}

export function buildGatePassedMarkdown(payload: GatePassedPayload, sf: SemanticFitnessResult): string {
  const lines: string[] = [
    '## ✅ 需求材料评估通过',
    '',
    `**评估评分：${sf.score}/100** · 已识别 **${
      sf.identified.filter(i => i.category === '业务事件').length || sf.identified.length
    }** 项关键要素，可以进入流水线。`,
    '',
  ];
  if (sf.identified.length) lines.push(identifiedTable(sf.identified), '');
  if (payload.warnings?.length) {
    lines.push('### 提示', '', ...payload.warnings.map(w => `- ${w}`), '');
  }
  lines.push('---', '', '系统将自动启动 **PM 骨架提取**；完成后请在对话流中确认 IR-0 业务事件骨架。');
  return lines.join('\n');
}

export function buildGateErrorMarkdown(payload: GateErrorPayload): string {
  return [
    '## ❌ 需求评估异常',
    '',
    payload.message ?? '需求评估过程中发生异常，请稍后重试。',
    '',
    payload.errorCode ? `错误代码：\`${payload.errorCode}\`` : '',
    '',
    '---',
    '',
    '请检查网络连接后，在下方重新发送需求；若持续失败请联系管理员。',
  ]
    .filter(Boolean)
    .join('\n');
}

export const REQUIREMENT_WRITING_GUIDE = `请尽量包含：
1. **业务事件** — 谁在什么场景下做什么（如「工人完成工序后提交报工」）
2. **参与角色** — 工人、主管、质检员等
3. **管理的数据** — 工单、报工记录、设备等`;

export const GATE_EXAMPLE_PROMPT =
  '我们是汽车零部件工厂，需要一个报工管理系统。工人完成工序后扫描工单号，输入完成数量和不良品数量；车间主任审核报工记录，质检员处理不良品。系统需管理工单、工序、报工记录、员工、设备。';

export function gateFailedActions(): ChatStreamAction[] {
  return [
    { label: '填入报工系统范例', type: 'primary', action: 'fill_prompt', payload: GATE_EXAMPLE_PROMPT },
    { label: '查看撰写要求', type: 'default', action: 'fill_prompt', payload: REQUIREMENT_WRITING_GUIDE },
    { label: '聚焦输入框', type: 'link', action: 'focus_input' },
  ];
}

export function gateErrorActions(): ChatStreamAction[] {
  return [{ label: '重新发送', type: 'primary', action: 'focus_input' }];
}

export function buildAttachmentsReadyMarkdown(payload: AttachmentsReadyPayload): string {
  const lines: string[] = ['### 📎 附件入库结果', ''];
  const rawItems = (payload as any).items ?? (payload as any).Items ?? payload.items ?? [];
  const items = rawItems.map((it: any) => ({
    fileName: it.fileName ?? it.FileName ?? '—',
    processStatus: it.processStatus ?? it.ProcessStatus ?? 0,
    extractedLength: it.extractedLength ?? it.ExtractedLength ?? 0,
    error: it.error ?? it.Error,
  }));
  if (items.length === 0) {
    lines.push('未登记新附件。', '');
    return lines.join('\n');
  }
  lines.push('| 文件 | 状态 | 提取字数 | 备注 |', '| --- | --- | --- | --- |');
  for (const it of items) {
    const status = it.processStatus === 2 ? '✅ 已解析' : it.processStatus === 3 ? '❌ 失败' : '⏳ 处理中';
    const note = it.error ? escapeCell(it.error) : '—';
    lines.push(`| ${escapeCell(it.fileName ?? '—')} | ${status} | ${it.extractedLength ?? 0} | ${note} |`);
  }
  if (payload.warnings?.length) {
    lines.push('', '**提示：**', ...payload.warnings.map(w => `- ${w}`));
  }
  lines.push('');
  return lines.join('\n');
}

/** 将整段 Markdown 以打字机效果写入 msg.content */
export async function streamTextToMessage(
  msg: { content: string },
  fullText: string,
  opts?: { chunkSize?: number; delayMs?: number; onChunk?: () => void },
): Promise<void> {
  const chunkSize = opts?.chunkSize ?? 16;
  const delayMs = opts?.delayMs ?? 12;
  msg.content = '';
  for (let i = 0; i < fullText.length; i += chunkSize) {
    msg.content += fullText.slice(i, i + chunkSize);
    opts?.onChunk?.();
    if (delayMs > 0) await new Promise(r => setTimeout(r, delayMs));
  }
}
