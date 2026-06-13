/**
 * FlowIR 序列化/反序列化
 *
 * 提供 FlowIR ↔ JSON ↔ 平台 Schema 的双向转换。
 *
 * @jnpf-generated v5.2.0 type=ir-serializer platform=universal
 */

import type { FlowIR, FlowSnapshot } from './flow-types';
import { validateFlowIR } from './flow-types';

// ============================================================
// FlowIR ↔ JSON
// ============================================================

/**
 * 序列化：FlowIR → JSON 字符串
 */
export function serializeFlowIR(ir: FlowIR): string {
  const output = {
    $schema: 'jnpf-flow-ir/v1',
    $generated: new Date().toISOString(),
    ...ir,
  };
  return JSON.stringify(output, null, 2);
}

/**
 * 反序列化：JSON 字符串 → FlowIR（含校验）
 * 缺失字段自动补默认值，不影响解析。
 */
export function deserializeFlowIR(json: string): {
  ir: FlowIR | null;
  errors: string[];
} {
  let parsed: Record<string, unknown>;

  try {
    parsed = JSON.parse(json);
  } catch (e) {
    return {
      ir: null,
      errors: [`JSON parse error: ${(e as Error).message}`],
    };
  }

  if (parsed.type !== 'workflow') {
    return {
      ir: null,
      errors: ['Invalid FlowIR: type must be "workflow"'],
    };
  }

  // 补全缺失字段
  const ir: FlowIR = {
    type: 'workflow',
    id: (parsed.id as string) ?? '',
    name: (parsed.name as string) ?? '',
    version: (parsed.version as string) ?? '1.0.0',
    nodes: (parsed.nodes as FlowIR['nodes']) ?? [],
    edges: (parsed.edges as FlowIR['edges']) ?? [],
    variables: (parsed.variables as FlowIR['variables']) ?? [],
    aiHints: parsed.aiHints as FlowIR['aiHints'] | undefined,
  };

  // 验证
  const issues = validateFlowIR(ir);
  const errors = issues.filter(i => i.level === 'error').map(i => `[${i.code}] ${i.message}`);

  return { ir, errors };
}

// ============================================================
// FlowIR ↔ 平台 Schema（用于数据库存储）
// ============================================================

/**
 * FlowIR → 平台存储格式
 */
export function flowIRToSchema(ir: FlowIR): Record<string, unknown> {
  return {
    flowData: serializeFlowIR(ir),
    flowName: ir.name,
    flowVersion: ir.version,
    nodeCount: ir.nodes.length,
    edgeCount: ir.edges.length,
  };
}

/**
 * 平台存储格式 → FlowIR
 */
export function schemaToFlowIR(schema: Record<string, unknown>): FlowIR | null {
  const data = schema.flowData;
  if (typeof data !== 'string') return null;
  const { ir } = deserializeFlowIR(data);
  return ir;
}

// ============================================================
// 快照序列化
// ============================================================

/**
 * 快照 → JSON 字符串
 */
export function serializeSnapshot(snapshot: FlowSnapshot): string {
  return JSON.stringify(snapshot, null, 2);
}

/**
 * JSON 字符串 → 快照
 */
export function deserializeSnapshot(json: string): FlowSnapshot | null {
  try {
    return JSON.parse(json) as FlowSnapshot;
  } catch {
    return null;
  }
}
