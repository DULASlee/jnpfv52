/**
 * 沙箱上下文
 *
 * 两级数据模型：
 *   真实状态（reactive, 可写）→ Vue 组件的 data
 *   安全快照（frozen, 只读）→ 传入表达式引擎的副本
 *
 * 规则：
 *   1. createSafeSnapshot 创建浅拷贝 + 冻结副本，不修改原始数据
 *   2. 表达式引擎只能读取快照，不能修改真实状态
 *   3. 每次求值都创建新快照，不缓存快照对象
 */

import { WHITELIST_FUNCTIONS } from './functions';

const ALLOWED_TOP_KEYS = [
  'formData',
  'data',
  'rowIndex',
  'row',
  'column',
  'value',
  'setFormData',
  'setShowOrHide',
  'setRequired',
  'setDisabled',
  'onlineUtils',
];

export function createSafeSnapshot(data: Record<string, unknown>): Readonly<Record<string, unknown>> {
  // 浅拷贝顶层数据（性能考虑——深拷贝由调用方决定）
  const safe: Record<string, unknown> = {};

  for (const key of ALLOWED_TOP_KEYS) {
    if (key in data) {
      safe[key] = data[key];
    }
  }

  // 注入白名单函数
  for (const [name, fn] of Object.entries(WHITELIST_FUNCTIONS)) {
    safe[name] = fn;
  }

  // 冻结顶层（不递归——避免性能问题和与 Vue reactive 的冲突）
  return Object.freeze(safe);
}
