# 09 — 代码生成器前端对接扫描

> 扫描日期：2026-06-08

---

## 生成模板位置

```
backend/application/JNPF.API.Entry/wwwroot/Template/
├── 1-SingleTable/          — 单表 CRUD 后端模板
├── 2-MainBelt/             — 主从表
├── 3-Auxiliary/            — 辅助表
├── 4-MainBeltVice/         — 主从副
├── 5-PrimarySecondary/     — 主次表
├── SubTable/               — 子表
├── InlineEditor/           — 行内编辑
├── vue3/                   — ★ Vue3 前端模板
│   ├── index.vue.vm        — 列表页 (含树/搜索/表/子表/流程/打印/导入导出)
│   ├── Form.vue.vm         — 表单页
│   ├── Detail.vue.vm       — 详情页
│   ├── api.ts.vm           — API 层
│   ├── columnList.ts.vm    — 列定义
│   ├── searchList.ts.vm    — 搜索 Schema
│   ├── superQueryJson.ts.vm — 高级查询 JSON
│   ├── PureForm/           — 纯表单变体
│   ├── InlineEditing/      — 行内编辑变体
│   └── WorkFlow/           — 工作流变体
├── appIndex.vue.vm         — UniApp 列表页
├── appForm.vue.vm          — UniApp 表单页
└── appDetail.vue.vm        — UniApp 详情页
```

## 生成代码特征

- 模板语言: Apache Velocity (`.vm`)
- 转义规则: `@` → `@@` 在模板中，输出时为 `@` (如 `@@register` → `@register`)
- 产物: `helper/api.ts`, `helper/columnList.ts`, `helper/searchList.ts` 与 `index.vue`/`Form.vue`/`Detail.vue`
- 生成页面遵循 JNPF 标准布局: `jnpf-content-wrapper` → `jnpf-content-wrapper-left` + `jnpf-content-wrapper-center`

## 生成代码 vs 手写代码

| 特征 | 生成代码 | 手写代码 |
|---|---|---|
| 位置 | views/ 下任意位置 (菜单驱动) | views/ 下、components/ 下 |
| 标记 | 无显式标记 (依赖 .vm 模板约定) | 无 |
| 修改 | 通过生成器重新生成覆盖 | 直接编辑 |
| 模板维护者 | 后端工程师 (.vm 文件) | — |

## 关键发现

| # | 发现 |
|---|---|
| 1 | 生成代码无显式标记 — 无法区分哪些文件是生成器产物 |
| 2 | 生成代码被修改后重新生成会覆盖 — 无合并/保护机制 |
| 3 | .vm 模板成熟度高 — 支持树/搜索/子表/流程/打印/导入导出 |
| 4 | Vue3 和 UniApp 模板独立维护 — 功能不完全对等 |
