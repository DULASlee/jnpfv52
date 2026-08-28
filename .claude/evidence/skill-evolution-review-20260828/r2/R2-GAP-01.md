# R2-GAP-01 — 计数口径解释假定：grep 定点读取是否计 Scope

> **类型**：Implementation Clarification | **日期**：2026-08-28 | **状态**：🟢 **ACCEPTED — Implementation Clarification / No R1 Change**（首席架构师 2026-08-28 裁定：不启动 R1 演进，R1 分档表零修改）  
> **发现于**：R2 机制包构建期（前瞻登记，非 36 runs 实测产物）  
> **下游**：`Level-1-Context-Acquisition.md` §4（口径同步更新）、`tests/skill-r2/trace-validator.ts` V-1a（含锁定语义负例 2 条，28/28 绿）

## 裁定的锁定语义（A-§4 定稿，永久边界）

> **Targeted evidence localization ≠ Scope expansion；Broad repository discovery = Scope expansion。**

- **免 Scope**：定点 grep / symbol lookup / exact file-line retrieval —— 只为钉住一条具体证据行的"证据定位操作"，不扩大调查对象集合。
- **不免账**：定位操作读取与产生的证据**必须照常计入 Artifact / Depth / Iteration**。
- **防滥用锁死**："grep 不算 Scope" **绝不**等于"可以 grep 整个仓库"——每次定点触碰一个新文件都消耗 Artifact，无差别拖网会被 Artifact 预算线性卡死（Validator 已配负例证明：Low×Regional a2 档 5 次定点 grep → V-1a FAIL）。
- **判据本质**（裁定原文）：Context Budget 限制的是**无边界扩张**，不是**完成当前 Claim 所必需的最小证据获取**。若机械口径导致"为证明该被证明的事实反被预算阻止"，即属误读。

## 原现象记录（保留）

RB-01（OrderService 无事务→GO）的合法证据天然跨两个 framework project（`JNPF` 属性/标记接口 + `JNPF.API.Entry` AOP 注册），机械按"读了新 project=+1 Scope"会让 Regional S=1 结构性装不下任何 DI 类 Finding 的 GO。现按锁定语义解决：AOP 注册行经**定点取证**（计 Artifact/Depth，不扩调查对象集合，免 Scope）→ RB-01 Scope=1 达标，GO 合法可达。
