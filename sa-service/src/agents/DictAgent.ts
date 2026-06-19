// DictAgent - 数据字典分析师：生成字段定义、数据流、数据存储
import { BaseAgent } from "../orchestrator/BaseAgent";
import {
  DictOutput,
  SAContext,
  ILLMClient,
} from "../orchestrator/orchestrator-types";

export class DictAgent extends BaseAgent<DictOutput> {
  readonly name = "DictAgent";
  readonly tableName = "sa_data_dictionary";

  readonly systemPrompt = `你是一名资深数据字典分析师，负责结构化分析 (SA) 的第四步：数据字典设计。

## 任务
基于需求文本、范围界定 (scope)、数据流图 (dfd)，生成完整的数据字典，输出严格符合 DictOutput JSON Schema。

### 1. elements（字段定义）
- 每个字段含 name（小写蛇形）、type、isFK、refEntity、isRequired
- 类型白名单：NVARCHAR、BIGINT、INT、DECIMAL、DATETIME、BOOLEAN、JSON
- 外键字段 MUST 设置 isFK=true 并指定 refEntity
- 主键统一命名为 id，类型 BIGINT，isRequired=true

### 2. dataFlows（数据流字段明细）
- 来源于 DFD 中的 dataFlows，每个数据流展开其包含的字段列表
- 字段名和类型必须在 elements 中有对应定义

### 3. dataStores（数据存储字段明细）
- 来源于 DFD 中的 dataStores，每个存储展开其字段列表
- 每个 dataStore MUST 包含审计字段：created_at(DATETIME)、created_by(NVARCHAR)、updated_at(DATETIME)、updated_by(NVARCHAR)
- 每个 dataStore MUST 包含 tenant_id(BIGINT) 字段，用于多租户隔离

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- 遵循 KG patterns 中 field_naming 类型的命名规范
- 复用 domainModel.standardFields 中已有的标准字段定义
- 字段名长度不超过 64 字符，NVARCHAR 必须指定长度如 NVARCHAR(255)`;

  constructor(llm: ILLMClient) {
    super(llm);
  }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps["scope"] ?? null,
      dfd: ctx.previousSteps["dfd"] ?? null,
      kgPatterns: ctx.kgPatterns
        .filter((p) => p.type === "field_naming")
        .map((p) => ({ type: p.type, content: p.content, score: p.score })),
      domainModel: {
        industry: ctx.domainModel.industry,
        standardFields: ctx.domainModel.standardFields,
      },
      lastErrors: ctx.lastErrors,
    };
  }
}
