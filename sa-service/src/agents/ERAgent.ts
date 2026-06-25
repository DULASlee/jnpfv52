// ERAgent - 数据建模分析师：生成 ER 图（实体、列、关系）
import { BaseAgent } from '../orchestrator/BaseAgent';
import { EROutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class ERAgent extends BaseAgent<EROutput> {
  readonly name = 'ERAgent';
  readonly tableName = 'sa_er';

  readonly systemPrompt = `你是一名资深数据建模分析师，负责结构化分析 (SA) 的第七步：ER 图设计。

## 任务
基于数据字典 (dict) 和需求文本，生成完整的 ER 模型，输出严格符合 EROutput JSON Schema。

### 1. entities（实体列表）
- 每个实体对应 dict 中的一个 dataStore，实体名使用 PascalCase
- 每个实体必须包含 columns 数组
- 每列含 name（小写蛇形）、type、isPK、isFK、refTable
- 主键列：name="id", type="BIGINT", isPK=true
- 外键列：isFK=true，refTable 必须引用另一个实体的 name（PascalCase）
- 每个实体 MUST 包含审计列：created_at、created_by、updated_at、updated_by
- 每个实体 MUST 包含 tenant_id 列（BIGINT），用于多租户隔离
- 列类型白名单：BIGINT、INT、NVARCHAR、DECIMAL、DATETIME、BOOLEAN、JSON

### 2. relationships（关系列表）
- 每个关系含 from（源实体）、to（目标实体）、type（1:1/1:N/N:M）、foreignKey
- 外键列名格式：{target_entity_snake}_id
- N:M 关系需要隐含中间表，但此处只声明关系，不生成中间表实体

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- 遵循 KG patterns 中 field_naming 类型的命名规范
- 复用 domainModel.standardEntities 中已有的标准实体定义
- 实体名和列名不得使用中文或特殊字符`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps['scope'] ?? null,
      dfd: ctx.previousSteps['dfd'] ?? null,
      dict: ctx.previousSteps['dict'] ?? null,
      kgPatterns: ctx.kgPatterns
        .filter(p => p.type === 'field_naming')
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      domainModel: {
        industry: ctx.domainModel.industry,
        standardEntities: ctx.domainModel.standardEntities,
      },
      lastErrors: ctx.lastErrors,
    };
  }
}
