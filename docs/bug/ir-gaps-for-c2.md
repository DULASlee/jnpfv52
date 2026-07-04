# IR 字段缺口清单 — C2 代码生成前置条件

> **日期**：2026-07-02
> **对照基准**：CodeGenService.DownloadCode 所需输入 vs 当前 `F_FinalIR` JSON 结构
> **IR 样本来源**：`docs/evals/golden-set/GOLDEN-01.json`

---

## 一、当前 IR 已有的信息

基于 `GOLDEN-01.json` 的 `expectedIR` 结构：

```json
{
  "modules": [
    { "nodeType": "ModuleDefinition", "moduleName": "学生管理" }
  ],
  "tables": [
    { "nodeType": "TableDefinition", "tableName": "Student",
      "requiredFields": ["student_no", "name", "class"] }
  ],
  "pages": {
    "nodeType": "Component", "minCount": 3,
    "descriptions": ["学生列表", "课程列表", "成绩录入"]
  },
  "dashboard": {
    "nodeType": "DashboardConfig",
    "requiredCharts": ["平均分柱状图", "排名表格"]
  }
}
```

---

## 二、对照 CodeGenService 所需输入的缺口

| # | CodeGen 所需 | IR 当前状态 | 缺口严重度 | 补全方案 |
|---|-------------|------------|-----------|----------|
| 1 | **字段类型/长度** (`fieldType`, `fieldLength`) | 仅有字段名列表 `requiredFields: ["name", "student_no"]` | 🔴 CRITICAL | IR 的 `TableDefinition` 需扩展为 `[{ fieldName, fieldType, fieldLength, isNullable, isPrimaryKey }]` |
| 2 | **表间外键关联** (`DbTableRelationModel`) | 无 | 🔴 CRITICAL | IR 需新增 `relations: [{ fromTable, fromField, toTable, toField, relationType }]` |
| 3 | **列设计** (`ColumnDesignModel`) | 无 | 🟡 HIGH | IR 需为每个 table 提供默认列配置，可从 fields 自动派生（字段名 → 列名 → 宽度 → 对齐） |
| 4 | **字典字段标记** (`DictionaryDataEntity` 绑定) | 无 | 🟡 HIGH | IR 的 field 需新增 `isDictionary: bool` 和 `dictionaryCode: string` |
| 5 | **模块父子关系** (`parentId`) | 仅有平铺模块列表 | 🟡 HIGH | IR 的 ModuleDefinition 需新增 `parentModule: string` |
| 6 | **权限操作定义** (CRUD 权限码) | 无 | 🟡 HIGH | IR 需为每个 module 新增 `operations: ["add","edit","delete","view","export"]` |
| 7 | **WebType / TemplateType** | 无 | 🟢 MEDIUM | 可从 IR 推断默认值：WebType=1 (PC), Type=1 (标准模板) |
| 8 | **分类编码** (`Category`) | 无 | 🟢 MEDIUM | AI 流水线使用固定 category 或从模块名推断 |
| 9 | **App 列设计** (`AppColumnData`) | 无 | 🟢 LOW | 移动端默认与 PC 端同列配置 |
| 10 | **表单设计** (`FormData.formField`) | 仅有字段名，无组件选择 | 🟢 MEDIUM | 可从 fieldType 推断默认组件：string→Input, int→InputNumber, date→DatePicker |

---

## 三、IR 扩展方案（C2.1 第一动作）

C2 开工前，SA 流水线的详细设计阶段产出物需扩展为以下结构：

```json
{
  "tables": [{
    "tableName": "Student",
    "comment": "学生表",
    "fields": [
      { "fieldName": "id", "fieldType": "string", "fieldLength": 36, "isPrimaryKey": true, "isNullable": false },
      { "fieldName": "name", "fieldType": "string", "fieldLength": 50, "isNullable": false },
      { "fieldName": "student_no", "fieldType": "string", "fieldLength": 20, "isNullable": false },
      { "fieldName": "class_id", "fieldType": "string", "fieldLength": 36, "isNullable": true, "isDictionary": false }
    ]
  }],
  "relations": [
    { "fromTable": "Student", "fromField": "class_id", "toTable": "Class", "toField": "id", "relationType": "ManyToOne" }
  ],
  "modules": [
    { "moduleName": "学生管理", "parentModule": null, "operations": ["add","edit","delete","view","export"] }
  ],
  "dictionaries": {
    "class_type": { "name": "班级类型", "values": ["普通班","重点班","国际班"] }
  }
}
```

---

## 四、兜底策略

如果 IR 扩展在 C2 开工时未完成，可采用以下降级方案：

| 缺口 | 降级方案 |
|------|----------|
| 字段类型缺失 | 所有字段默认 `string(200)`，主键默认 `string(36)` |
| 外键缺失 | 不生成 Navigation Property，仅生成基础 Entity |
| 列设计缺失 | 每表默认 4 列（前 4 个非主键字段），宽度 150 |
| 字典缺失 | 字段标记 `isDictionary=false`，不渲染 `jnpf-select` |
| 权限缺失 | 默认 `[AllowAnonymous]`，交付时手动补权限声明 |
| 模块父子 | 全部平铺为一级菜单 |

降级方案可让 C2 先跑通链路，IR 扩展与链路跑通可并行推进。
