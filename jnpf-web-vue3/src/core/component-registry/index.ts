/**
 * 组件注册表 — 统一导出入口
 *
 * 使用方式：
 *   import { registry } from '@/core/component-registry';
 *   const entry = registry.resolve('JnpfInput');
 */

import { ComponentRegistry } from './registry';
import { BUILTIN_COMPONENTS } from './builtin';

// 创建全局单例注册表
export const registry = new ComponentRegistry();

// 注册所有内置组件
registry.registerBatch(BUILTIN_COMPONENTS);

// 导出类型和类
export { ComponentRegistry } from './registry';
export type { ComponentEntry, ComponentCategory, PropSchema } from './types';
export { BUILTIN_COMPONENTS } from './builtin';
