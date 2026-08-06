# ADR — 前端专科质量五柜选型

> **类型**：架构决策记录（ADR）  
> **状态**：Accepted（2026-08-06）  
> **范围**：`jnpf-web-vue3`（PC；大屏/移动复用方法，不合并工程）  
> **关联**：[`design-quality-diagnostics.md`](design-quality-diagnostics.md) · [`design-quality-frontend-ct-report.md`](design-quality-frontend-ct-report.md)  
> **证据**：`.claude/evidence/frontend-ct/cab{1..4}-*`

---

## 1. 背景

全量「万能扫描」（单一 Sonar / 行数榜）会淹没业务热点。需要按专科分柜：架构、复杂度、组件契约、运行时内存，再以 ADR 固定选型，避免每轮会话重新选型。

## 2. 决策摘要

| 柜 | 主工具 | 辅工具 | 门禁姿态 |
|----|--------|--------|----------|
| 1 架构 | **dependency-cruiser** | **Knip**（files） | 报告优先；`no-circular` 记基线，不立刻 fail CI |
| 2 复杂度 | **eslint-plugin-sonarjs**（认知复杂度） | **vue-mess-detector**（圈复杂度 / Vue 异味） | 双榜对照，禁止只认一个数字 |
| 3 组件 | **vue-component-meta** | Knip 未引用 `.vue` 抽样 | 抽样契约；删除前必须核对动态路由 |
| 4 内存 | **fuite** + Chrome | R6 Hook / DevTools | 场景脚本；需 `:3100` + Chrome |
| 5 决策 | 本文 ADR | CATALOG / package scripts | 变更选型先改 ADR |

## 3. 备选与取舍

### 3.1 架构柜

| 方案 | 结论 |
|------|------|
| A. dependency-cruiser + Knip | **采纳**。环/分层规则可编码；Knip 补死文件 |
| B. madge only | 拒绝作主工具（规则表达弱） |
| C. skott | 可选增强，不阻塞 |

**实测基线（cab1）**

- dependency-cruiser：`1418` modules / `4965` deps；`1174` violations（`854` error / `320` warn）
- `no-circular`：`854` 行，涉及 **314** 个唯一模块；枢纽含 `Jnpf/index.ts`、`router/routes`、`dynamicModel/list`、`FlowParser`、`AiChatPanel`
- Knip（`include: files`，8GB 堆）：**592** unused files（其中 `.vue` 约 443 行命中）——**含动态路由假阳性**，禁止批量删

配置：`jnpf-web-vue3/.dependency-cruiser.cjs` · `knip.json` · `depcruise-webpack.resolve.cjs`

### 3.2 复杂度柜

| 方案 | 结论 |
|------|------|
| A. SonarJS cognitive + VMD cyclomatic | **采纳**。认知 vs 圈复杂度互补 |
| B. 仅 VMD | 不足（无 eslint 集成路径） |
| C. 仅 SonarJS recommended 全开当 CI | 拒绝（噪音过大） |

**实测基线（cab2，热点目录 223 文件）**

| 文件 | SonarJS 认知 CC（函数） |
|------|-------------------------|
| `dynamicModel/list/Form.vue` | 140 |
| `dynamicModel/list/index.vue` | 119 |
| `dynamicModel/list/CustomForm.vue` | 112 |
| `AiChatPanel.vue` | 85 / 72 |
| `InputTable.vue` | 57 / 50 |

与 VMD 对照原则：**按文件交叉排名，禁止直接比绝对数值**。

配置：`.eslintrc.complexity.cjs` · `scripts/quality/cab2-sonarjs-run.cjs`

### 3.3 组件柜

| 方案 | 结论 |
|------|------|
| A. vue-component-meta + Knip `.vue` | **采纳**。公开 npm 无稳定「vue-unused」主包 |
| B. 自研未用组件扫描 | 暂缓（动态 `import()` / 菜单路由假阳高） |

**实测（cab3）**

| 组件 | props | events |
|------|-------|--------|
| AiChatPanel | 9 | 3 |
| InputTable | 12 | 2 |
| BasicForm | 43 | 0 |

脚本：`scripts/quality/cab3-component-meta.cjs`

### 3.4 内存柜

| 方案 | 结论 |
|------|------|
| A. fuite 场景 + R6 清单 | **采纳** |
| B. 仅静态 grep EventSource | 不足（需堆快照） |

说明：`fuite` 依赖 Puppeteer/Chrome；本机 Chrome 可用时设 `PUPPETEER_EXECUTABLE_PATH`。前端未启动时只落 runbook，不伪造泄漏结论。

脚本：`scripts/quality/cab4-fuite-scenario.cjs`（`--run` 才真正测）

### 3.5 明确不选

- 三前端合成单一 Vite 工程（UI 库 / UniApp 冲突）
- 日常开发全进 Docker Desktop 做前端热更
- 用「全仓 Sonar 一次红」替代五柜基线

## 4. 验收命令（可复现）

```powershell
cd D:\JNPF-v52\jnpf-web-vue3
pnpm quality:arch
pnpm quality:knip
pnpm quality:complexity
pnpm quality:components
pnpm quality:memory
# 前端已起且需堆快照时：
# $env:PUPPETEER_EXECUTABLE_PATH='C:\Program Files\Google\Chrome\Application\chrome.exe'
# pnpm quality:memory:run
```

证据目录：`.claude/evidence/frontend-ct/`

## 5. 后果与后续

| 项 | 动作 |
|----|------|
| CI | 先 advisory 报告；环依赖枢纽拆完后再考虑 `no-circular` error |
| 整改顺序 | 仍遵守诊断手册：`业务核心度 × commits × 复杂度` |
| 假阳性 | Knip / views 深依赖规则不得直接驱动删除 |
| 文档 | CT 报告 L2「受阻」状态改为「已配置」 |

## 6. 失败边界

- Knip OOM → 保持 `include: ["files"]` + `NODE_OPTIONS=8192`，禁止无脑扩 `exports/types`
- ESLint Windows 参数过长 → 必须走 `cab2-sonarjs-run.cjs` Node API
- fuite 无 Chrome / 未登录 → 只更新 runbook，不声称「无泄漏」
