# Skill Version Evolution Timeline — Git 实证

> 全部行来自 `git log --oneline -- .claude/skills/generic-class-refactor-expert`（HEAD=b3b8acde）。不补不存在的版本。

| Version | Commit / 时间 | 主要变化 | 能力变化(净) | 验证状态 |
|---------|---------------|----------|--------------|----------|
| **v4.0 (first)** | `e45f724a` | 建立 Evidence-Driven Skill + 首个生产样本 EmailService F-03 异常保栈 | 系统奠基（P0五维/6要素/门控） | 单样本落地 |
| **v4.0 (frozen baseline)** | `81bc1dce` (08-27 13:14) | 冻结 Golden #1 + Evidence→Modify/Stop 门 | 门控骨架冻结 | **FROZEN**（本审计基准线） |
| v4.0 Golden 累积 | `92684c5a`/`dfe7d91b`/`606cfa00`/`26a65d1a` | 登记 Golden #2/#3/#4 + F-L3 STOP + Resource-Lifetime-Ownership 规则 + Schedule P0 | 跨技术性质样本扩充(Exception/Resource/Transaction) + STOP 样本 | 决策样本可追溯 |
| **v4.0 (CALIBRATED)** | `91e90cdb` | 校准 M1(NEED≠STOP 语义)/M2(语义预算)/M3(收敛停止规则) + Decision Replay | **规则显式化/强化**（非新能力域） | Calibration Review 实证(六问) |
| **v5.0** | `093b4e11` (08-28 04:53) | +P2 ORM行为/+P3 数据量/+P4 影响面，3 张 reference 查表，后处理挂既有维 | **C 规则细化 + D 新文档**；0 新能力域 | commit 自述 F1 0.90→0.95，**仓库无评测工件** → 未证实 |
| **v6.0-alpha** | `b3b8acde` (08-28 05:05) | +D11 Step2.5 跨类生命周期 + Cross-Class-Context-Rule.md | 新增第11维**分析视野**，但仅 Level 0 人工 | **`NO VERIFIED VERSION`**（自述 alpha，Level 2 工具"第二期开发"未落仓） |
| v5.x 中间版 | — | — | — | **NO VERIFIED VERSION**（不存在） |
| v6.0 稳定版 | — | — | — | **NO VERIFIED VERSION**（不存在，仅 -alpha 桩） |

## 时间线关键事实

1. **v4.0 用了一天多**（08-27），含 4 Golden + 3 STOP/NEED 样本 + 六问校准 → 成熟、有证据链。
2. **v5.0 与 v6.0-alpha 同一天、相隔 12 分钟**（08-28 04:53 → 05:05）连续提交 → 版本号跳跃速度远超能力沉淀速度。
3. **v6.0 之后无任何后续提交**（b3b8acde = HEAD）→ v6.0 停在 alpha，其定义性能力(Level 2 自动取证)尚未开始落仓。

## 缺口登记（NOT FOUND）

- 无 v5.0 规格 / 计划 / 评测集 / Golden（仅 v4.0 有规格+计划）。
- 无 v6.0 规格 / 计划 / 路线图文档；"R1→R4 Roslyn Correctness Gate"仅存在于会话 chat，全仓零命中。
- 无 v5.0/v6.0 的可复现验证工件（commit message 声称的指标无法在仓库复算）。
