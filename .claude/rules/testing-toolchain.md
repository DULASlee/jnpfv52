# 测试工具链 — AI 模型执行手册

> **读者：** 所有在此项目中工作的 AI 模型（Claude、Codex、Gemini 等）。
> **定位：** 本文档是"怎么用"的执行手册。配套的"用什么"速查表见 `.cursor/rules/testing-toolchain.mdc`。
> **更新：** 2026-07-06 Phase B 全保真日志 + RAG 记忆 + Pact + k6/artillery + xUnit 迁移。

---

## 铁律（违反 = 验收不通过）

1. **无浏览器验证。** 后端/API/Skill 验证 MUST 用 `jnpf-api.mjs` 或 `pnpm test:api`，禁止手点浏览器登录。
2. **快测优先。** 已有 pipeline 的断言 MUST 先 `pnpm test:api`（~10s），禁止默认跑慢速 mjs 全链。
3. **改 prompt = 跑 promptfoo。** SA Agent 提示词修改后 MUST 跑 `npx promptfoo@latest eval`。
4. **改 UI = 跑 Playwright。** 前端 .vue/.ts 文件变更 MUST 产出 E1 截图证据。
5. **Bug 先抓数据，不准猜。** 同一问题 2 次修复无效 → 停止改代码，用全保真日志或 jnpf-api.mjs 抓运行时数据。

---

## 一、日常 Dev Loop（每次代码变更后，顺序不可颠倒）

```
你改完代码 →
  Step 1: dotnet build                    （编译，30s-2min）
  Step 2: node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser  （API 存活，5s）
  Step 3: $env:E2E_PIPELINE_ID="311"; pnpm test:api             （快断言，10s）
  Step 4: （仅改前端时） pnpm e2e:studio:gate                    （UI 验证，3min）
  Step 5: （仅改 prompt 时） npx promptfoo@latest eval           （LLM 回归，分钟级）
```

**三步全绿 = 继续写代码。任何一步红 = 停下来用下方场景指南排查。**

---

## 二、场景驱动速查

### 场景 A：后端 API / Skill / IR 开发

```
MUST 跑（每次）:
  dotnet build
  node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser
  E2E_PIPELINE_ID=311 pnpm test:api

按需跑:
  pnpm sync:http-env                    # REST Client 手工探 API
  node scripts/phase-sup-s2-e2e.mjs verify  # 长链 evidence（分钟级）
  dotnet test backend/tests/JNPF.Tests.Gate --filter StudioApiSmokeTests
  dotnet test backend/tests/JNPF.Tests.PhaseB  # xUnit 适配层（dotnet test 可发现 [Fact]）

禁止:
  ❌ 仅 dotnet build 就声称完成
  ❌ 手点浏览器登录调 API
  ❌ 为同一断言新建 .mjs
```

### 场景 B：前端 UI 变更

```
MUST 跑（每次）:
  pnpm type-check                       # studio 默认；legacy 用 type-check:full
  pnpm e2e:studio:layout               # ~30s 页面控件验证
  产出截图到 .claude/evidence/          # E1 证据

按需跑:
  pnpm e2e:studio:gate                 # ~3min 门控 UI
  $env:E2E_PIPELINE_ID="311"; pnpm e2e:studio:deliverables  # 交付物下载
  cd jnpf-web-vue3 && pnpm test:browser  # Vitest Browser 组件测试

选择器铁律:
  ✅ page.getByTestId('submit-requirement-send-btn')   # data-testid 最稳定
  ✅ page.getByRole('button', { name: '发送' })        # 语义化
  ✅ page.getByPlaceholder('请输入需求...')             # 文案驱动
  ❌ page.locator('.input-bar .send-btn')              # CSS class — UI 改了就挂

data-testid 清单（已部署）:
  login-account-input / login-password-input / login-submit-btn
  submit-requirement-textarea / submit-requirement-send-btn
  submit-requirement-attach-btn / chat-stream
  panel-left / panel-right
```

### 场景 C：SA Agent 提示词修改

```
MUST 跑:
  npx promptfoo@latest eval -c promptfoo/promptfooconfig.yaml

按 Agent 过滤:
  npx promptfoo@latest eval --filter ScopeAgent
  npx promptfoo@latest eval --filter UIAgent
  npx promptfoo@latest eval --filter DFDAgent
  # ... 共 9 个 Agent 全覆盖

查看结果:
  npx promptfoo@latest view

需要 sa-service 运行在 :3001。
如果只改了单个 Agent → 可只跑那个 Agent 的 suite（--filter）。
```

### 场景 D：Bug / 测试失败 / 异常行为

```
第一步：不是改代码，是抓数据。

根据症状选择采集工具：

| 症状 | 工具 | 命令 |
|------|------|------|
| 前端无响应/白屏 | full-fidelity-debug | node scripts/lib/full-fidelity-debug.mjs --login --url=http://localhost:3100/#/... |
| 快速录 GIF | visual-debug | node scripts/lib/visual-debug.mjs --login --url=... --duration=15 |
| API 返回异常 | jnpf-api-cli | node scripts/jnpf-api.mjs POST /api/xxx '{...}' |
| 不确定是否老问题 | mistake-rag | node scripts/lib/mistake-rag.mjs "错误关键词" |
| 需要完整网络追踪 | full-fidelity-debug | 采集 HAR + DOM 快照 + Console + 截图 |
| 测试输出报错 | mistake-rag --file | node scripts/lib/mistake-rag.mjs --file=test-output.txt |

第二步：RAG 匹配历史修复方案
  node scripts/lib/mistake-rag.mjs "具体错误信息"
  # 或从文件/管道读入
  cat error.log | node scripts/lib/mistake-rag.mjs --stdin
  node scripts/lib/mistake-rag.mjs --json "ReferenceError"  # JSON 输出供 Agent 消费

第三步：修复 → 回 Dev Loop 验证
  修复后 MUST 重跑 Step 1-3 确认症状消失。

如果 2 次修复仍无效 → 强制切 data-driven-debug，不准再猜。
```

### 场景 E：提 PR / 合并前

```
MUST 全部通过:
  dotnet build                           # 后端编译 0 error
  pnpm type-check                        # 前端类型 0 error
  E2E_PIPELINE_ID=311 pnpm test:api      # 快断言 PASS
  pnpm e2e:studio:gate                   # UI 门控 PASS

如果改了 SA Agent 提示词，加:
  npx promptfoo@latest eval -c promptfoo/promptfooconfig.yaml

如果改了 API 响应结构，加:
  cd tests/contract && npm test          # Pact 合约验证

CI 自动执行（PR 触发）:
  - 后端 build + test + 分析器 + 漏洞扫描
  - 前端 build + lint + type-check + unit test
  - promptfoo LLM 回归（ScopeAgent + UIAgent smoke）
  - k6 性能冒烟（1 VU 验证响应 < 3s）
```

### 场景 F：怀疑性能退化

```
k6 三档:
  k6 run --env SMOKE=1 scripts/load/studio-pipeline.js     # 冒烟: 1 VU × 1 次, p95<3s
  k6 run scripts/load/studio-pipeline.js                   # 负载: 10 VU × 30s, p95<5s
  k6 run --env STRESS=1 scripts/load/studio-pipeline.js    # 压力: 50 VU × 60s, p99<15s

artillery (声明式):
  npx artillery@latest run scripts/load/studio-gate.yml
  npx artillery@latest run -o report.json scripts/load/studio-gate.yml
  npx artillery@latest report report.json
```

### 场景 G：API 合约变更

```
cd tests/contract && npm test

合约覆盖:
  POST /api/studio/pipeline/execute   → 创建流水线
  GET  /api/studio/pipeline/execute/:id/deliverables → 交付物列表
  GET  /api/studio/pipeline/execute/:id/events       → IR 事件
  GET  /api/oauth/CurrentUser         → 认证冒烟

依赖: npm install（首次），后端需运行在 :5000
```

---

## 三、Phase B 新增工具详解

### 3.1 全保真日志 (`full-fidelity-debug.mjs`)

比 visual-debug 更强大——不做 GIF/视频，聚焦结构化诊断数据，**Agent 可以不重跑就分析失败原因**。

```
# 带登录录制 30 秒
node scripts/lib/full-fidelity-debug.mjs --login --url=http://localhost:3100/#/studio/ai/submit-requirement --duration=30

# CI 模式（仅错误时输出）
node scripts/lib/full-fidelity-debug.mjs --ci --url=... --duration=10

# 自定义步骤脚本
node scripts/lib/full-fidelity-debug.mjs --login --steps=my-steps.json --output=gate-debug

# 步骤脚本格式 (my-steps.json):
[
  { "action": "fill", "selector": "[data-testid='submit-requirement-textarea']", "value": "请假系统" },
  { "action": "click", "selector": "[data-testid='submit-requirement-send-btn']" },
  { "action": "wait", "ms": 5000 },
  { "action": "snapshot", "label": "after-gate" }
]

产出:
  .claude/evidence/<name>.json  — 完整诊断包（步骤链路 + Console + Network + DOM + WS）
  .claude/evidence/<name>.har  — 网络日志
  .claude/evidence/<name>.png  — 全页截图
```

### 3.2 RAG 记忆搜索 (`mistake-rag.mjs`)

测试失败时自动从 31 条历史错题中匹配修复方案。TF-IDF 关键词倒排索引，零外部依赖。

```
# 直接搜索
node scripts/lib/mistake-rag.mjs "ReferenceError is not defined"

# JSON 输出（供 Agent 自动消费）
node scripts/lib/mistake-rag.mjs --json "import type 运行时值"

# 从测试输出文件读
node scripts/lib/mistake-rag.mjs --file=test-results/output.txt

# 从管道读
cat error.log | node scripts/lib/mistake-rag.mjs --stdin
```

### 3.3 k6 负载测试 (`scripts/load/studio-pipeline.js`)

```
k6 run --env SMOKE=1 --env PIPELINE_ID=311 scripts/load/studio-pipeline.js
k6 run --env STRESS=1 scripts/load/studio-pipeline.js
```

### 3.4 artillery 负载测试 (`scripts/load/studio-gate.yml`)

```
npx artillery@latest run scripts/load/studio-gate.yml
```

### 3.5 Pact 合约测试 (`tests/contract/`)

```
cd tests/contract && npm test
cd tests/contract && npm run pact:verify
```

### 3.6 PhaseB xUnit 适配 (`backend/tests/JNPF.Tests.PhaseB/`)

```
dotnet test backend/tests/JNPF.Tests.PhaseB   # xUnit 发现 [Fact] 测试
dotnet run --project backend/tests/JNPF.Tests.PhaseB  # 旧 Runner 仍可用
```

---

## 四、工具分层全景

```
L0  编译       dotnet build / pnpm type-check
L1  快断言     pnpm test:api (Vitest, ~10s)
L2  手工探针   REST Client (.http) / jnpf-api.mjs
L3  长链       phase-sup-s2-e2e.mjs 分步 (Skill watch / evidence)
L4  UI         Playwright (pnpm e2e:studio:*)
L5  LLM 质量   promptfoo (9 Agent 全覆盖)
L6  诊断       full-fidelity-debug / visual-debug / mistake-rag
L7  合约       Pact (tests/contract/)
L8  性能       k6 / artillery (scripts/load/)
L9  数据       sqlcmd (九表审计)

你的选择顺序: L0 → L1(秒级) → 按场景选 L2-L9
```

---

## 五、禁止清单（L0 硬阻断）

| ❌ 禁止行为 | ✅ 正确做法 |
|------------|------------|
| 手点浏览器登录/API 冒烟 | `jnpf-auth.mjs` + `jnpf-api.mjs` |
| `POST /api/auth/login` | 不存在；用 `/api/oauth/Login` |
| 裸 `npx vue-tsc --noEmit` | OOM；用 `pnpm type-check` |
| 为每个 API 写新 `.mjs` 断言 | 用 Vitest 或 `.http` |
| 日常 Dev Loop 仅跑 mjs、跳过 `pnpm test:api` | 快测优先 |
| `.http` 替代 Skill 分钟级 watch | 无轮询能力 |
| 仅 `dotnet build` 声称 Skill/IR 完成 | 须 `pnpm test:api` 或 phase-sup 分步 |
| 改 prompt 不跑 promptfoo | 回归盲区 |
| sa-service 写业务库做物化 | 已迁至 C# `SaMaterializer` |
| CSS class 定位器 (`.input-bar .send-btn`) | `data-testid` 或 `getByRole` |

---

## 六、关联索引

| 文档 | 内容 |
|------|------|
| `.cursor/rules/testing-toolchain.mdc` | 工具选型矩阵（Cursor alwaysApply） |
| `.cursor/rules/auto-test-fix-loop.mdc` | L1–L5 闭环铁律 |
| `.claude/rules/testing.md` | Gate Function 5 步协议 |
| `openspec/specs/studio-e2e-toolchain/spec.md` | 分层 E2E 知识库 |
| `api-tests/README.md` | API 测试工具对照 |
| `e2e/README.md` | Playwright 按页测试 |
| `scripts/README-api-cli.md` | jnpf-api-cli 完整说明 |
| `promptfoo/promptfooconfig.yaml` | 9 Agent LLM 回归配置 |
| `scripts/load/` | k6 + artillery 负载测试 |
| `tests/contract/` | Pact 消费者契约测试 |
