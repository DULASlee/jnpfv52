# ADR-018: UniApp UI 库选型

**状态:** Final
**日期:** 2026-06-12
**阶段:** Sprint 0-A Day 4

---

## 背景

前端新编译器需要为 UniApp 端生成移动端代码。当前 JNPF 有两套 UniApp 项目：

- **legacyApp** (`jnpf-app-vue3`): 现有移动端，使用 `uni_modules` 生态（`uni-easyinput`、`uni-datetime-picker` 等）
- **newApp**: 计划中的新 UniApp 项目，使用新编译器生成

需要为新编译器选择 UniApp UI 库。

---

## 决策内容

**新编译器采用 `wot-design-uni`；legacyApp 保留 `uni_modules` 不变。**

### 选型理由

| 维度 | `uni_modules` (legacy) | `wot-design-uni` (new) |
|------|----------------------|------------------------|
| 组件丰富度 | 基础组件 (easyinput, datetime-picker, etc.) | 60+ 组件，含 form/table/picker/toast |
| TypeScript | 类型定义不完整 | 完整 TypeScript 类型 |
| 样式定制 | 依赖 uni-app 内置样式 | CSS 变量主题系统 |
| Vue 3 支持 | 有限 | 原生 Vue 3 Composition API |
| 维护活跃度 | 低（uni-app 官方维护，频率低） | 高（社区活跃，周更） |
| 与 AntDV 对齐 | 组件 API 差异大 | 组件 API 与 AntDV 高度对齐 |

### 三层映射约定

```
component-mapping.ts:

  pc:         "a-input"          → Web (Ant Design Vue)
  app:        "wd-input"         → 新 UniApp (wot-design-uni)
  legacyApp:  "uni-easyinput"    → 旧 UniApp (uni_modules)
```

### 迁移策略

| 项目 | 状态 | UI 库 |
|------|------|-------|
| `jnpf-app-vue3` (legacyApp) | 维护模式，不改动 | `uni_modules` |
| `jnpf-app-vue3-new` (newApp) | 新编译器生成目标 | `wot-design-uni` |

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|------|------|------|----------|
| 统一用 `uni_modules` | 与 legacyApp 一致 | 组件少、类型差、API 不对齐 AntDV | 阻碍编译器输出质量 |
| 用 `uView Plus` | 组件丰富 | 维护停滞、Vue 3 支持差 | 风险高 |
| **新用 `wot-design-uni` + legacy 不动** | 最优对齐 + 零影响 | 两套 UI 库并存 | ✅ 选择 |

---

## 后果

**正面:** 新编译器生成的 UniApp 代码质量与 Web 端一致；TypeScript 类型完整；组件 API 对齐 AntDV 降低学习成本。

**负面:** 两套 UniApp UI 库并存；组件映射表需维护三层映射（pc/app/legacyApp）。

**缓解:** legacyApp 处于维护模式，不新增功能；新应用全部走 wot-design-uni。

---

## 组件映射示例

```typescript
// JnpfKey → 三层映射
'JnpfInput':       { pc: 'a-input',        app: 'wd-input',       legacyApp: 'uni-easyinput' },
'JnpfInputNumber': { pc: 'a-input-number',  app: 'wd-input-number', legacyApp: 'uni-number-box' },
'JnpfSelect':      { pc: 'a-select',        app: 'wd-select',      legacyApp: 'uni-data-select' },
'JnpfDatePicker':  { pc: 'a-date-picker',    app: 'wd-datetime-picker', legacyApp: 'uni-datetime-picker' },
'JnpfSwitch':      { pc: 'a-switch',         app: 'wd-switch',      legacyApp: 'switch' },
```
