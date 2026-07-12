# 针式搜索铁律（Needle Search — 永久遵循）

> **一句核心：针式问题用针，不用拖网。**  
> **Cursor 镜像：** `.cursor/rules/needle-search.mdc`（alwaysApply: true）  
> **关联：** `toolchain-division` · `AGENTS.md` §Code Search

本仓为大型 monorepo。广域搜索、堆并行、整文件灌入是会话卡死的主因。**任何文件定位 / 符号查找 MUST 遵守本规则。**

## 决策树（强制）

| 你知道什么 | ✅ 正确动作 | ❌ 禁止 |
|---|---|---|
| 已知路径 / 模块文件名 | 直接 Read（大文件带 offset/limit） | 先全仓 Glob/Grep |
| 只知关键词、不知路径 | **一次**精准 Grep（必须带 path 和/或 glob） | 无 path/glob 的全仓扫 |
| C# 类/方法/接口名 | **Serena** find_symbol / find_referencing_symbols | Shell find；广域 Grep 扫符号 |
| 只知文件名模式 | **窄** Glob（如 `**/PmSkill*.cs`） | `**/*`、`**/*.cs` 拖网 |
| 「这个文件在哪」单点问题 | 窄 Glob 或一次 Grep | 派 explore / Task 子 Agent |

## 六条硬约束

1. **先窄后宽** — 先限定已知模块目录；禁止第一步全仓扫。
2. **工具选型** — C# 符号 → Serena；文本 → Grep；文件名 → 窄 Glob；已知路径 → Read。
3. **禁止 Shell 全仓搜索** — 不得用 find / dir /s / Get-ChildItem -Recurse 做文件发现。
4. **禁止为找文件派 explore** — 单文件/单符号定位禁止派子 Agent。
5. **并行上限** — 同轮最多 2–3 个独立窄查询；禁止 8+ 广域并行。
6. **大文件局部读** — 先 Grep 定位行号，再局部 Read。

## 卡住信号（>15s）

单次工具 >15 秒无结果或被用户中断 → 停止盲重试 → 收窄 path/glob 或换 Serena/直接 Read → MCP 卡住则跳过改用窄 Grep/Read。

## 禁止清单

- ❌ 无 path/glob 的全仓 Grep 作为第一步
- ❌ Glob `**/*` 拖网
- ❌ Shell 递归列目录找文件
- ❌ 为找一个文件派 explore
- ❌ 同轮 8+ 广域工具并行
- ❌ 大文件无 offset/limit 整读
- ❌ 超时后原参数重试
