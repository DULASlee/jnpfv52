# JNPF AI 流水线 — 开发者指南

> v5.2 Phase 5 | 2026-06-14

## 一、项目结构

```
jnpf-web-vue3/src/core/ai/
├── llm/                    # LLM 网关层
│   ├── types.ts            # LLMConfig, ChatRequest, LLMGateway 接口
│   ├── deepseek.ts         # DeepSeek 标准网关
│   ├── deepseek-v4.ts      # DeepSeek V4 Pro 变体
│   ├── tongyi.ts           # 通义千问 (DashScope)
│   ├── openai.ts           # OpenAI (GPT-4o)
│   ├── ollama.ts           # Ollama 本地模型 (非OpenAI格式)
│   ├── mimo.ts             # MiMo-2.5-Pro
│   ├── fallback.ts         # 多供应商降级网关
│   ├── prompts.ts          # 4个智能体Prompt模板
│   └── index.ts            # 统一导出
├── agents/                 # 智能体层
│   ├── base.ts             # BaseAgent 抽象基类
│   ├── requirement-analyst.ts
│   ├── architect.ts        # 含自动注入逻辑
│   ├── ui-ux.ts            # 含Registry校验
│   └── database.ts         # 含命名规范化
├── pipeline/               # 流水线编排 (A1)
│   ├── stages.ts           # 5阶段定义
│   ├── state-machine.ts    # 状态机（6状态）
│   └── orchestrator.ts     # OrchestratorAgent
├── dkee/                   # 知识进化引擎
│   └── v1.ts               # observe/persist/recall
├── rules/                  # 业务规则引擎
│   └── engine.ts           # 决策表/决策树/规则链
├── integration/            # 编译桥接
│   ├── compile-bridge.ts   # AI IR → compileGateway
│   └── use-compile.ts      # Vue3 composable
├── expert-mode.ts          # 无AI专家模式
├── evals/                  # 评估基准
│   └── golden-set.ts       # 15条核心测试用例
└── __tests__/              # 测试（10个文件）
    ├── mock-llm.ts
    ├── prompts.test.ts
    ├── agents.test.ts
    ├── database.test.ts
    ├── ui-ux.test.ts
    ├── pipeline.test.ts
    ├── dkee.test.ts
    ├── compile-bridge.test.ts
    ├── e2e-pipeline.test.ts
    ├── cross-domain.test.ts
    └── multitenancy-readiness.test.ts
```

---

## 二、运行测试

```bash
cd jnpf-web-vue3

# 全量测试
npx vitest run

# 仅AI模块
npx vitest run src/core/ai/

# 单个文件
npx vitest run src/core/ai/__tests__/pipeline.test.ts

# ESLint
npx eslint src/core/ai/ --max-warnings 0

# 类型检查
npx vue-tsc --noEmit 2>&1 | grep "src/core/ai/"
```

---

## 三、添加新的领域模式

在 `evals/golden-set.ts` 中追加：

```typescript
{
  id: 'domain-xx',
  domain: '你的领域',
  input: '自然语言需求描述',
  expectedEntities: ['实体1', '实体2'],
  expectedFields: ['字段1', '字段2'],
  expectedRules: ['规则1'],
  expectedTableNames: ['BASE_TABLE1']
}
```

---

## 四、DKEE 知识图谱扩展

```typescript
// 观察人类操作
const pattern = observeAndExtract(humanActions, '制造');

// 持久化
if (pattern) persistPattern(pattern);

// 召回（AI执行前）
const patterns = recallPatterns('制造');
// patterns可用于AgentContext.domains
```

---

## 五、MultiTenancy 启用

参见 `docs/multitenancy-impact-analysis.md` 的4阶段启用计划：

1. Week 1：创建 BASE_TENANT 表 + 回填默认 TenantId
2. Week 2：逐模块激活 ITenantFilter
3. Week 3-4：Dapper 查询修复 + Base 模块
4. Week 5：越权测试验证

Phase 5 新增代码已100%租户就绪：
- AiCallLogEntity 含 F_TENANT_ID
- 架构师/数据库智能体强制注入 TenantId
- 所有生成表含租户隔离字段

---

## 六、安全注意事项

1. **API Key**：仅从 `import.meta.env.VITE_*` 读取，禁止硬编码
2. **LLM 输出**：`parseResponse` 自动处理3种格式，不信任纯JSON
3. **审计**：所有LLM调用记录到 `BASE_AI_Call_LOG` 表（请求/响应截断200字）
4. **多租户**：`injectAuditFields` 在 `design()` 返回前自动执行（非事后补丁）
5. **IR 校验**：编译网关的 `validateIR` 在编译前拦截不合格IR
