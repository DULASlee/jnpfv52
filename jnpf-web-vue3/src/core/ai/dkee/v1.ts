/**
 * DKEE v1.0 — 领域知识进化引擎
 *
 * 观察人类操作 → 提炼领域模式 → 沉淀知识图谱 → 下次主动调用。
 *
 * 核心原理：
 *   1. 观察 — 追踪用户在专家模式下的创建/修改/删除操作
 *   2. 提炼 — 当同一领域累计 3+ 次操作时，提取领域模式
 *   3. 沉淀 — 将模式写入知识图谱（in-memory + localStorage持久化）
 *   4. 召回 — AI 执行前按领域关键词搜索相关模式
 *
 * @version 1.0.0
 * @module ai/dkee/v1
 */

// ============================================================
// 类型
// ============================================================

/** 领域模式 */
export interface DomainPattern {
  id: string;
  name: string;
  domain: string;
  description: string;
  source: 'ai-discovered' | 'human-created' | 'self-play' | 'hybrid';
  pattern: {
    entities: Array<{ name: string; fields: Array<{ name: string; type: string }> }>;
    rules: Array<{ name: string; condition: string; action: string }>;
    components: string[];
  };
  usageCount: number;
  successRate: number;
  version: number;
  createdAt: string;
  updatedAt: string;
}

/** 人类操作事件 */
export interface HumanAction {
  type: 'create' | 'modify' | 'delete';
  target: string;
  before: unknown;
  after: unknown;
}

/** 操作分组（按领域聚合） */
interface _ActionGroup {
  domain: string;
  actions: HumanAction[];
}

// ============================================================
// 知识图谱存储（in-memory + localStorage）
// ============================================================

const STORAGE_KEY = 'jnpf_dkee_patterns';

let knowledgeGraph: DomainPattern[] = [];

/** 从 localStorage 加载知识图谱 */
function loadFromStorage(): void {
  try {
    const storage = globalThis.localStorage;
    if (!storage) return;
    const raw = storage.getItem(STORAGE_KEY);
    if (raw) {
      knowledgeGraph = JSON.parse(raw) as DomainPattern[];
    }
  } catch {
    // Node/test environment — keep in-memory state
    return;
  }
}

/** 保存知识图谱到 localStorage */
function saveToStorage(): void {
  try {
    const storage = globalThis.localStorage;
    if (!storage) return;
    storage.setItem(STORAGE_KEY, JSON.stringify(knowledgeGraph));
  } catch {
    // localStorage 不可用（SSR/测试环境），静默失败
  }
}

// ============================================================
// 核心 API
// ============================================================

/**
 * 观察人类操作，提炼领域模式。
 *
 * 当同一领域累计 3+ 次 create 操作时，
 * 提取实体结构、业务规则和常用组件，生成 DomainPattern。
 *
 * @param humanActions - 人类操作事件数组
 * @param currentDomain - 当前业务领域
 * @returns 提炼出的领域模式（不满足条件时返回 null）
 */
export function observeAndExtract(humanActions: HumanAction[], currentDomain: string): DomainPattern | null {
  if (!currentDomain) return null;

  // 过滤当前领域的 create 操作
  const creates = humanActions.filter(a => a.type === 'create' && (a.target.includes(currentDomain) || a.target === currentDomain));

  // 需要至少 3 次操作
  if (creates.length < 3) return null;

  // 提取实体模式
  const entities = extractEntities(creates);
  if (entities.length === 0) return null;

  // 提取业务规则
  const rules = extractRules(creates);

  // 提取常用组件
  const components = extractComponents(creates);

  const pattern: DomainPattern = {
    id: `${currentDomain.replace(/\s+/g, '_')}_${Date.now()}`,
    name: `${currentDomain}领域模式`,
    domain: currentDomain,
    description: `从 ${creates.length} 次人工创建中提炼的${currentDomain}领域模式`,
    source: 'human-created',
    pattern: { entities, rules, components },
    usageCount: 1,
    successRate: 1.0,
    version: 1,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  return pattern;
}

/**
 * 将领域模式持久化到知识图谱。
 * 如果同 domain 已存在，合并并更新版本。
 */
export function persistPattern(pattern: DomainPattern): void {
  loadFromStorage();

  const existing = knowledgeGraph.findIndex(p => p.domain === pattern.domain && p.name === pattern.name);

  if (existing >= 0) {
    // 合并：保留原有使用次数，更新内容
    const old = knowledgeGraph[existing];
    pattern.usageCount = old.usageCount + 1;
    pattern.version = old.version + 1;
    pattern.source = 'hybrid';
    knowledgeGraph[existing] = pattern;
  } else {
    knowledgeGraph.push(pattern);
  }

  saveToStorage();
}

/**
 * 按领域关键词召回模式。
 *
 * @param domain - 领域关键词（如 "教育"、"制造"）
 * @returns 匹配的领域模式列表，按成功率降序
 */
export function recallPatterns(domain: string): DomainPattern[] {
  loadFromStorage();

  if (!domain) return [];

  const results = knowledgeGraph
    .filter(p => p.domain.includes(domain) || p.name.includes(domain) || p.description.includes(domain))
    .sort((a, b) => b.successRate - a.successRate || b.usageCount - a.usageCount);

  // 更新使用计数
  for (const p of results) {
    p.usageCount++;
    p.updatedAt = new Date().toISOString();
  }

  if (results.length > 0) {
    saveToStorage();
  }

  return results;
}

/**
 * 获取完整知识图谱。
 */
export function getKnowledgeGraph(): DomainPattern[] {
  loadFromStorage();
  return [...knowledgeGraph];
}

/**
 * 清空知识图谱（测试用）。
 */
export function clearKnowledgeGraph(): void {
  knowledgeGraph = [];
  saveToStorage();
}

// ============================================================
// 内部提取函数
// ============================================================

/** 从创建操作中提取实体结构 */
function extractEntities(creates: HumanAction[]): DomainPattern['pattern']['entities'] {
  const entities: DomainPattern['pattern']['entities'] = [];

  for (const action of creates) {
    const after = action.after as Record<string, unknown> | null;
    if (after && typeof after === 'object' && typeof (after as Record<string, unknown>).name === 'string') {
      const entityName = (after as Record<string, unknown>).name as string;
      const fields = extractFields(after);

      if (!entities.find(e => e.name === entityName)) {
        entities.push({ name: entityName, fields });
      } else {
        // 合并字段（去重）
        const existing = entities.find(e => e.name === entityName)!;
        for (const f of fields) {
          if (!existing.fields.find(ef => ef.name === f.name)) {
            existing.fields.push(f);
          }
        }
      }
    }
  }

  return entities;
}

/** 从实体对象中提取字段 */
function extractFields(obj: Record<string, unknown>, prefix = ''): Array<{ name: string; type: string }> {
  const fields: Array<{ name: string; type: string }> = [];

  for (const [key, value] of Object.entries(obj)) {
    if (key === 'name' || key === 'id') continue;
    const type = typeof value === 'number' ? 'number' : typeof value === 'boolean' ? 'boolean' : 'string';
    const fieldName = prefix ? `${prefix}.${key}` : key;
    if (typeof value !== 'object' || value === null) {
      fields.push({ name: fieldName, type });
    }
  }

  return fields;
}

/** 从创建操作中提取业务规则 */
function extractRules(creates: HumanAction[]): DomainPattern['pattern']['rules'] {
  const rules: DomainPattern['pattern']['rules'] = [];

  for (const action of creates) {
    const after = action.after as Record<string, unknown> | null;
    if (after?.conditions || after?.rules) {
      const rawRules = (after.rules ?? after.conditions) as Array<Record<string, unknown>>;
      if (Array.isArray(rawRules)) {
        for (const r of rawRules) {
          rules.push({
            name: (r.name as string) ?? 'unnamed',
            condition: (r.condition as string) ?? '',
            action: (r.action as string) ?? '',
          });
        }
      }
    }
  }

  return rules;
}

/** 从创建操作中提取常用组件类型 */
function extractComponents(creates: HumanAction[]): string[] {
  const components = new Set<string>();

  for (const action of creates) {
    const after = action.after as Record<string, unknown> | null;
    const fields = (after?.fields ?? after?.columns) as Array<Record<string, unknown>> | undefined;
    if (Array.isArray(fields)) {
      for (const f of fields) {
        if (typeof f.component === 'string') {
          components.add(f.component);
        }
        if (typeof f.type === 'string' && f.type.startsWith('Jnpf')) {
          components.add(f.type);
        }
      }
    }
  }

  return [...components];
}
