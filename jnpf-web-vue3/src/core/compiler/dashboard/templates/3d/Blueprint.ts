/**
 * 3D 蓝图逻辑引擎 — 事件→条件→动作链式编译器
 *
 * 将 BlueprintFlow 配置编译为 TypeScript 代码字符串。
 * 支持事件监听、条件分支、多种动作类型。
 * 编译输出零 eval/Function，纯 if/else 控制流。
 *
 * @jnpf-generated dashboard-3d-blueprint v2.0.0
 */

// ============================================================
// Types
// ============================================================

export type BlueprintNodeType = 'event' | 'condition' | 'action';

export type BlueprintEventType = 'click' | 'hover' | 'data-change';

export type BlueprintActionType = 'highlight' | 'navigate' | 'show-popup' | 'update-data';

export interface BlueprintNode {
  /** 节点唯一标识 */
  id: string;
  /** 节点类型 */
  type: BlueprintNodeType;
  /** 节点配置 */
  config: Record<string, unknown>;
  /** 顺序下一个节点 ID */
  next?: string;
  /** 条件为 true 时的下一个节点 ID */
  nextTrue?: string;
  /** 条件为 false 时的下一个节点 ID */
  nextFalse?: string;
}

export interface BlueprintFlow {
  /** 流程唯一标识 */
  id: string;
  /** 流程名称 */
  name: string;
  /** 流程节点列表 */
  nodes: BlueprintNode[];
}

// ============================================================
// Compiler state
// ============================================================

interface CompileState {
  flow: BlueprintFlow;
  /** id → node */
  nodeMap: Map<string, BlueprintNode>;
  /** 已访问节点 ID 集合（用于循环检测） */
  visited: Set<string>;
  /** 生成的代码行 */
  lines: string[];
  /** 缩进级别 */
  indent: number;
}

// ============================================================
// Public API
// ============================================================

/**
 * 编译蓝图流程为 TypeScript 代码字符串。
 *
 * 从第一个 event 类型节点开始遍历，生成事件监听器 + 条件分支 + 动作链代码。
 *
 * @param flow 蓝图流程配置
 * @returns 带 @jnpf-generated 标记的 TypeScript 代码
 *
 * @example
 * const code = compileBlueprint({
 *   id: 'scene-click-alert',
 *   name: '点击告警',
 *   nodes: [
 *     { id: 'e1', type: 'event', config: { eventType: 'click', targetId: 'poi-sensor' } },
 *     { id: 'c1', type: 'condition', config: { field: 'data.temp', operator: '>', value: 80 }, nextTrue: 'a1', nextFalse: 'a2' },
 *     { id: 'a1', type: 'action', config: { actionType: 'highlight', targetId: 'poi-sensor', color: '#ff4560' } },
 *     { id: 'a2', type: 'action', config: { actionType: 'show-popup', title: '正常', content: '温度正常' } },
 *   ],
 * });
 */
export function compileBlueprint(flow: BlueprintFlow): string {
  const state: CompileState = {
    flow,
    nodeMap: new Map(flow.nodes.map(n => [n.id, n])),
    visited: new Set(),
    lines: [],
    indent: 0,
  };

  // ── Header ──
  emit(state, `// @jnpf-generated blueprint id="${flow.id}" name="${flow.name}"`);
  emit(state, `// Generated at ${new Date().toISOString()}`);
  emit(state, '');
  emit(state, "import type { SceneContext } from './types';");
  emit(state, '');

  // ── Find event nodes and compile each chain ──
  const eventNodes = flow.nodes.filter(n => n.type === 'event');
  if (eventNodes.length === 0) {
    emit(state, '// Warning: No event nodes found in flow');
    return state.lines.join('\n');
  }

  emit(state, `export function register${toPascalCase(flow.id)}Blueprints(ctx: SceneContext): void {`);
  state.indent++;

  for (const eventNode of eventNodes) {
    compileEventChain(state, eventNode);
  }

  state.indent--;
  emit(state, '}');
  emit(state, '');

  return state.lines.join('\n');
}

// ============================================================
// Event chain compilation
// ============================================================

function compileEventChain(state: CompileState, eventNode: BlueprintNode): void {
  state.visited.clear();

  const eventType = eventNode.config.eventType as BlueprintEventType | undefined;
  const targetId = eventNode.config.targetId as string | undefined;

  if (!eventType) {
    emit(state, `// Error: event node "${eventNode.id}" missing eventType`);
    return;
  }

  const handlerName = `handle_${eventNode.id}`;

  emit(state, '');

  // ── Event listener registration ──
  switch (eventType) {
    case 'click':
      emit(state, `// Event: click on "${targetId || '*'}"`);
      emit(state, `ctx.on('click', '${targetId || '*'}', ${handlerName});`);
      break;

    case 'hover':
      emit(state, `// Event: hover on "${targetId || '*'}"`);
      emit(state, `ctx.on('hover', '${targetId || '*'}', ${handlerName});`);
      break;

    case 'data-change':
      emit(state, `// Event: data-change on field "${targetId || '*'}"`);
      emit(state, `ctx.on('data-change', '${targetId || '*'}', ${handlerName});`);
      break;

    default:
      emit(state, `// Unknown event type: ${eventType}`);
      return;
  }

  // ── Handler function ──
  emit(state, `function ${handlerName}(payload: unknown): void {`);
  state.indent++;

  let currentNode = eventNode.next ? state.nodeMap.get(eventNode.next) : undefined;

  while (currentNode && !state.visited.has(currentNode.id)) {
    state.visited.add(currentNode.id);
    currentNode = compileNode(state, currentNode);
  }

  // Cycle detected
  if (currentNode && state.visited.has(currentNode.id)) {
    emit(state, `// Warning: cycle detected at node "${currentNode.id}", stopping`);
  }

  state.indent--;
  emit(state, '}');
}

/**
 * Compile a single node, return the next node or undefined.
 */
function compileNode(state: CompileState, node: BlueprintNode): BlueprintNode | undefined {
  switch (node.type) {
    case 'condition':
      return compileCondition(state, node);
    case 'action':
      return compileAction(state, node);
    default:
      emit(state, `// Unknown node type "${node.type}" at "${node.id}"`);
      return undefined;
  }
}

// ============================================================
// Condition compilation
// ============================================================

function compileCondition(state: CompileState, node: BlueprintNode): BlueprintNode | undefined {
  const field = node.config.field as string | undefined;
  const operator = node.config.operator as string | undefined;
  const value = node.config.value;

  if (!field || !operator) {
    emit(state, `// Error: condition node "${node.id}" missing field or operator`);
    return undefined;
  }

  const safeValue = formatLiteral(value);
  const conditionExpr = buildConditionExpr(field, operator, safeValue);

  emit(state, `if (${conditionExpr}) {`);
  state.indent++;

  // Follow nextTrue branch
  let finalNode: BlueprintNode | undefined = undefined;
  if (node.nextTrue) {
    const trueNode = state.nodeMap.get(node.nextTrue);
    if (trueNode && !state.visited.has(trueNode.id)) {
      state.visited.add(trueNode.id);
      let current: BlueprintNode | undefined = trueNode;
      while (current && !state.visited.has(current.id)) {
        state.visited.add(current.id);
        current = compileNode(state, current);
      }
      finalNode = current;
    }
  }

  state.indent--;
  emit(state, '}');

  // Follow nextFalse branch
  if (node.nextFalse) {
    emit(state, 'else {');
    state.indent++;

    const falseNode = state.nodeMap.get(node.nextFalse);
    if (falseNode && !state.visited.has(falseNode.id)) {
      state.visited.add(falseNode.id);
      let current: BlueprintNode | undefined = falseNode;
      while (current && !state.visited.has(current.id)) {
        state.visited.add(current.id);
        current = compileNode(state, current);
      }
      if (current) finalNode = current;
    }

    state.indent--;
    emit(state, '}');
  }

  return finalNode;
}

// ============================================================
// Action compilation
// ============================================================

function compileAction(state: CompileState, node: BlueprintNode): BlueprintNode | undefined {
  const actionType = node.config.actionType as BlueprintActionType | undefined;

  if (!actionType) {
    emit(state, `// Error: action node "${node.id}" missing actionType`);
    return undefined;
  }

  switch (actionType) {
    case 'highlight': {
      const targetId = node.config.targetId as string | undefined;
      const color = (node.config.color as string) || '#ff4560';
      emit(state, `ctx.highlight('${targetId || '*'}', '${color}', payload);`);
      break;
    }

    case 'navigate': {
      const url = (node.config.url as string) || '/';
      emit(state, `ctx.navigate('${url}', payload);`);
      break;
    }

    case 'show-popup': {
      const title = (node.config.title as string) || '';
      const content = (node.config.content as string) || '';
      emit(state, `ctx.showPopup('${escapeString(title)}', '${escapeString(content)}', payload);`);
      break;
    }

    case 'update-data': {
      const dataField = (node.config.dataField as string) || '';
      const dataValue = formatLiteral(node.config.dataValue);
      emit(state, `ctx.updateData('${dataField}', ${dataValue}, payload);`);
      break;
    }

    default:
      emit(state, `// Unknown action type: ${actionType}`);
      break;
  }

  // Follow next
  if (node.next) {
    return state.nodeMap.get(node.next);
  }
  return undefined;
}

// ============================================================
// Helpers
// ============================================================

function buildConditionExpr(field: string, operator: string, safeValue: string): string {
  const accessor = field
    .split('.')
    .map((k, i) => (i === 0 ? `(payload as any)?.['${k}']` : `?.['${k}']`))
    .join('');

  switch (operator) {
    case '>':
      return `${accessor} > ${safeValue}`;
    case '>=':
      return `${accessor} >= ${safeValue}`;
    case '<':
      return `${accessor} < ${safeValue}`;
    case '<=':
      return `${accessor} <= ${safeValue}`;
    case '==':
      return `${accessor} == ${safeValue}`;
    case '!=':
      return `${accessor} != ${safeValue}`;
    case '===':
      return `${accessor} === ${safeValue}`;
    case '!==':
      return `${accessor} !== ${safeValue}`;
    default:
      return `${accessor} ${operator} ${safeValue}`;
  }
}

function formatLiteral(value: unknown): string {
  if (value === null || value === undefined) return 'null';
  if (typeof value === 'string') return `'${escapeString(value)}'`;
  if (typeof value === 'number') return String(value);
  if (typeof value === 'boolean') return String(value);
  return JSON.stringify(value);
}

function escapeString(s: string): string {
  return s.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/\n/g, '\\n');
}

function toPascalCase(s: string): string {
  return s.replace(/[-_\s]+(.)?/g, (_, c: string) => (c ? c.toUpperCase() : '')).replace(/^./, c => c.toUpperCase());
}

function emit(state: CompileState, line: string): void {
  const prefix = '  '.repeat(state.indent);
  state.lines.push(prefix + line);
}
