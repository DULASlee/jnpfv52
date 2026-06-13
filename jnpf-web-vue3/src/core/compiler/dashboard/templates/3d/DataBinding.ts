/**
 * 3D 数据绑定层 — 安全规则引擎
 *
 * 根据数据源更新 3D 要素（POI、围栏、热力图、飞线）的状态、
 * 颜色和可见性。条件求值使用字符串解析，零 eval/Function。
 *
 * @jnpf-generated dashboard-3d-databinding v2.0.0
 */

import * as THREE from 'three';
import type { CSS2DObject } from 'three/examples/jsm/renderers/CSS2DRenderer.js';
import type { FenceObject } from './Fence';
import { updatePOIStatus } from './POI';
import { updateFenceStatus } from './Fence';
import type { POIStatus } from './POI';
import type { FenceStatus } from './Fence';

// ============================================================
// Types
// ============================================================

export type DataBindingTargetType = 'poi' | 'fence' | 'heatmap' | 'flyline';

export interface DataBindingMapping {
  /** 条件表达式字符串，如 "> 80"、"== 'alarm'"、"!= normal" */
  condition: string;
  /** 满足条件时执行的动作 */
  action: DataBindingAction;
}

export interface DataBindingAction {
  /** 设置状态 */
  status?: string;
  /** 设置颜色 */
  color?: string;
  /** 设置可见性 */
  visible?: boolean;
}

export interface DataBindingRule {
  /** 目标 3D 对象名称 */
  targetId: string;
  /** 目标类型 */
  targetType: DataBindingTargetType;
  /** 数据字段路径，如 'temperature'、'data.value' */
  dataField: string;
  /** 条件→动作映射数组，按顺序匹配，命中首个后停止 */
  mapping: DataBindingMapping[];
}

// ============================================================
// Safe nested value access
// ============================================================

/**
 * 从嵌套对象中安全取值。
 * @example getNestedValue({ data: { temp: 42 } }, 'data.temp') → 42
 */
export function getNestedValue(data: Record<string, unknown>, path: string): unknown {
  const keys = path.split('.');
  let current: unknown = data;

  for (const key of keys) {
    if (current === null || current === undefined) return undefined;
    if (typeof current !== 'object') return undefined;
    current = (current as Record<string, unknown>)[key];
  }

  return current;
}

// ============================================================
// Safe condition evaluation (zero eval/Function)
// ============================================================

/**
 * 安全求值条件表达式。
 *
 * 支持格式：
 *   - 比较运算符: "> 80", ">= 50", "< 30", "<= 10", "== active", "!= normal"
 *   - 括号分组仅在需要时手动加
 *
 * @param value 实际数据值
 * @param condition 条件字符串
 * @returns 是否满足条件
 */
function evaluateCondition(value: unknown, condition: string): boolean {
  const trimmed = condition.trim();

  // ── "!= <value>" ──
  const neqMatch = trimmed.match(/^!=\s*(.+)$/);
  if (neqMatch) {
    return !valuesEqual(value, parseConditionValue(neqMatch[1]));
  }

  // ── "== <value>" ──
  const eqMatch = trimmed.match(/^==\s*(.+)$/);
  if (eqMatch) {
    return valuesEqual(value, parseConditionValue(eqMatch[1]));
  }

  // ── ">= <number>" ──
  const gteMatch = trimmed.match(/^>=\s*(.+)$/);
  if (gteMatch) {
    const numVal = toNumber(value);
    const threshold = parseFloat(gteMatch[1]);
    return !isNaN(numVal) && !isNaN(threshold) && numVal >= threshold;
  }

  // ── "<= <number>" ──
  const lteMatch = trimmed.match(/^<=\s*(.+)$/);
  if (lteMatch) {
    const numVal = toNumber(value);
    const threshold = parseFloat(lteMatch[1]);
    return !isNaN(numVal) && !isNaN(threshold) && numVal <= threshold;
  }

  // ── "> <number>" ──
  const gtMatch = trimmed.match(/^>\s*(.+)$/);
  if (gtMatch) {
    const numVal = toNumber(value);
    const threshold = parseFloat(gtMatch[1]);
    return !isNaN(numVal) && !isNaN(threshold) && numVal > threshold;
  }

  // ── "< <number>" ──
  const ltMatch = trimmed.match(/^<\s*(.+)$/);
  if (ltMatch) {
    const numVal = toNumber(value);
    const threshold = parseFloat(ltMatch[1]);
    return !isNaN(numVal) && !isNaN(threshold) && numVal < threshold;
  }

  // ── truthy check (bare string) ──
  return !!value;
}

/**
 * 解析条件值（去除可能的引号）
 */
function parseConditionValue(raw: string): string | number | boolean {
  const t = raw.trim();

  // Quoted string
  if ((t.startsWith("'") && t.endsWith("'")) || (t.startsWith('"') && t.endsWith('"'))) {
    return t.slice(1, -1);
  }

  // Boolean
  if (t === 'true') return true;
  if (t === 'false') return false;

  // Number
  const num = Number(t);
  if (!isNaN(num) && t !== '') return num;

  // String
  return t;
}

/**
 * 值相等比较（兼容 string/number/boolean）
 */
function valuesEqual(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  // Loose number/string comparison
  if (typeof a === 'number' && typeof b === 'string') return a === Number(b);
  if (typeof a === 'string' && typeof b === 'number') return Number(a) === b;
  return String(a) === String(b);
}

/**
 * 尝试将值转为 number
 */
function toNumber(v: unknown): number {
  if (typeof v === 'number') return v;
  if (typeof v === 'string') {
    const n = Number(v);
    return isNaN(n) ? NaN : n;
  }
  return NaN;
}

// ============================================================
// Action application
// ============================================================

function applyAction(obj: THREE.Object3D, targetType: DataBindingTargetType, action: DataBindingAction): void {
  // ── Status ──
  if (action.status !== undefined) {
    if (targetType === 'poi') {
      updatePOIStatus(obj as CSS2DObject, action.status as POIStatus);
    } else if (targetType === 'fence') {
      updateFenceStatus(obj as FenceObject, action.status as FenceStatus);
    }
    // heatmap / flyline: status stored in userData
    obj.userData.status = action.status;
  }

  // ── Color ──
  if (action.color !== undefined) {
    const newColor = new THREE.Color(action.color);
    obj.traverse(child => {
      if (child instanceof THREE.Mesh && child.material) {
        const materials = Array.isArray(child.material) ? child.material : [child.material];
        for (const mat of materials) {
          if ('color' in mat && mat.color instanceof THREE.Color) {
            mat.color.copy(newColor);
          }
        }
      }
      if (child instanceof THREE.Line && child.material) {
        const materials = Array.isArray(child.material) ? child.material : [child.material];
        for (const mat of materials) {
          if ('color' in mat && mat.color instanceof THREE.Color) {
            mat.color.copy(newColor);
          }
        }
      }
    });
  }

  // ── Visibility ──
  if (action.visible !== undefined) {
    obj.visible = action.visible;
  }
}

// ============================================================
// Main API
// ============================================================

/**
 * 根据数据源应用绑定规则。
 *
 * 遍历规则列表，对每条规则：取数据字段值 → 检查条件→动作映射 → 执行首个命中的动作。
 *
 * @param rules 数据绑定规则数组
 * @param data 数据源对象
 * @param scene Three.js 场景（用于查找目标对象）
 *
 * @example
 * applyDataBindings([
 *   { targetId: 'temp-sensor', targetType: 'poi', dataField: 'temperature',
 *     mapping: [
 *       { condition: '> 80', action: { status: 'alarm', color: '#ff4560' } },
 *       { condition: '> 50', action: { status: 'warning', color: '#ffa940' } },
 *     ]},
 * ], { temperature: 85 }, scene);
 */
export function applyDataBindings(rules: DataBindingRule[], data: Record<string, unknown>, scene: THREE.Scene): void {
  for (const rule of rules) {
    // Find target object
    const target = findTarget(scene, rule.targetId, rule.targetType);
    if (!target) {
      console.warn(`[DataBinding] target not found: ${rule.targetId} (${rule.targetType})`);
      continue;
    }

    // Get data value
    const value = getNestedValue(data, rule.dataField);

    // Match conditions in order
    for (const mapping of rule.mapping) {
      if (evaluateCondition(value, mapping.condition)) {
        applyAction(target, rule.targetType, mapping.action);
        break; // First match wins
      }
    }
  }
}

// ============================================================
// Object lookup
// ============================================================

function findTarget(scene: THREE.Scene, targetId: string, targetType: DataBindingTargetType): THREE.Object3D | undefined {
  // Try exact name match first
  const byName = scene.getObjectByName(targetId);
  if (byName) return byName;

  // Try prefixed names
  const prefixMap: Record<DataBindingTargetType, string> = {
    poi: `poi-${targetId}`,
    fence: `fence-${targetId}`,
    heatmap: `heatmap-${targetId}`,
    flyline: `flyline-${targetId}`,
  };

  const prefixed = scene.getObjectByName(prefixMap[targetType]);
  if (prefixed) return prefixed;

  // Deep search by name
  let found: THREE.Object3D | undefined;
  scene.traverse(child => {
    if (found) return;
    if (child.name === targetId || child.name === prefixMap[targetType]) {
      found = child;
    }
  });

  return found;
}
