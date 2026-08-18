# SA SDK - 后端调用入口

完整的 SA 流水线 SDK,后端只需 3 行就能用。

## 文件结构

```
sa-sdk/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts                          # 主入口(导出所有)
│   ├── types.ts                          # 类型定义
│   ├── types-validator.ts                # Validator 类型
│   ├── orchestrator/
│   │   ├── SAOrchestrator.ts             # ★ 主入口
│   │   ├── StepRouter.ts                 # 6 问清单路由
│   │   └── RetryLoop.ts                  # 生成→验证→自修复
│   ├── agents/
│   │   ├── BaseAgent.ts                  # Agent 基类
│   │   └── index.ts                      # 9 个 Agent 实现
│   ├── persistence/
│   │   └── SADatabase.ts                 # DB 接口 + 内存实现
│   └── dkee/
│       └── index.ts                      # DKEE 提炼 + Pattern 评分
└── __tests__/
    └── SAOrchestrator.test.ts           # 集成测试
```

## 后端 3 行接入

```typescript
import { SAOrchestrator, InMemorySADatabase, ILLMClient } from 'sa-sdk';
import { DictValidator, DFDValidator, UIValidator, ... } from 'sa-validators';

// 1. 实现 LLM 客户端(任何 LLM 都能接入)
const llm: ILLMClient = {
  async generate({ systemPrompt, context, lastErrors }) {
    const response = await openai.chat({
      model: 'gpt-4',
      messages: [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: JSON.stringify({ context, lastErrors }) },
      ],
    });
    return JSON.parse(response.choices[0].message.content);
  },
};

// 2. 实现 DB(SQL Server / PostgreSQL / 内存都行)
const db = new SqlServerSADatabase(connectionString);  // 生产实现
// const db = new InMemorySADatabase();               // 测试用

// 3. 注入 7 个 Validator
const orchestrator = new SAOrchestrator(llm, db, {
  DFDValidator: new DFDValidator(),
  BPMValidator: new BPMValidator(),
  DictValidator: new DictValidator(),
  LogicValidator: new LogicValidator(),
  CrossEventConsistencyValidator: new CrossEventConsistencyValidator(),
  ERValidator: new ERValidator(),
  UIValidator: new UIValidator(),
});

// 4. 调用 - 跑完整 9 步 SA 流水线
const result = await orchestrator.runSA({
  tenantId: 'tenant_001',
  projectId: 1001,
  requirementId: 5001,
  requirementText: '我们要建 MES 报工系统,机加工车间,工单报工,物料消耗',
  userId: 'user_123',
});

console.log(result.scope);
console.log(result.dfd);
console.log(result.dict);
console.log(result.decisionTable);
```

## 核心机制

### 1. 三级分层路由(StepRouter)

```typescript
const decision = decideSteps('complex', hasStateChange);
// complex 事件跑全部 9 步,simple 只跑 UI
```

### 2. 生成→验证→自修复(RetryLoop)

每个步骤最多重试 5 次,错误回灌到 LLM:

```typescript
// 第 1 次:LLM 输出 ProductName 字段
// Validator 报 DICT_INVALID_FIELD
// 错误塞回 ctx.lastErrors
// 第 2 次:LLM 看到错误,删除 ProductName,输出正确数据
// Validator 通过
```

### 3. 强外键链

数据库写入按 9 步顺序,前面的步骤不写入,后面步骤无法写入(强外键约束):

```
sa_scope → sa_dfd → sa_business_process → sa_data_dictionary → sa_pspec → sa_decision_table → sa_er / sa_state_machine / sa_ui
```

### 4. DKEE 沉淀

SA 流水线跑完后,DKEE 自动从 PASS+human_confirmed 记录提炼 Pattern,按评分公式入库:

```
score = 0.30 * log(1+usage_count)
      + 0.25 * success_rate
      + 0.20 * source_weight
      + 0.15 * log(1+cross_industry_count)
      + 0.10 * recency_score
```

## 与 SQL 表的对接

SDK 写入的字段直接对应 `sa_schema.sql` 里的 10 张表。

生产环境需要实现 `SqlServerSADatabase`,参照 `InMemorySADatabase` 的接口即可。

## 测试

```bash
cd sa-sdk
npm install
npm test
```

## 配置项

```typescript
const config: SAConfig = {
  maxRetries: 5,            // 每步最多重试 5 次
  retryDelayMs: 1000,       // 重试间隔
  enableDKEE: true,         // 是否跑 DKEE 提炼
  enableCrossEventCheck: true,  // 是否做判定表跨事件一致性
  logLevel: 'info',
};
```

## 错误处理

如果某步 5 次重试都失败,SDK 抛出 Error:

```
Error: [sa_data_dictionary] 失败 5 次仍未收敛。最后错误:
  [DICT_INVALID_FIELD] 字段 "X" 是 LLM 幻觉
  [DICT_INVALID_TYPE] 字段 "Y" 类型 "Z" 不在白名单
```

**生产环境**:捕获后写入 `sa_validation_log`,业务方可以人工 review 后重新触发。
