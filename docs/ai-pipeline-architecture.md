# JNPF AI 流水线架构设计

> v5.2 Phase 5 | 2026-06-14

## 一、总体架构

```
用户输入 (自然语言)
       │
       ▼
┌─────────────────────────────────────────────────────────┐
│              OrchestratorAgent (A1 五阶段编排)          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐ │
│  │需求分析师 │→│  架构师   │→│ UI/UX设计 │→│ 数据库  │ │
│  └──────────┘  └──────────┘  └──────────┘  └────────┘ │
│       │              │              │             │      │
│  Requirement    Architecture    UIDesign    Database    │
│  Analysis       Design                      Design     │
└─────────────────────────────────────────────────────────┘
       │                                            │
       ▼                                            ▼
  ┌─────────┐                              ┌────────────┐
  │  DKEE   │← 人类操作观察 → 领域模式      │ 编译网关    │
  │ v1.0    │                              │ (Phase 4)  │
  └─────────┘                              └────────────┘
                                                 │
                     ┌───────────────────────────┼────...
                     ▼                           ▼
              Vue3 Web + 小程序 + H5 + 大屏 + 工作流 + ZIP
```

---

## 二、LLM 网关层

6个供应商实现，通过 `LLMGateway` 接口统一接入：

```
         ┌──────────────────────────┐
         │   FallbackLLMGateway     │  降级链
         │   1. DeepSeek V4 Pro     │
         │   2. MiMo-2.5-Pro        │
         │   3. DeepSeek 标准        │
         │   4. 通义千问 (DashScope) │
         │   5. OpenAI (GPT-4o)     │
         │   6. Ollama (本地)       │
         └──────────────────────────┘
```

**降级机制**：
- 指数退避重试（`Math.pow(2, attempt) × 1000ms` + 随机抖动）
- 连续失败3次自动切换供应商
- 成功响应后重置失败计数

**安全设计**：
- API Key 100% 从环境变量读取（`import.meta.env.VITE_*`）
- 零硬编码，零提交到Git
- 审计日志自动写入 `BASE_AI_Call_LOG` 表

---

## 三、智能体体系

### BaseAgent 基类
| 能力 | 说明 |
|:---|:---|
| `execute<T>()` | 构建Prompt → LLM调用 → 解析 → 置信度评估 |
| `executeStream()` | 流式输出 + 实时展示AI思考 |
| `parseResponse<T>()` | 3种JSON格式：纯JSON、```json```包裹、混合文本提取 |
| `evaluateConfidence()` | 基础0.7，3+字段+0.1 |

### 4个专业智能体

| 智能体 | 文件 | 核心能力 |
|:---|:---|:---|
| 需求分析师 | `requirement-analyst.ts` | 模糊需求→结构化分析 + 追问深化 |
| 架构师 | `architect.ts` | 自动注入TenantId/审计字段, UPPER_SNAKE规范化 |
| UI/UX设计师 | `ui-ux.ts` | Registry组件校验, aiHints注入, VIP检测 |
| 数据库设计师 | `database.ts` | F_前缀规范化, 索引IDX_命名, API补全 |

---

## 四、五阶段流水线状态机

```
idle → running → waiting_confirmation → completed
  │       │              │
  │       ├─ failed ─────┤
  │       │              │
  └─── expert_mode ←─────┘  (confidence < 0.6)
```

**状态转换规则**：
- 需求/架构/设计/交付：需人类确认后推进
- 开发：自动执行（不需确认）
- 置信度<60%：自动切换专家模式

---

## 五、IR输出契约

所有AI和专家模式产出的IR结构完全一致：

```typescript
interface FormPageIR {
  type: 'form';
  id: string;
  name: string;
  config: FormConfig;
  fields: FieldIR[];
  databaseFields: DatabaseFieldIR[];
  expressions: ExpressionIR[];
  aiHints?: { domain, requirement, designRationale, confidence };
}
```

IR → Schema → cleanSchema → validateIR → compiler → GeneratedProject → ZIP

---

## 六、扩展指南

### 新增LLM供应商
1. 创建 `llm/newprovider.ts` → `implements LLMGateway`
2. 在 `index.ts` 中导出
3. 在 `FallbackLLMGateway` 构造函数中添加

### 新增智能体
1. 创建 `agents/newagent.ts` → `extends BaseAgent`
2. 定义 `AgentResponse<T>` 的具体类型
3. 在 `OrchestratorAgent` 中注入

### 新增编译目标
1. `targets.ts` 中添加 `CompileTarget` 和元数据
2. 实现对应的 `Compiler`
3. `gateway.ts` 的 switch 中添加 case
