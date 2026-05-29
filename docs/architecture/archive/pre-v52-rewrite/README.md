# 迁入失败快照（pre-v52-rewrite）

> **状态**：已废弃 · **禁止**作为 v5.2 架构真相来源  
> **归档日期**：2026-05-24  
> **归档原因**：文档本质为 v3.6 时代内容 + 局部 v5.2 标签，存在版本污染（如 `localhost:5000`、旧部署拓扑）  

---

## 本目录内容

| 文件 | 原用途 | 污染示例 |
|------|--------|----------|
| `01-core-framework.md` | 专项01 核心框架 | 部分已 v5.2 化，但未系统核验 |
| `02-application-services.md` | 专项02 应用服务 | 混有旧假设 |
| `03-application-modules-deep-dive.md` | 专项03 业务模块 | DDL/路径未对齐 v5.2 迁移环境 |
| `04-application-frontend-deep-dive.md` | 专项04 前端 | **`localhost:5000`**、旧 proxy 描述 |
| `05-frontend-source-merge-completion.md` | 前端收口记录 | `:5000`、v3.6.0 package 语境 |
| `02-application-services-review.md` | 审查报告 | 基于旧 02 正文 |
| `water-module-from-dist.*` | water 模块档案 | dist 对照，非 v5.2 运行时 |

---

## 使用规则

| 允许 | 禁止 |
|------|------|
| 查阅「曾写过哪些章节、哪些主题」 | 复制粘贴进 `v52/` |
| 对照 v3.6 术语与业务背景 | 当作 v5.2 配置依据 |
| 提取功能清单作编写 checklist | 修补后重新迁入 v52 |

**v5.2 现行编写入口**：[`../../v52/README.md`](../../v52/README.md)

---

## v5.2 正确环境速查（勿用本目录中的端口）

| 服务 | v5.2 地址 |
|------|-----------|
| 后端 API | `http://localhost:30000` |
| 主 WEB dev | `http://localhost:3100`（`VITE_PROXY` → `:30000`） |
| 数字大屏 | `http://localhost:8100/DataV/` |
| UniApp H5 | `http://localhost:3800` |

见：[`../../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md`](../../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md)
