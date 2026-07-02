# IR Field Extension Design for C2 Code Generation

> **Date**: 2026-07-02
> **Status**: Draft
> **Consumes**: `docs/bug/ir-gaps-for-c2.md` (10 gaps identified)
> **Produces**: JSON Schema extensions for `GeneratedProjectEntity.F_FinalIR`, classified by C2 phase severity
> **Adapter companion**: `docs/superpowers/specs/2026-07-02-c2-ir-adapter-design.md`

---

## 1. Current IR Schema (baseline)

The IR is stored as a JSON string in `GeneratedProjectEntity.F_FinalIR` (`BASE_AI_GENERATED_PROJECT.F_FinalIR`). Current structure derived from golden test data (`GOLDEN-01.json` expectedIR):

```json
{
  "modules": [
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "学生管理"
    }
  ],
  "tables": [
    {
      "nodeType": "TableDefinition",
      "tableName": "Student",
      "requiredFields": ["student_no", "name", "class"]
    }
  ],
  "pages": {
    "nodeType": "Component",
    "minCount": 3,
    "descriptions": ["学生列表", "课程列表", "成绩录入"]
  },
  "dashboard": {
    "nodeType": "DashboardConfig",
    "requiredCharts": ["平均分柱状图", "排名表格"]
  }
}
```

**Limitations:**
- `requiredFields` is a flat string array with no type, length, or constraint metadata
- No table relationships (foreign keys, join tables)
- No column layout or dictionary bindings
- Modules are flat with no parent-child hierarchy
- No permission operation declarations
- No form component mapping (field type to UI component)

---

## 2. Required Extensions

### 2.1 Field Type/Length Extension (CRITICAL — MUST-FIX before C2.1)

**Current**: `requiredFields: ["student_no", "name", "class"]`

**Problem**: CodeGenService requires `fieldType`, `fieldLength`, `isPrimaryKey`, and `isNullable` for every field to generate Entity property declarations and database column definitions. Without these, the generated code defaults to `string(200)` for everything, producing incorrect DDL.

**Extension**: Replace `requiredFields: string[]` with `fields: FieldDefinition[]` inside `TableDefinition`:

```json
{
  "nodeType": "TableDefinition",
  "tableName": "Student",
  "comment": "学生表",
  "fields": [
    {
      "fieldName": "id",
      "fieldType": "string",
      "fieldLength": 36,
      "isPrimaryKey": true,
      "isNullable": false,
      "comment": "主键"
    },
    {
      "fieldName": "name",
      "fieldType": "string",
      "fieldLength": 50,
      "isNullable": false,
      "comment": "姓名"
    },
    {
      "fieldName": "student_no",
      "fieldType": "string",
      "fieldLength": 20,
      "isNullable": false,
      "comment": "学号"
    },
    {
      "fieldName": "class_id",
      "fieldType": "string",
      "fieldLength": 36,
      "isNullable": true,
      "comment": "班级ID"
    },
    {
      "fieldName": "age",
      "fieldType": "int",
      "fieldLength": null,
      "isPrimaryKey": false,
      "isNullable": true,
      "comment": "年龄"
    },
    {
      "fieldName": "enrolled_at",
      "fieldType": "datetime",
      "fieldLength": null,
      "isPrimaryKey": false,
      "isNullable": true,
      "comment": "入学时间"
    }
  ]
}
```

**JSON Schema fragment:**

```json
{
  "FieldDefinition": {
    "type": "object",
    "required": ["fieldName", "fieldType", "isNullable"],
    "properties": {
      "fieldName": { "type": "string", "description": "字段名, camelCase, e.g. studentNo" },
      "fieldType": {
        "type": "string",
        "enum": ["string", "int", "long", "decimal", "double", "bool", "datetime", "guid", "text"],
        "description": "C# 数据类型"
      },
      "fieldLength": {
        "type": ["integer", "null"],
        "description": "字段长度. null 表示不适用(int/long/datetime/bool), 或取默认值(string=200, decimal=18,2)"
      },
      "isPrimaryKey": { "type": "boolean", "default": false },
      "isNullable": { "type": "boolean", "default": true },
      "comment": { "type": "string", "description": "字段注释, 自动映射为 SugarColumn(ColumnDescription)" },
      "defaultValue": { "type": "string", "description": "可选的数据库默认值, e.g. 'getdate()'" }
    }
  }
}
```

**FieldType-to-DDL mapping table:**

| fieldType    | C# Type       | SqlSugar Mapping          | Default fieldLength |
|--------------|---------------|---------------------------|---------------------|
| string       | `string`      | `nvarchar`                | 200                 |
| int          | `int`         | `int`                     | null                |
| long         | `long`        | `bigint`                  | null                |
| decimal      | `decimal`     | `decimal(18,2)`           | 18,2 (precision,scale) |
| double       | `double`      | `float`                   | null                |
| bool         | `bool`        | `bit`                     | null                |
| datetime     | `DateTime`    | `datetime`                | null                |
| guid         | `Guid`        | `uniqueidentifier`        | null                |
| text         | `string`      | `nvarchar(max)` / `text`  | null                |

---

### 2.2 Table Relations Extension (CRITICAL — MUST-FIX before C2.1)

**Current**: No relation data exists in IR.

**Problem**: CodeGenService generates navigation properties and foreign key constraints from `DbTableRelationModel`. Without relations, generated entities lack navigation properties, and frontend detail forms cannot display related table lookups.

**Extension**: Add top-level `relations` array:

```json
{
  "relations": [
    {
      "fromTable": "Student",
      "fromField": "class_id",
      "toTable": "Class",
      "toField": "id",
      "relationType": "ManyToOne",
      "constraintName": "FK_Student_Class"
    },
    {
      "fromTable": "Course",
      "fromField": "id",
      "toTable": "Student",
      "toField": "id",
      "relationType": "ManyToMany",
      "throughTable": "StudentCourse",
      "throughFromField": "student_id",
      "throughToField": "course_id"
    }
  ]
}
```

**JSON Schema fragment:**

```json
{
  "RelationDefinition": {
    "type": "object",
    "required": ["fromTable", "fromField", "toTable", "toField", "relationType"],
    "properties": {
      "fromTable": { "type": "string", "description": "源表名 (PascalCase, 与 tableName 匹配)" },
      "fromField": { "type": "string", "description": "源表字段 (camelCase)" },
      "toTable": { "type": "string", "description": "目标表名" },
      "toField": { "type": "string", "description": "目标表字段, 通常是 id" },
      "relationType": {
        "type": "string",
        "enum": ["OneToOne", "OneToMany", "ManyToOne", "ManyToMany"],
        "description": "关系类型. ManyToMany 需要 throughTable/through* 字段"
      },
      "throughTable": {
        "type": "string",
        "description": "ManyToMany 中间表名, 仅 relationType=ManyToMany 时必需"
      },
      "throughFromField": {
        "type": "string",
        "description": "中间表中指向源表的字段, 仅 ManyToMany"
      },
      "throughToField": {
        "type": "string",
        "description": "中间表中指向目标表的字段, 仅 ManyToMany"
      },
      "constraintName": {
        "type": "string",
        "description": "外键约束名 (可选, 不指定时自动生成 FK_{fromTable}_{toTable})"
      }
    }
  }
}
```

---

### 2.3 Column Design Extension (HIGH — MUST-FIX before C2.1)

**Current**: No column design data.

**Problem**: CodeGenService consumes `ColumnDesignModel` to render the frontend list page column configuration (column widths, alignment, sortable, visible columns). Without this, the generated list page defaults to showing the first 4 fields with 150px width, which is often wrong (e.g., `text` fields shown at full width, timestamps shown at tiny widths).

**Extension**: Add optional `columnDesign` array to each `TableDefinition`. This is **derivable** from `fields` with sensible defaults but can be overridden by the AI pipeline's detailed design phase:

```json
{
  "nodeType": "TableDefinition",
  "tableName": "Student",
  "fields": [ /* ... */ ],
  "columnDesign": [
    {
      "field": "student_no",
      "width": 120,
      "align": "center",
      "sortable": true,
      "fixed": "left",
      "show": true
    },
    {
      "field": "name",
      "width": 200,
      "align": "left",
      "sortable": true,
      "fixed": null,
      "show": true
    },
    {
      "field": "class_id",
      "width": 120,
      "align": "center",
      "sortable": false,
      "fixed": null,
      "show": true,
      "isDictionary": true,
      "dictionaryCode": "class_type"
    },
    {
      "field": "age",
      "width": 80,
      "align": "right",
      "sortable": true,
      "fixed": null,
      "show": true
    },
    {
      "field": "enrolled_at",
      "width": 180,
      "align": "center",
      "sortable": true,
      "fixed": null,
      "show": true
    }
  ]
}
```

**Default derivation rule (when columnDesign is absent):**

1. Skip `isPrimaryKey` fields
2. Skip `text` type fields
3. Take first 4 non-PK fields
4. Each gets `width: 150, align: "center", sortable: true, show: true, fixed: null`
5. Timestamp fields (`created_at`, `modify_at`, `F_CreatorTime`) are placed at the end

**JSON Schema fragment:**

```json
{
  "ColumnDesign": {
    "type": "object",
    "required": ["field"],
    "properties": {
      "field": { "type": "string", "description": "对应 fieldName" },
      "width": { "type": "integer", "default": 150, "description": "列宽(px)" },
      "align": { "type": "string", "enum": ["left", "center", "right"], "default": "center" },
      "sortable": { "type": "boolean", "default": true },
      "fixed": { "type": ["string", "null"], "enum": ["left", "right", null], "default": null },
      "show": { "type": "boolean", "default": true }
    }
  }
}
```

---

### 2.4 Dictionary Field Extension (HIGH — MUST-FIX before C2.1)

**Current**: No dictionary binding metadata on fields.

**Problem**: When a field stores a dictionary code (e.g., `class_type = "1"` means "重点班"), CodeGenService needs to know to render it as a `<jnpf-select>` with dictionary options (rather than a plain `<a-input>`). Without this, dictionary fields render as text inputs, and the frontend never resolves the display values.

**Extension**: Add `isDictionary`, `dictionaryCode` to `FieldDefinition`, and add a top-level `dictionaries` object to IR:

```json
{
  "tables": [{
    "tableName": "Student",
    "fields": [
      {
        "fieldName": "class_id",
        "fieldType": "string",
        "fieldLength": 36,
        "isNullable": true,
        "isDictionary": true,
        "dictionaryCode": "CLASS_TYPE",
        "comment": "班级类型"
      }
    ]
  }],
  "dictionaries": {
    "CLASS_TYPE": {
      "name": "班级类型",
      "values": [
        { "code": "1", "label": "普通班" },
        { "code": "2", "label": "重点班" },
        { "code": "3", "label": "国际班" }
      ]
    }
  }
}
```

**JSON Schema fragment:**

```json
{
  "FieldDefinition": {
    "allOf": [
      { "$ref": "#/FieldDefinition-base" },
      {
        "properties": {
          "isDictionary": { "type": "boolean", "default": false },
          "dictionaryCode": {
            "type": "string",
            "description": "字典代码, 对应 dictionaries 对象的 key. isDictionary=true 时必需."
          }
        }
      }
    ]
  },
  "DictionaryDef": {
    "type": "object",
    "required": ["name", "values"],
    "properties": {
      "name": { "type": "string", "description": "字典名称" },
      "values": {
        "type": "array",
        "items": {
          "type": "object",
          "required": ["code", "label"],
          "properties": {
            "code": { "type": "string" },
            "label": { "type": "string" }
          }
        }
      }
    }
  }
}
```

**Integration note**: The `dictionaries` object at IR root is a **transient definition** — it defines dictionary entries inline for code generation. After code generation, the developer should migrate these to the platform's `DICTIONARY_DATA` table. The adapter layer (`IrToVisualDevAdapter`) does not write to `DICTIONARY_DATA`; it only embeds the values in the generated form/column config.

---

### 2.5 Module Hierarchy Extension (HIGH)

**Current**: Modules are a flat list with no parent-child relationship.

**Problem**: JNPF's menu system supports nested menus (parent → child). Without `parentModule`, all modules render as top-level menu items, which is incorrect for multi-level navigation structures (e.g., "教学管理" → "学生管理", "课程管理").

**Extension**: Add `parentModule` to `ModuleDefinition`:

```json
{
  "modules": [
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "教学管理",
      "parentModule": null,
      "icon": "EducationOutlined",
      "sortOrder": 1
    },
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "学生管理",
      "parentModule": "教学管理",
      "icon": "TeamOutlined",
      "sortOrder": 1
    },
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "课程管理",
      "parentModule": "教学管理",
      "icon": "BookOutlined",
      "sortOrder": 2
    }
  ]
}
```

**JSON Schema fragment:**

```json
{
  "ModuleDefinition": {
    "type": "object",
    "required": ["moduleName", "parentModule"],
    "properties": {
      "moduleName": { "type": "string" },
      "parentModule": {
        "type": ["string", "null"],
        "description": "父模块名. null = 顶层菜单. 必须匹配另一个 ModuleDefinition 的 moduleName"
      },
      "icon": { "type": "string", "default": "FileOutlined", "description": "Ant Design 图标名" },
      "sortOrder": { "type": "integer", "default": 1 },
      "nodeType": { "type": "string", "const": "ModuleDefinition" }
    }
  }
}
```

**Validation rule**: The `modules` array MUST be topologically sortable — no circular parent references. `null` parentModule defines the root level(s).

---

### 2.6 Permission Operations Extension (HIGH)

**Current**: No permission data in IR.

**Problem**: Generated APIs default to `[AllowAnonymous]` when no permission declarations exist. The AI pipeline should declare which CRUD operations each module needs, so the generated code can emit proper `[SecurityDefine("权限码")]` attributes and register permission entries in the menu system.

**Extension**: Add `operations` and `permissionPrefix` to `ModuleDefinition`:

```json
{
  "modules": [
    {
      "moduleName": "学生管理",
      "parentModule": "教学管理",
      "permissionPrefix": "Student",
      "operations": ["add", "edit", "delete", "view", "export", "import"]
    }
  ]
}
```

**Permission code generation rule:**

For each `operation` in `operations`, generate a permission code `{permissionPrefix}.{Operation}` → `Student.Add`, `Student.Edit`, etc.

**Standard operation set:**

| Operation  | Meaning          | Generated permission code | API endpoint method   |
|------------|------------------|---------------------------|-----------------------|
| `add`      | Create           | `{prefix}.Add`            | `POST`                |
| `edit`     | Update           | `{prefix}.Edit`           | `PUT`                 |
| `delete`   | Delete           | `{prefix}.Delete`         | `DELETE`              |
| `view`     | View list/detail | `{prefix}.View`           | `GET` list + detail   |
| `export`   | Export           | `{prefix}.Export`         | `GET` export endpoint  |
| `import`   | Import           | `{prefix}.Import`         | `POST` import endpoint |

**JSON Schema fragment:**

```json
{
  "ModuleDefinition": {
    "allOf": [
      { "$ref": "#/ModuleDefinition-base" },
      {
        "properties": {
          "permissionPrefix": {
            "type": "string",
            "description": "权限码前缀, 通常取 PascalCase 表名. 默认值 = moduleName 的拼音/Pascal 化."
          },
          "operations": {
            "type": "array",
            "items": {
              "type": "string",
              "enum": ["add", "edit", "delete", "view", "export", "import"]
            },
            "default": ["add", "edit", "delete", "view"],
            "description": "模块需要的 CRUD 操作集合, CodeGenService 据此生成 [SecurityDefine] 和注册菜单权限"
          }
        }
      }
    ]
  }
}
```

---

### 2.7 WebType / TemplateType Extension (MEDIUM — CAN-DEFER to C2.2)

**Current**: No web type or template type in IR.

**Problem**: CodeGenService requires `WebType` (1=PC, 2=Mobile) and `Type` (template kind). Without these, all generated output defaults to PC web type with standard template.

**Extension**: Add top-level `webConfig` block:

```json
{
  "webConfig": {
    "webType": 1,
    "templateType": 1,
    "sortCode": 1
  }
}
```

**JSON Schema fragment:**

```json
{
  "WebConfig": {
    "type": "object",
    "properties": {
      "webType": {
        "type": "integer",
        "enum": [1, 2],
        "default": 1,
        "description": "1=PC (jnpf-web-vue3), 2=Mobile (jnpf-app-vue3)"
      },
      "templateType": {
        "type": "integer",
        "enum": [1, 2, 3],
        "default": 1,
        "description": "1=标准模板, 2=表单模板, 3=流程模板"
      },
      "sortCode": { "type": "integer", "default": 1 }
    }
  }
}
```

**Defer rationale**: Default values (WebType=1, Type=1) are acceptable for C2.1. C2.2 should make these configurable from the AI pipeline's detailed design phase.

---

### 2.8 Category Extension (MEDIUM — CAN-DEFER to C2.2)

**Current**: No category code in IR.

**Problem**: `VisualDevEntity.Category` classifies the project in the platform's type system. Without it, the generated project falls into the default uncategorized bucket.

**Extension**: Add top-level `category` field:

```json
{
  "category": "custom_module",
  "categoryName": "自定义模块"
}
```

**JSON Schema fragment:**

```json
{
  "CategoryConfig": {
    "type": "object",
    "properties": {
      "category": {
        "type": "string",
        "default": "custom_module",
        "description": "VisualDev Category 分类码. 预定义值见 PlatformConsts.VisualDevCategory"
      },
      "categoryName": {
        "type": "string",
        "default": "自定义模块",
        "description": "分类显示名, 用于 UI 展示"
      }
    }
  }
}
```

**Defer rationale**: A fixed default `"custom_module"` works for C2.1. This extension provides explicit control when needed.

---

### 2.9 App Column Design Extension (LOW — CAN-DEFER indefinitely)

**Current**: No mobile column config in IR.

**Problem**: CodeGenService consumes `AppColumnData` separately for mobile app column layout. Without this, mobile view falls back to PC column config (which may have different width/visibility requirements).

**Extension**: Add optional `appColumnDesign` with same shape as `columnDesign`:

```json
{
  "tables": [{
    "tableName": "Student",
    "appColumnDesign": [ /* same schema as columnDesign */ ]
  }]
}
```

**JSON Schema fragment**: Identical to `#/ColumnDesign` (section 2.3). The intent is to allow the AI pipeline to specify a different layout for mobile vs. PC.

**Defer rationale**: Mobile-first C2 output is not in scope for C2.1 or C2.2. This extension is reserved for future mobile codegen. The adapter layer can copy `columnDesign` entries as fallback.

---

### 2.10 Form Component Extension (MEDIUM — CAN-DEFER to C2.2)

**Current**: Fields have no form component mapping.

**Problem**: CodeGenService's `FormData.formField` requires component type per field (e.g., `Input`, `InputNumber`, `DatePicker`, `Select`, `Textarea`). Without this, the adapter must infer components from `fieldType`, which works for simple types but fails for special cases (e.g., a `string` field might need `Textarea` instead of `Input`).

**Extension**: Add optional `component` to `FieldDefinition`:

```json
{
  "fields": [
    {
      "fieldName": "bio",
      "fieldType": "string",
      "fieldLength": 500,
      "isNullable": true,
      "component": "Textarea",
      "componentProps": {
        "rows": 4,
        "maxLength": 500
      }
    },
    {
      "fieldName": "enrolled_at",
      "fieldType": "datetime",
      "isNullable": true,
      "component": "DatePicker",
      "componentProps": {
        "format": "YYYY-MM-DD",
        "showTime": false
      }
    }
  ]
}
```

**JSON Schema fragment:**

```json
{
  "FieldDefinition": {
    "allOf": [
      { "$ref": "#/FieldDefinition-base" },
      {
        "properties": {
          "component": {
            "type": "string",
            "enum": ["Input", "InputNumber", "Textarea", "DatePicker", "TimePicker",
                     "Select", "Switch", "Radio", "Checkbox", "Upload", "Editor",
                     "TreeSelect", "Cascader", "Rate", "Slider", "AutoComplete"],
            "default": "Input",
            "description": "前端表单组件类型. 不指定时由 fieldType + fieldLength 推导"
          },
          "componentProps": {
            "type": "object",
            "default": {},
            "description": "组件属性, 透传给 Ant Design Vue 组件. 例如 { rows: 4, maxLength: 500 }"
          }
        }
      }
    ]
  }
}
```

**Default derivation rule (fallback when component is absent):**

| fieldType     | Default component | Notes                               |
|---------------|-------------------|-------------------------------------|
| string        | `Input`           |                                     |
| string + length ≥ 200 | `Textarea`  | Long strings default to textarea    |
| int / long    | `InputNumber`     |                                     |
| decimal/double | `InputNumber`     | Component should allow decimal input |
| bool          | `Switch`          |                                     |
| datetime      | `DatePicker`      |                                     |
| guid          | `Input`           | Hidden by default, auto-generated   |
| text          | `Textarea`        |                                     |

**Defer rationale**: The adapter layer can apply the derivation table above to produce acceptable defaults for C2.1. Explicit `component` override is a C2.2 enhancement.

---

## 3. Complete Extended IR Schema

Below is the full extended JSON structure with all 10 extensions applied to the golden-sample scenario (学生管理):

```json
{
  "version": "2.0",
  "webConfig": {
    "webType": 1,
    "templateType": 1,
    "sortCode": 1
  },
  "category": "custom_module",
  "categoryName": "自定义模块",
  "modules": [
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "教学管理",
      "parentModule": null,
      "icon": "EducationOutlined",
      "sortOrder": 1,
      "permissionPrefix": "Education",
      "operations": ["view"]
    },
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "学生管理",
      "parentModule": "教学管理",
      "icon": "TeamOutlined",
      "sortOrder": 1,
      "permissionPrefix": "Student",
      "operations": ["add", "edit", "delete", "view", "export"]
    },
    {
      "nodeType": "ModuleDefinition",
      "moduleName": "课程管理",
      "parentModule": "教学管理",
      "icon": "BookOutlined",
      "sortOrder": 2,
      "permissionPrefix": "Course",
      "operations": ["add", "edit", "delete", "view"]
    }
  ],
  "tables": [
    {
      "nodeType": "TableDefinition",
      "tableName": "Student",
      "comment": "学生表",
      "fields": [
        { "fieldName": "id", "fieldType": "guid", "fieldLength": null, "isPrimaryKey": true, "isNullable": false, "comment": "主键" },
        { "fieldName": "studentNo", "fieldType": "string", "fieldLength": 20, "isPrimaryKey": false, "isNullable": false, "comment": "学号" },
        { "fieldName": "name", "fieldType": "string", "fieldLength": 50, "isPrimaryKey": false, "isNullable": false, "comment": "姓名" },
        { "fieldName": "classId", "fieldType": "guid", "fieldLength": null, "isPrimaryKey": false, "isNullable": true, "isDictionary": true, "dictionaryCode": "CLASS_TYPE", "comment": "班级ID" },
        { "fieldName": "age", "fieldType": "int", "fieldLength": null, "isPrimaryKey": false, "isNullable": true, "comment": "年龄", "component": "InputNumber", "componentProps": { "min": 0, "max": 150 } },
        { "fieldName": "bio", "fieldType": "string", "fieldLength": 500, "isPrimaryKey": false, "isNullable": true, "comment": "简介", "component": "Textarea", "componentProps": { "rows": 4 } },
        { "fieldName": "enrolledAt", "fieldType": "datetime", "fieldLength": null, "isPrimaryKey": false, "isNullable": true, "comment": "入学时间", "component": "DatePicker" }
      ],
      "columnDesign": [
        { "field": "studentNo", "width": 120, "align": "center", "sortable": true, "show": true },
        { "field": "name", "width": 200, "align": "left", "sortable": true, "show": true },
        { "field": "classId", "width": 120, "align": "center", "sortable": false, "show": true },
        { "field": "age", "width": 80, "align": "right", "sortable": true, "show": true },
        { "field": "enrolledAt", "width": 180, "align": "center", "sortable": true, "show": true }
      ],
      "appColumnDesign": [
        { "field": "studentNo", "width": 120, "align": "center", "sortable": true, "show": true },
        { "field": "name", "width": 200, "align": "left", "sortable": true, "show": true }
      ]
    },
    {
      "nodeType": "TableDefinition",
      "tableName": "Class",
      "comment": "班级表",
      "fields": [
        { "fieldName": "id", "fieldType": "guid", "fieldLength": null, "isPrimaryKey": true, "isNullable": false, "comment": "主键" },
        { "fieldName": "className", "fieldType": "string", "fieldLength": 50, "isPrimaryKey": false, "isNullable": false, "comment": "班级名称" },
        { "fieldName": "grade", "fieldType": "string", "fieldLength": 20, "isPrimaryKey": false, "isNullable": false, "comment": "年级" }
      ],
      "columnDesign": [
        { "field": "className", "width": 200, "align": "left", "sortable": true, "show": true },
        { "field": "grade", "width": 120, "align": "center", "sortable": true, "show": true }
      ]
    },
    {
      "nodeType": "TableDefinition",
      "tableName": "Course",
      "comment": "课程表",
      "fields": [
        { "fieldName": "id", "fieldType": "guid", "fieldLength": null, "isPrimaryKey": true, "isNullable": false, "comment": "主键" },
        { "fieldName": "courseName", "fieldType": "string", "fieldLength": 100, "isPrimaryKey": false, "isNullable": false, "comment": "课程名称" },
        { "fieldName": "credits", "fieldType": "int", "fieldLength": null, "isPrimaryKey": false, "isNullable": false, "comment": "学分" }
      ],
      "columnDesign": [
        { "field": "courseName", "width": 200, "align": "left", "sortable": true, "show": true },
        { "field": "credits", "width": 80, "align": "center", "sortable": true, "show": true }
      ]
    }
  ],
  "relations": [
    {
      "fromTable": "Student",
      "fromField": "classId",
      "toTable": "Class",
      "toField": "id",
      "relationType": "ManyToOne",
      "constraintName": "FK_Student_Class"
    }
  ],
  "dictionaries": {
    "CLASS_TYPE": {
      "name": "班级类型",
      "values": [
        { "code": "1", "label": "普通班" },
        { "code": "2", "label": "重点班" },
        { "code": "3", "label": "国际班" }
      ]
    }
  },
  "pages": {
    "nodeType": "Component",
    "minCount": 3,
    "descriptions": ["学生列表", "课程列表", "成绩录入"]
  },
  "dashboard": {
    "nodeType": "DashboardConfig",
    "requiredCharts": ["平均分柱状图", "排名表格"]
  }
}
```

---

## 4. Implementation Priority Matrix

| Gap | Severity | C2 Phase | IR Extension Trigger | Adapter Fallback Available? |
|-----|----------|----------|----------------------|-----------------------------|
| **2.1 Field Type/Length** | CRITICAL | C2.1 | SA detailed design phase produces typed fields instead of name-only lists | Partial — defaults to string(200) |
| **2.2 Table Relations** | CRITICAL | C2.1 | SA detailed design phase identifies foreign keys and join tables | Yes — no navigation properties generated |
| **2.3 Column Design** | HIGH | C2.1 | Derived from fields at adapter layer; explicit override from SA detailed design | Yes — default derivation rule (section 2.3) |
| **2.4 Dictionary Field** | HIGH | C2.1 | SA field analysis detects enum-like fields | Yes — isDictionary=false, plain Input rendered |
| **2.5 Module Hierarchy** | HIGH | C2.1 | SA detailed design phase specifies parent-child | Yes — flat top-level menu |
| **2.6 Permission Operations** | HIGH | C2.1 | SA designates operations per module | Partial — defaults to [AllowAnonymous] |
| **2.7 WebType/TemplateType** | MEDIUM | C2.2 | SA pipeline configuration | Yes — fixed defaults (WebType=1, Type=1) |
| **2.8 Category** | MEDIUM | C2.2 | SA pipeline configuration | Yes — fixed default "custom_module" |
| **2.9 App Column Design** | LOW | Post-C2 | Mobile codegen requirement | Yes — fallback to columnDesign |
| **2.10 Form Component** | MEDIUM | C2.2 | SA field type analysis + special cases | Yes — derivation table (section 2.10) |

**C2.1 MUST-FIX gates** (must be implemented before C2.1 adapter work begins):

1. `F_FinalIR` JSON schema updated to support `fields: FieldDefinition[]` replacing `requiredFields: string[]`
2. `relations: RelationDefinition[]` top-level array added
3. `ModuleDefinition` extended with `parentModule` and `operations`
4. `FieldDefinition` extended with `isDictionary`, `dictionaryCode`, and `dictionaries` top-level object
5. `columnDesign` implemented (derivable at adapter layer — SA detailed design can override)
6. `webConfig` and `category` added with default fallbacks (actual override optional for C2.1)

**C2.2 enhancement gates** (can be deferred):

7. Explicit `component` and `componentProps` in FieldDefinition
8. Configurable `WebType` / `TemplateType` via SA pipeline

---

## 5. Backward Compatibility

### 5.1 Detection mechanism

The `IrToVisualDevAdapter` MUST detect whether the incoming IR is "legacy" (pre-extension) or "extended" by checking the presence of the `version` field:

```
version exists && version >= "2.0"  →  extended IR
version absent or version < "2.0"   →  legacy IR
```

### 5.2 Legacy IR handling (no extensions)

When a legacy IR is detected (or any field is absent), the adapter applies the following defaults:

| Missing element | Default behavior |
|----------------|------------------|
| `version` | Assume `"1.0"` (legacy) |
| `tables[].fields` (but `requiredFields` exists) | Convert each entry in `requiredFields` to `{ fieldName: entry, fieldType: "string", fieldLength: 200, isPrimaryKey: false, isNullable: true }` |
| `tables[].columnDesign` | Auto-derive via rule in section 2.3 |
| `relations` | Empty array — no navigation properties generated |
| `modules[].parentModule` | `null` — all modules are top-level |
| `modules[].operations` | `["add", "edit", "delete", "view"]` — default CRUD |
| `modules[].permissionPrefix` | Derived from `moduleName` via PascalCase conversion |
| `fields[].isDictionary` | `false` |
| `fields[].dictionaryCode` | `null` |
| `dictionaries` | `{}` |
| `webConfig` | `{ webType: 1, templateType: 1, sortCode: 1 }` |
| `category` | `"custom_module"` |
| `fields[].component` | Derived via rule in section 2.10 |
| `fields[].componentProps` | `{}` |
| `appColumnDesign` | Fallback to `columnDesign` |

### 5.3 Warning log

When the adapter encounters missing extension data, it MUST emit a structured warning:

```csharp
_logger.LogWarning(
    "IR extension '{ExtensionName}' is missing in FinalIR (version={IrVersion}). " +
    "Using default: {DefaultValue}. " +
    "PipelineId={PipelineId}, Project={ProjectName}",
    extensionName, irVersion, defaultValue, pipelineId, projectName
);
```

This ensures that during the transition period (mixed legacy and extended IRs), operators can identify which pipelines are still producing legacy IRs and plan the migration.

### 5.4 Migration path

| Phase | Action | IR version produced |
|-------|--------|---------------------|
| Pre-C2.1 | No changes — SA pipeline produces legacy IR | `"1.0"` (or absent) |
| C2.1 launch | SA pipeline extended to produce all MUST-FIX + HIGH fields. Adapter handles both versions. | `"2.0"` |
| C2.1 + 2 weeks | Remove legacy IR handling from adapter. All pipelines must produce `version >= "2.0"`. | `"2.0"` |
| C2.2 | Add MEDIUM extensions (component, webConfig override). | `"2.1"` |

---

## Appendix A: TypeScript type definitions (for frontend/adapter reference)

```typescript
// IR v2.0 extended schema — TypeScript types

interface IRRoot {
  version: "2.0";
  webConfig?: WebConfig;
  category?: string;
  categoryName?: string;
  modules: ModuleDefinition[];
  tables: TableDefinition[];
  relations?: RelationDefinition[];
  dictionaries?: Record<string, DictionaryDef>;
  pages: PageComponent;
  dashboard: DashboardConfig;
}

interface WebConfig {
  webType: 1 | 2;
  templateType: 1 | 2 | 3;
  sortCode?: number;
}

interface ModuleDefinition {
  nodeType: "ModuleDefinition";
  moduleName: string;
  parentModule: string | null;
  icon?: string;
  sortOrder?: number;
  permissionPrefix?: string;
  operations?: ("add" | "edit" | "delete" | "view" | "export" | "import")[];
}

interface TableDefinition {
  nodeType: "TableDefinition";
  tableName: string;
  comment?: string;
  fields: FieldDefinition[];
  columnDesign?: ColumnDesign[];
  appColumnDesign?: ColumnDesign[];
}

interface FieldDefinition {
  fieldName: string;
  fieldType: "string" | "int" | "long" | "decimal" | "double" | "bool" | "datetime" | "guid" | "text";
  fieldLength: number | null;
  isPrimaryKey?: boolean;
  isNullable: boolean;
  isDictionary?: boolean;
  dictionaryCode?: string;
  comment?: string;
  defaultValue?: string;
  component?: string;
  componentProps?: Record<string, unknown>;
}

interface ColumnDesign {
  field: string;
  width?: number;
  align?: "left" | "center" | "right";
  sortable?: boolean;
  fixed?: "left" | "right" | null;
  show?: boolean;
}

interface RelationDefinition {
  fromTable: string;
  fromField: string;
  toTable: string;
  toField: string;
  relationType: "OneToOne" | "OneToMany" | "ManyToOne" | "ManyToMany";
  throughTable?: string;
  throughFromField?: string;
  throughToField?: string;
  constraintName?: string;
}

interface DictionaryDef {
  name: string;
  values: { code: string; label: string }[];
}

interface PageComponent {
  nodeType: "Component";
  minCount: number;
  descriptions: string[];
}

interface DashboardConfig {
  nodeType: "DashboardConfig";
  requiredCharts: string[];
}
```
