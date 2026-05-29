# 前端项目施工阶段 · 开发计划与施工包

> **状态**：待架构师审核（审核通过后方可执行）  
> **编制日期**：2026-05-22  
> **对应迭代方案**：[`../1、迭代方案/1、第一次整理意见.md`](../1、迭代方案/1、第一次整理意见.md)

---

## 文档清单

| 编号 | 文档 | 说明 |
|------|------|------|
| **01** | [`01-前端整理开发计划与施工包.md`](01-前端整理开发计划与施工包.md) | **主施工包**：原则、排期、分步骤任务、验收标准、禁止项 |
| **02** | [`02-dist源码对照矩阵.md`](02-dist源码对照矩阵.md) | dist_v1.1 与 `jnpf-web-vue3` 页面对照（已审计数据） |
| **03** | [`03-GAP待补清单与功能对等验收表.md`](03-GAP待补清单与功能对等验收表.md) | 19 项 dist 独有缺口 + 冒烟/对等验收表（实施时勾选） |

## 关联资产（仓库内已有）

| 路径 | 说明 |
|------|------|
| `jnpf-web-vue3/` | 正式前端工程（Git：`aplyhj/jnpfsoft-jnpf-jnpf-web-vue3-`，v3.6.0） |
| `web/dist_v1.1/` | **生产基准 dist**（真理之源，保留不删） |
| `jnpf-web-vue3/.env.production.dist-v1.1.template` | 与 dist 对齐的生产 env 模板 |
| `docs/architecture/dist-src-audit.json` | 机器可读审计 JSON |
| `docs/architecture/04-application-frontend-deep-dive.md` | 专项文档 04（前端架构内参） |
| `docs/architecture/water-module-from-dist.md` | water 模块路径清单 |

## 推荐执行顺序

```
审核通过 → F0 dist审计收口 → F1 环境/构建基线 → F2 功能对等验证
         → F3 按 GAP 优先级修补 → F4 部署切换（P0 全绿后）
```

**预估工期**：F0–F2 约 **3 人日**；F3 视 GAP 业务确认 **3–10 人日**；F4 **0.5 人日**。
