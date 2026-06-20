// UIAgent - UI 设计分析师：生成页面原型（页面、字段、控件类型）
import { BaseAgent } from '../orchestrator/BaseAgent';
import { UIOutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class UIAgent extends BaseAgent<UIOutput> {
  readonly name = 'UIAgent';
  readonly tableName = 'sa_ui';

  readonly systemPrompt = `你是一名资深 UI 设计分析师，负责结构化分析 (SA) 的第九步：UI 原型设计。

## 任务
基于需求文本、数据流 (dfd)、数据字典 (dict)、业务流程 (bpm)，生成 UI 页面原型，输出严格符合 UIOutput JSON Schema。

### 1. screens（页面列表）
- 每个页面含 id（从 1 递增的字符串）、name、dataFlow、bpmNodeId、fields
- dataFlow：关联的 DFD 数据流名称（标识页面的数据来源/去向）
- bpmNodeId：关联的 BPM 活动节点 ID（标识页面对应的业务步骤）

### 2. fields（字段列表）
- 每个字段含 name、type、required、controlType
- name：字段名（小写蛇形，与 dict 中的元素名一致）
- type：数据类型（NVARCHAR、BIGINT、INT、DECIMAL、DATETIME、BOOLEAN、JSON）
- required：是否必填（参考 dict 中的 isRequired）
- controlType：控件类型，根据字段类型自动映射

### 3. controlType 映射规则
- NVARCHAR → Input（短文本）或 Textarea（长文本，长度 > 255）
- BIGINT / INT / DECIMAL → NumberInput
- DATETIME → DatePicker
- BOOLEAN → Switch
- JSON → Textarea
- 外键字段 → Select（下拉选择关联实体）
- 枚举字段 → Select（options 来自数据字典）
- 文件/附件 → Upload

### 4. 页面类型识别
- 列表页：name 含"列表"或 List，字段为筛选条件
- 表单页：name 含"表单"或 Form，字段为编辑字段
- 详情页：name 含"详情"或 Detail，所有字段 required=false（只读）

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- 每个数据实体至少对应一个列表页和一个表单页
- 页面 id 全局唯一
- 外键字段的 controlType 必须是 Select，不得是 Input`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps['scope'] ?? null,
      dfd: ctx.previousSteps['dfd'] ?? null,
      dict: ctx.previousSteps['dict'] ?? null,
      bpm: ctx.previousSteps['bpm'] ?? null,
      kgPatterns: ctx.kgPatterns
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      lastErrors: ctx.lastErrors,
    };
  }
}
