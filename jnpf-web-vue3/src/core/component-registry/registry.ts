/**
 * 组件注册表核心
 *
 * 设计原则：
 *   1. 单一数据源——所有组件在此注册，不再分散
 *   2. 降级策略——未知组件返回默认映射 + 警告
 *   3. 版本感知——查找时支持版本检查
 *   4. AI 就位——category 为 AI 提供组件语义信息
 */

import type { ComponentEntry, ComponentCategory } from './types';

export class ComponentRegistry {
  private entries = new Map<string, ComponentEntry>();

  /**
   * 注册组件
   */
  register(entry: ComponentEntry): void {
    if (this.entries.has(entry.type)) {
      console.warn(`[ComponentRegistry] 组件 "${entry.type}" 已存在，将被覆盖`);
    }
    this.entries.set(entry.type, entry);
  }

  /**
   * 批量注册
   */
  registerBatch(entries: ComponentEntry[]): void {
    for (const entry of entries) {
      this.register(entry);
    }
  }

  /**
   * 按类型查找组件
   * 未知类型返回降级映射（a-input / uni-easyinput）+ 警告
   */
  resolve(type: string): ComponentEntry {
    const entry = this.entries.get(type);
    if (!entry) {
      console.warn(`[ComponentRegistry] 未知组件类型: ${type}，降级为 a-input / uni-easyinput`);
      return {
        type,
        name: type,
        category: 'other',
        pc: 'a-input',
        app: 'uni-easyinput',
      };
    }
    if (entry.deprecated) {
      console.warn(`[ComponentRegistry] ${type} 已废弃` + (entry.replacedBy ? `，请使用 ${entry.replacedBy}` : ''));
    }
    return entry;
  }

  /**
   * 简化查找——只返回 PC 和 App 组件名
   */
  resolveMapping(type: string): { pc: string; app: string } {
    const entry = this.resolve(type);
    return { pc: entry.pc, app: entry.app };
  }

  /**
   * 按分类查找
   */
  getByCategory(category: ComponentCategory): ComponentEntry[] {
    return [...this.entries.values()].filter(e => e.category === category);
  }

  /**
   * 列出所有已注册组件
   */
  list(): ComponentEntry[] {
    return [...this.entries.values()];
  }

  /**
   * 列出所有已注册的类型标识
   */
  listTypes(): string[] {
    return [...this.entries.keys()];
  }

  /**
   * 检查类型是否已注册
   */
  has(type: string): boolean {
    return this.entries.has(type);
  }

  /**
   * 统计
   */
  stats(): { total: number; byCategory: Record<string, number> } {
    const byCategory: Record<string, number> = {};
    for (const entry of this.entries.values()) {
      byCategory[entry.category] = (byCategory[entry.category] || 0) + 1;
    }
    return { total: this.entries.size, byCategory };
  }
}
