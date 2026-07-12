# JNPF V5.2 前端架构重构 — 文档中心

> 分支: `frontend-architecture-refactor`
> 创建日期: 2026-06-08
> 状态: 启动阶段

---

## 目录

本目录保存前端架构重构的所有文档，包括设计文档、ADR、实施计划、验证报告等。

```
docs/frontend-architecture/
├── README.md                    ← 本文件 (文档索引)
├── 00-overview.md               ← 前端架构总览与现状分析
├── 01-component-architecture.md ← 组件架构设计
├── 02-state-management.md       ← 状态管理方案
├── 03-routing-system.md         ← 路由系统设计
├── 04-build-toolchain.md        ← 构建工具链优化
├── 05-performance.md            ← 性能优化方案
├── 06-code-standards.md         ← 代码规范与最佳实践
├── adr/                         ← 前端架构决策记录
│   └── README.md
├── plans/                       ← 实施计划
│   └── README.md
├── verification/                ← 验证报告
│   └── README.md
└── reports/                     ← 阶段报告
    └── README.md
```

---

## 前端项目清单

| 项目 | 技术栈 | 端口 | 说明 |
|---|---|---|---|
| `jnpf-web-vue3` | Vue3 + Vite + Ant Design Vue + WindiCSS | :3100 | PC 管理后台 |
| `jnpf-web-datascreen` | Vue3 + Vite + DataV | :3102 | 数字大屏 |
| `jnpf-app-vue3` | UniApp (Vue3) | H5 代理 | 移动端 H5/App |

---

## 重构原则

1. **非颠覆性**: 不改变对外 API 和数据流契约
2. **渐进式**: 每阶段可独立交付，不影响业务功能
3. **组件骨架不动**: BasicTable / BasicForm / BasicPopup / jnpf-content-wrapper 用法不可更改
4. **皮肤层可提升**: 颜色、间距、阴影、字体层级、hover 效果、加载动画
5. **生成页面禁止改**: .vm 模板输出的页面不属于增强范围

---

## 阶段规划

| 阶段 | 内容 | 预估 |
|---|---|---|
| Phase 0 | 现状评估与基线建立 | TBD |
| Phase 1 | 组件架构标准化 | TBD |
| Phase 2 | 状态管理与数据流 | TBD |
| Phase 3 | 构建工具链与性能 | TBD |
| Phase 4 | 代码质量与工程化 | TBD |

---

## 相关文档

- 前端开发规范: `docs/frontend/jnpf-taste-blueprint.md`
- 后端架构总览: `docs/architecture/overview.md`
- 开发规范: `docs/development/guide.md`
- 前端规则: `.claude/rules/jnpf-frontend-rules.md`
