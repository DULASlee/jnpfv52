# JNPF V5.2 前端 IR 现状

> 来源：production-func-analysis.txt + 源码 componentMap.ts + graph.json + eval() 安全修复记录
> 用途：DKEE 知识图谱 — 前端组件与 IR 层
> 更新日期：2026-06-11

---

## 0. IR 是什么

IR (Intermediate Representation) 是 JNPF 前端架构重构的核心抽象层，桥梁角色：

```
┌──────────────────────────────────────────────────────────────────────┐
│                      三层数据流                                       │
│                                                                      │
│  JNPF 在线设计器                                                      │
│  (VisualDev 表单设计)                                                │
│       │                                                              │
│       ▼ 平台 Schema (FormData JSON)                                  │
│  ┌─────────────────────┐                                             │
│  │   Schema 清洗器     │  ← 标准化平台差异（__config__ → IR property） │
│  │   (schema-cleaner)  │                                             │
│  └─────────┬───────────┘                                             │
│            ▼                                                         │
│  ┌─────────────────────┐                                             │
│  │   IR (中间表示层)    │  ← 平台无关的结构化描述                      │
│  │   types.ts          │     - FieldNode: 字段定义                   │
│  │   component-mapping │     - LayoutNode: 布局定义                   │
│  │   expression-engine │     - ActionNode: 动作/事件定义              │
│  └─────────┬───────────┘     - aiHints: AI 理解业务上下文的探针      │
│            │                                                         │
│            ├──→ IR 编译器 (ir-compiler)                              │
│            │       ↓  Vue 3 SFC (运行时代码)                         │
│            │                                                         │
│            ├──→ DKEE 写入器 (dkee-writer)                            │
│            │       ↓  领域知识图谱 (结构化实体 + 关系)                │
│            │                                                         │
│            └──→ AI Prompt 编译器 (ai-compiler)                       │
│                    ↓  注入 aiHints 的 Prompt Context                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

## 1. IR 类型层次

### 1.1 顶层结构

```typescript
// src/core/ir/types.ts (F-1 创建中)

interface IRLayer {
  version: string;              // IR 版本号
  meta: IRMeta;                 // 元数据
  fields: FieldNode[];          // 字段节点列表
  layout: LayoutNode;           // 布局树
  actions: ActionNode[];        // 动作/事件列表
  validation: ValidationNode[]; // 校验规则
  aiHints: AIHints;             // AI 理解探针
}

interface IRMeta {
  modelId: string;              // 原始模型 ID
  modelName: string;            // 模型名称
  source: 'visualdev' | 'codegen' | 'manual'; // 来源
  createdAt: string;
  originalSchema: string;       // 原始 Schema 引用（用于审计）
}
```

### 1.2 字段类型系统

```typescript
type JnpfKey =
  | 'input' | 'textarea' | 'inputNumber' | 'comInput'
  | 'select' | 'radio' | 'checkbox' | 'switch'
  | 'datePicker' | 'timePicker' | 'cascader'
  | 'popupSelect' | 'popupTableSelect'
  | 'userSelect' | 'depSelect' | 'organizeSelect'
  | 'uploadFile' | 'uploadImg'
  | 'rate' | 'slider' | 'sign' | 'editor'
  | 'table' | 'inputTable' | 'relationForm'
  | 'calculate' | 'autoComplete' | 'colorPicker'
  | 'text' | 'link' | 'divider' | 'groupTitle'
  | 'button' | 'iframe' | 'qrcode' | 'barcode'
  | 'areaSelect' | 'treeSelect' | 'cron';

interface FieldNode {
  id: string;
  jnpfKey: JnpfKey;             // JNPF 组件类型键
  vModel: string;               // 双向绑定字段名
  label: string;
  defaultValue?: any;
  required: boolean;
  disabled: boolean;
  hidden: boolean;
  placeholder?: string;
  props: Record<string, any>;   // 组件属性（清洗后）
  rules: ValidationNode[];      // 校验规则
  style: FieldStyle;            // 样式属性
  on: FieldEventHandler[];      // 事件处理器
  aiHints: FieldAIHints;        // AI 理解探针
}

interface FieldAIHints {
  semanticRole?: string;        // 语义角色 e.g. "email", "phone", "currency"
  businessMeaning?: string;     // 业务含义 e.g. "客户联系人邮箱"
  examples?: string[];          // 示例值
  relatedFields?: string[];     // 关联字段 ID
  intentHints?: string[];       // 意图提示 e.g. ["user_personal_info"]
  confidenceLevel?: 'low' | 'medium' | 'high';
}
```

### 1.3 表达式与动作

```typescript
interface FieldEventHandler {
  trigger: 'change' | 'blur' | 'focus';
  expression: ExpressionNode;   // 编译后的表达式（非 raw string）
  rawScript?: string;           // 原始脚本（用于审计/回退）
  compiled: boolean;            // 是否已编译为安全表达式
}

interface ExpressionNode {
  type: 'empty' | 'assignment' | 'condition' | 'promise' | 'composite';
  params: ExpressionParam[];
  body: ExpressionStep[];
}

interface ActionNode {
  trigger: 'onLoad' | 'beforeSubmit' | 'afterSubmit';
  expression: ExpressionNode;
  rawScript?: string;
}

interface ValidationNode {
  type: 'required' | 'regex' | 'custom' | 'range' | 'async';
  pattern?: RegExp;             // 正则（安全解析后的）
  patternRaw?: string;          // 原始正则字符串（审计用）
  message: string;
  trigger: 'blur' | 'change';
}
```

## 2. 组件映射现状

### 2.1 已映射组件（67 个，来自 componentMap.ts）

| 类型 | 数量 | 组件 |
|------|------|------|
| 输入类 | 12 | Input, Textarea, InputNumber, InputGroup, InputSearch, InputPassword, AutoComplete, ColorPicker, IconPicker, Link, Editor, Calculate |
| 选择类 | 10 | Select, Radio, Checkbox, Switch, Rate, Slider, Cascader, TreeSelect, AreaSelect, Cron |
| 日期类 | 5 | DatePicker, TimePicker, DateRange, TimeRange, MonthPicker, WeekPicker |
| 弹出选择 | 4 | PopupSelect, PopupTableSelect, PopupAttr, PopupAttr |
| 组织/用户 | 8 | UserSelect, UsersSelect, OrganizeSelect, DepSelect, PosSelect, GroupSelect, RoleSelect, RelationForm |
| 上传/媒体 | 4 | UploadFile, UploadImg, UploadImgSingle, Sign, Signature, Qrcode, Barcode, Iframe |
| 布局 | 5 | GroupTitle, Divider, Alert, Text, Button |
| 表格 | 3 | Table, InputTable, RelationFormAttr |
| 系统字段 | 6 | BillRule, ModifyUser, ModifyTime, CreateUser, CreateTime, CurrOrganize, CurrPosition |
| 其他 | 3 | Location, NumberRange, StrengthMeter |

**总计：** 67 个 `JnpfKey` → Vue Component 映射

### 2.2 未映射/占位符组件

| JnpfKey | 映射到 | 说明 |
|---------|--------|------|
| `BillRule` | JnpfInput | 占位：业务规则编号 → 简单输入框 |
| `ModifyUser` | JnpfInput | 占位：修改人 → 简单输入框（应为只读） |
| `ModifyTime` | JnpfInput | 占位：修改时间 → 简单输入框（应为只读） |
| `CreateUser` | JnpfOpenData | ✅ 正确：只读展示 |
| `CreateTime` | JnpfOpenData | ✅ 正确：只读展示 |
| `CurrOrganize` | JnpfOpenData | ✅ 正确：只读展示 |
| `CurrPosition` | JnpfOpenData | ✅ 正确：只读展示 |

### 2.3 组件注册流程

```
componentMap.ts (67 entries)
       │
       ▼
Form/src/componentMap.ts (Form 专用注册表)
       │
       ▼
FormGenerator/src/helper/render.ts (渲染工厂: h(Comp, props))
       │
       ▼
FormGenerator/src/components/Parser.vue (递归 TSX 渲染器)
```

## 3. 函数字符串分析

### 3.1 来源数据

从线上 API 捕获 3 个生产 Schema（followLink / order.receivable / tesst000），分析 25 个函数。

### 3.2 复杂度分布

| 级别 | 数量 | 占比 | 特征 |
|------|------|------|------|
| EMPTY | 22 | 88% | 箭头函数空体 `{}` |
| TEMPLATE | 3 | 12% | `beforeSubmit` 含 `Promise.resolve()`，无业务逻辑 |
| L1-SIMPLE | 0 | 0% | — |
| L2-MEDIUM | 0 | 0% | — |
| L3-HARD | 0 | 0% | — |

**结论：** 测试数据库中 100% 的函数是模板生成的占位符，0 个包含自定义业务逻辑。真实复杂度数据需从生产环境获取。

### 3.3 函数签名目录（已验证）

```
Form-level (fields[].funcs) — 7 params:
  onLoad({ formData, setFormData, setShowOrHide, setRequired, setDisabled, onlineUtils })
  beforeSubmit({ ... }) → new Promise((resolve, reject) => { resolve() })
  afterSubmit({ ... })

Field-level (fields[].on) — 8 params:
  on.change({ data, rowIndex, formData, setFormData, setShowOrHide, setRequired, setDisabled, onlineUtils })
  on.blur({ ... })  // 同上 8 参数签名
```

### 3.4 框架注入参数 API Surface

| 参数 | 来源 | 描述 | 级别 |
|------|------|------|------|
| `data` | 框架 | 当前字段值 | 字段级 only |
| `rowIndex` | 框架 | 子表行索引 | 字段级 only |
| `formData` | Form Model State | 所有表单字段值 | both |
| `setFormData` | Form Model Setter | 更新表单字段值 | both |
| `setShowOrHide` | Form Visibility | 切换字段可见性 | both |
| `setRequired` | Form Validation | 切换必填状态 | both |
| `setDisabled` | Form State | 切换禁用状态 | both |
| `onlineUtils` | JNPF 平台工具 | 平台实用函数 | both |

## 4. eval() 安全修复记录

### 4.1 修复总览

| 项目 | eval() 数量 | new Function() 数量 | 状态 |
|------|------------|---------------------|------|
| `jnpf-web-vue3` (PC) | 0 | 0 | ✅ 全修复 (2026-06-11) |
| `jnpf-web-datascreen` (大屏) | 0 | 0 | ✅ 全修复 (2026-06-10) |

### 4.2 修复明细（PC 前端 7 处）

| 文件 | 原代码模式 | 修复方式 |
|------|-----------|----------|
| `transform.ts:46` | `eval(regList[i].pattern)` | `safeParseRegex()` |
| `Parser.vue:527` | `eval(item.pattern)` | `safeParseRegex()` |
| `Parser.vue:536` | `eval(val)` 正则检测 | `isValidRegex()` + 删除 isRegExp |
| `RInput.vue:144` | `eval(val)` 正则检测 | `isValidRegex()` |
| `InputTable.vue:584` | `eval(item.pattern)` | 内联 `safeParseRegex()` + null guard |
| `propPanel/index.vue:130` | `eval(key + 'NodeRef')` | `nodeRefMap[key]` 查表 |
| `propPanel/index.vue:282` | `eval(form + 'NodeRef')` | `nodeRefMap[form]` 查表 |

### 4.3 安全工具函数（已创建）

```
FormGenerator/src/helper/regexp.ts
  ├── safeParseRegex(pattern: string): RegExp | null
  │   " /^[a-z]+$/gi " → new RegExp("^[a-z]+$", "gi")
  └── isValidRegex(val: string): boolean
      safeParseRegex(val) !== null
```

## 5. AI 探针注入点

### 5.1 已注入的探针框架

```typescript
// src/core/ir/types.ts — aiHints 字段定义

interface AIHints {
  pagePurpose?: string;          // 页面意图 e.g. "客户管理列表"
  domainContext?: string;        // 领域上下文 e.g. "CRM"
  userIntent?: string;           // 用户意图 e.g. "查看并管理客户信息"
  complexityScore?: 1|2|3|4|5; // 复杂度评分
  suggestedPrompts?: string[];   // 推荐 AI Prompt
}

interface FieldAIHints {
  semanticRole?: string;         // e.g. "email", "phone", "currency"
  businessMeaning?: string;      // e.g. "客户联系人邮箱"
  examples?: string[];           // e.g. ["zhangsan@company.com"]
  relatedFields?: string[];      // 关联字段
  aiQuality?: 'low' | 'medium' | 'high'; // AI 生成质量检查级别
}
```

### 5.2 探针注入流程

```
在线设计器设计表单
       │
       ▼
Schema 清洗器 (schema-cleaner)
       │
       ├──→ 自动推断 FieldAIHints:
       │       - 字段名含 "email" → semanticRole: "email"
       │       - 字段名含 "phone"/"mobile" → semanticRole: "phone"
       │       - 字段名含 "price"/"amount"/"money" → semanticRole: "currency"
       │       - 字典绑定字段 → businessMeaning: 字典名称
       │
       ├──→ 自动推断 AIHints:
       │       - 模型名称 → pagePurpose
       │       - 所属模块 → domainContext
       │
       └──→ 输出 IR → DKEE 写入器 / AI Prompt 编译器
```

## 6. 封存文件清单（前端交互契约）

以下文件在重构稳定后标记为"封存"——修改需架构师审批：

| 文件 | 职责 | 封存阶段 |
|------|------|----------|
| `packages/shared/src/http/index.ts` | 统一 HTTP 请求层 | F-2 |
| `packages/shared/src/auth/token.ts` | 统一 Token 管理 (get/set/clear/isExpired/refresh) | F-2 |
| `packages/shared/src/crypto/index.ts` | 统一加密工具 (AES/MD5) | F-2 |
| `packages/shared/src/permission/index.ts` | 统一权限检查 | F-2 |
| `jnpf-web-vue3/src/components/Form/src/componentMap.ts` | 组件注册表 | F-3 |
| `jnpf-web-datascreen/src/utils/auth.js` | 大屏认证 | F-2 |

## 7. 当前缺失

| 项目 | 状态 | 预计完成 |
|------|------|----------|
| F-1 IR 类型定义文件 (6 个) | ❌ 未创建，代码已设计 | 本周 |
| Schema Cleaner 实现 | ❌ 未开始 | F-1 完成之后 |
| IR Compiler (IR → Vue SFC) | ❌ 未开始 | F-2 |
| Expression Engine (安全表达式编译) | ⚠️ 部分（regexp.ts 已创建，sandbox 未启动） | F-2 |
| DKEE Writer (IR → 知识图谱) | ❌ 未开始 | F-3 |
| AI Compiler (IR + aiHints → Prompt) | ❌ 未开始 | F-3 |
| production-func-analysis.txt 纳入 docs | ❌ 文件在 jnpf-survey/，未迁入 | 今日 |
