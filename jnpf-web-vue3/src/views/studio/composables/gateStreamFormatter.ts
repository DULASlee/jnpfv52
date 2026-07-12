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

/** 强制转成可读字符串，避免 `[object Object]` / `arr+undefined` 拼进 Markdown */
function asText(value: unknown, fallback = ''): string {
  if (value === undefined || value === null) return fallback;
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  if (Array.isArray(value)) {
    return value
      .map(v => asText(v))
      .filter(Boolean)
      .join('；');
  }
  if (typeof value === 'object') {
    const o = value as Record<string, unknown>;
    const nested =
      o.description ?? o.Description ?? o.text ?? o.Text ?? o.message ?? o.Message ?? o.howToFix ?? o.HowToFix;
    if (nested !== undefined && nested !== value) return asText(nested, fallback);
    try {
      return JSON.stringify(value);
    } catch {
      return fallback;
    }
  }
  return fallback;
}

function asArray(value: unknown): any[] {
  return Array.isArray(value) ? value : [];
}

export function normalizeIdentified(raw: any): IdentifiedElement {
  return {
    category: asText(pick(raw, 'category', 'Category')),
    description: asText(pick(raw, 'description', 'Description')),
    evidence: asText(pick(raw, 'evidence', 'Evidence')) || undefined,
  };
}

export function normalizeMissing(raw: any): MissingElement {
  return {
    category: asText(pick(raw, 'category', 'Category')),
    description: asText(pick(raw, 'description', 'Description')),
    severity: asText(pick(raw, 'severity', 'Severity'), 'warning') || 'warning',
    howToFix: asText(pick(raw, 'howToFix', 'HowToFix')),
  };
}

export function normalizeSemanticFitness(raw: any): SemanticFitnessResult | null {
  if (!raw) return null;
  const sf = raw.semanticFitness ?? raw.SemanticFitness ?? raw;
  const identified = asArray(pick(sf, 'identified', 'Identified')).map(normalizeIdentified);
  const missing = asArray(pick(sf, 'missing', 'Missing')).map(normalizeMissing);
  const levelRaw = pick(sf, 'level', 'Level');
  const level =
    typeof levelRaw === 'number'
      ? ['sufficient', 'partial', 'insufficient'][levelRaw] ?? 'insufficient'
      : asText(levelRaw, 'insufficient') || 'insufficient';
  return {
    passed: Boolean(pick(sf, 'passed', 'Passed') ?? false),
    score: Number(pick(sf, 'score', 'Score') ?? 0) || 0,
    level,
    identified,
    missing,
    nextStepGuidance: asText(pick(sf, 'nextStepGuidance', 'NextStepGuidance')) || undefined,
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

function escapeCell(text: unknown): string {
  return asText(text).replace(/\|/g, '\\|').replace(/\n/g, ' ');
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
      const category = escapeCell(m?.category);
      const description = escapeCell(m?.description);
      const howToFix = escapeCell(m?.howToFix);
      // 防御：若仍出现裸对象字符串化，强制 JSON，避免页面出现 [object Object]
      const safeCat = category.includes('[object Object]') ? escapeCell(JSON.stringify(m?.category ?? '')) : category;
      const safeDesc = description.includes('[object Object]') ? escapeCell(JSON.stringify(m?.description ?? '')) : description;
      const safeFix = howToFix.includes('[object Object]') ? escapeCell(JSON.stringify(m?.howToFix ?? '')) : howToFix;
      const sev = m.severity === 'critical' ? '**严重**' : '提示';
      return `| ${safeCat || '—'} | ${safeDesc || '—'} | ${sev} | ${safeFix || '—'} |`;
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
  const reason = asText(payload.reason);
  const hint = asText(payload.hint);
  // GATE_LLM_ERR 等系统失败：reason=BuildSummary 会与下方表格重复，只保留简短 hint
  const isSystemGateErr = sf.missing.some(
    m => asText(m.description).includes('GATE_') || asText(m.category) === '系统',
  );
  if (!isSystemGateErr && reason) lines.push(`> ${reason.replace(/\n/g, '\n> ')}`, '');
  if (hint) lines.push(`> 💡 ${hint.replace(/\n/g, '\n> ')}`, '');
  lines.push(`**评估评分：${sf.score}/100** · 等级：${levelLabel(asText(sf.level), Number(sf.score) || 0)}`, '');
  if (sf.identified.length) lines.push(identifiedTable(sf.identified), '');
  if (sf.missing.length) lines.push(missingTable(sf.missing), '');
  const guidance = asText(sf.nextStepGuidance);
  if (guidance && guidance !== hint) {
    lines.push('### 📌 下一步建议', '', guidance, '');
  }
  const warnings = asArray(payload.warnings).map(w => asText(w)).filter(Boolean);
  if (warnings.length) {
    lines.push('### 附件/格式提示', '', ...warnings.map(w => `- ${w}`), '');
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
1. **业务事件** — 谁在什么场景下做什么（如「员工刷卡取用更衣柜后，系统记录开柜事件」）
2. **参与角色** — 员工、管理员、运维人员等
3. **管理的数据** — 柜体、隔口、授权、开柜记录、告警等`;

/** 结构化需求范例：事件 + 角色 + 实体齐全，供门控失败后一键填入 */
export const GATE_EXAMPLE_PROMPT =
  '我们公司需要一套智能更衣柜管理系统。员工刷卡或扫码后打开分配的柜门存取衣物；管理员可为员工分配/回收柜位、查看使用状态；系统在超时未关柜、异常开柜时告警。需要管理：员工、柜体、柜门、授权关系、开柜记录、告警记录。';

export function gateFailedActions(): ChatStreamAction[] {
  return [
    { label: '填入需求范例', type: 'primary', action: 'fill_prompt', payload: GATE_EXAMPLE_PROMPT },
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

/** 将整段 Markdown 以打字机效果写入 msg.content（大段文本加大块、降频，减轻重渲染抖动） */
export async function streamTextToMessage(
  msg: { content: string },
  fullText: string,
  opts?: { chunkSize?: number; delayMs?: number; onChunk?: () => void },
): Promise<void> {
  // 长文（门控报告等）用更大块；短文保留轻微打字感
  const long = fullText.length > 800;
  const chunkSize = opts?.chunkSize ?? (long ? 96 : 32);
  const delayMs = opts?.delayMs ?? (long ? 0 : 8);
  msg.content = '';
  for (let i = 0; i < fullText.length; i += chunkSize) {
    msg.content += fullText.slice(i, i + chunkSize);
    opts?.onChunk?.();
    if (delayMs > 0) await new Promise(r => setTimeout(r, delayMs));
    else if (long && i % (chunkSize * 4) === 0) {
      // 每约 4 块让出一帧，避免同步堵死主线程
      await new Promise<void>(r => requestAnimationFrame(() => r()));
    }
  }
}
