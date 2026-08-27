# Evidence → Modify 准入条件（v4.0 冻结，6 要素全满足才允许）

> **来源**：Golden Example 复盘冻结检查 Q1  
> **门控语义**：缺一要素 = 禁止进入 Modify，必须走 Evidence → Stop  
> **适用**：所有类级重构，无论维度

| # | 要素 | 判定标准 | 证据位置 |
|---|------|----------|----------|
| 1 | Finding 已被证据确认 | P0 Evidence Pack 中该 Finding 有文件:行号 + 代码片段 + 影响面量化，非猜测 | P0-Evidence-Pack.md Findings 表 |
| 2 | 属于明确 Contract violation 或已批准修复类别 | 命中 JNPF 铁律（N1/N2/N3/N4）或扫描清单中已定级为 P0/P1 且在风险矩阵中标记为“P0 立即 / P1 本迭代” | Risk-Matrix |
| 3 | Fix Boundary 可单点定义 | 能用一句话界定单类单点（如 `EmailService.Delete catch 吞栈`），无需跨类/跨模块 | Decision → Fix Boundary |
| 4 | 风险门控通过 | Risk/Performance/Complexity 三门控已评估且均未触发“no-go / 过度架构” | Risk Matrix + Gate + Budget |
| 5 | 回归验证路径存在 | 存在可重复的验证：build + 行为特征（或单测）+ 架构测试，或明确的运行证据采集路径 | P0.4 Test Facts + 验证计划 |
| 6 | 不扩大公共 Contract | 修改后对外错误码/状态码/接口签名不变，调用方无需改；不新增公共方法/参数/配置项 | 代码 diff + 接口契约 |

**判定式**：`AllowModify = 1∧2∧3∧4∧5∧6`  
**执行**：任一要素为假 → 记为 `Decision = 暂不实施（Evidence → Stop）`，不得以“经验补全”绕过。
