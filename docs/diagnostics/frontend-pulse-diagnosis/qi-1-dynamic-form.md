# Qi 1: 动态表单渲染引擎（web-vue3）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，标注每个环节的数据格式和转换方式
> 诊断范围: jnpf-web-vue3 动态表单全链路（BasicForm + FormGenerator 双系统）

---

## 一、双表单系统架构总览

JNPF 存在两套独立的表单渲染系统，共享底层 componentMap：

```
┌─────────────────────────────────────────────────────────────────────┐
│                    JNPF 动态表单系统 (双引擎)                         │
│                                                                     │
│  System A: BasicForm (CRUD 业务表单)                                │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ FormSchema[] → BasicForm.vue → FormItem.vue (JSX)            │  │
│  │                              → componentMap.get(component)   │  │
│  │  Use: Standard CRUD pages, popup forms                       │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  System B: FormGenerator (在线设计 + 运行时渲染)                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Designer: FormGenerator.vue (drag-drop, undo/redo)           │  │
│  │ Runtime:  Parser.vue (JSX, 831 lines)                        │  │
│  │           → render.ts (VNode factory)                        │  │
│  │           → componentMap.get(jnpfKey)                        │  │
│  │  Use: Workflow forms, online dev, code generator             │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  Shared: componentMap.ts (~60 components, global Map)              │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 二、组件注册机制

### 2.1 注册表结构

**文件:** `src/components/Form/src/componentMap.ts` (151 lines)

```typescript
// 全局单例 Map
const componentMap = new Map<ComponentType, Component>();

// 内置注册 ~60 个组件
componentMap.set('Input', Input);
componentMap.set('Textarea', Input.TextArea);
componentMap.set('Select', Select);
componentMap.set('DatePicker', DatePicker);
// ... JNPF 自定义组件
componentMap.set('OrganizeSelect', OrganizeSelect);
componentMap.set('UserSelect', UserSelect);
componentMap.set('RelationForm', RelationForm);
componentMap.set('InputTable', InputTable);
componentMap.set('Calculate', Calculate);
componentMap.set('Editor', Editor);
componentMap.set('AreaSelect', AreaSelect);
// ...

// 系统字段映射
// BillRule→Input, ModifyUser→Input, ModifyTime→Input
// CreateUser→OpenData, CreateTime→OpenData
// CurrOrganize→OpenData, CurrPosition→OpenData
```

### 2.2 扩展机制

```typescript
// 运行时动态注册/移除
export function add(compName: ComponentType, component: Component) {
  componentMap.set(compName, component);
}
export function del(compName: ComponentType) {
  componentMap.delete(compName);
}
```

### 2.3 FormGenerator 设计器组件面板

**文件:** `src/components/FormGenerator/src/helper/componentMap.ts` (1966 lines)

设计器将组件分为 4 类面板：

| 面板 | 组件数 | 包含 |
|---|---|---|
| 基础控件 (inputComponents) | 18 | Input, Textarea, InputNumber, Switch, Radio, Checkbox, Select, Cascader, DatePicker, TimePicker, UploadFile, UploadImg, ColorPicker, Rate, Slider, Editor, Link, Button, Text, Alert, QRCode, Barcode |
| 高级控件 (selectComponents) | 16 | OrganizeSelect, DepSelect, PosSelect, UserSelect, RoleSelect, GroupSelect, UsersSelect, Table, TreeSelect, PopupTableSelect, AutoComplete, AreaSelect, RelationForm, RelationFormAttr, PopupSelect, PopupAttr, Signature, Sign, Location, Iframe |
| 系统控件 (systemComponents) | 7 | CreateUser, CreateTime, ModifyUser, ModifyTime, CurrOrganize, CurrPosition, BillRule |
| 布局控件 (layoutComponents) | 7 | GroupTitle, Divider, Collapse, Tab, Row, Card, TableGrid |

每个组件定义包含完整的默认配置 (`__config__`)、默认属性、事件处理函数模板（字符串形式）。

### 2.4 注册机制评估

| 维度 | 评分 | 说明 |
|---|---|---|
| 扩展性 | ⭐⭐⭐⭐ | `add()/del()` API 简单，全局 Map 随时可改 |
| 类型安全 | ⭐⭐ | `as unknown as` / `as any` 泛滥 |
| 发现性 | ⭐⭐ | 无文档，需阅读 componentMap.ts 源码 |
| 隔离性 | ⭐⭐ | 全局单例，无 scope 概念，多实例共享风险 |

---

## 三、Schema 驱动的渲染链路

### 3.1 System A: BasicForm → FormItem 链路

```
FormSchema[] (用户定义)
  │
  ▼
BasicForm.vue
  │ computed getSchema → 遍历 schemas
  │ 对每个 schema:
  ▼
FormItem.vue (JSX render 函数, 383 lines)
  │
  ├─ getComponentsProps(): 解析 componentProps
  │   ├─ 静态 object → 直接使用
  │   └─ 函数 → 调用({ schema, tableAction, formModel, formActionType })
  │
  ├─ getDisable(): 三源合并
  │   ├─ 全局 disabled (BasicForm props)
  │   ├─ schema.disabled (item-level)
  │   └─ dynamicDisabled 函数 → 调用({ schema, formModel, ... })
  │
  ├─ getShow(): 四源合并
  │   ├─ show (静态 boolean)
  │   ├─ ifShow → 函数({ field, model, values, ... })
  │   ├─ auth (权限控制)
  │   └─ isAdvanced (高级搜索折叠)
  │
  ├─ handleRules(): 动态规则生成
  │   ├─ required + type → 内置规则
  │   ├─ whitespace 规则 (Input 组件)
  │   └─ dynamicRules 函数 → 调用({ schema, formModel, ... })
  │
  ├─ renderComponent(): 从 componentMap 取组件
  │   ├─ componentMap.get(schema.component) → Component
  │   ├─ 绑定 changeEvent 事件
  │   └─ InputNumber value 强制类型转换
  │
  └─ renderItem(): 包裹 Ant Design Form.Item
      ├─ label + BasicHelp(schema.helpMessage)
      ├─ suffix (自定义后缀)
      └─ 整体输出 VNode
```

### 3.2 System B: FormGenerator → Parser 链路

```
Designer (FormGenerator.vue)
  │
  │ drag-drop → drawingList[] (嵌套 GenItem[])
  │ assembleFormData() → formConf = { fields: drawingList, ... }
  │
  ▼
JSON Config (formConf)
  │
  ▼
Parser.vue (JSX, 831 lines, Options API + setup)
  │
  ├─ init(): 初始化全流程 (8 步)
  │   ├─ initCss()           → 注入自定义 CSS 到 <head>
  │   ├─ initFormData()      → 从 config.defaultValue 填充 formData
  │   ├─ initRelationForm()  → 处理关联表单属性隐藏逻辑
  │   ├─ buildRules()        → 构建 Ant Design 表单校验规则
  │   ├─ buildOptions()      → 加载远端数据 (字典/远端/静态)
  │   ├─ buildRelations()    → 构建字段间联动关系图
  │   ├─ initDefaultRelationData() → 初始化默认联动数据
  │   └─ onLoad()            → 调用用户自定义 onLoad 脚本
  │
  ├─ renderFrom() → 顶层 JSX 渲染
  │   └─ <a-form model={formData} rules={formRules}>
  │       └─ <a-row>
  │           └─ renderFormItem(fields)
  │               ├─ colFormItem → <a-col><a-form-item>{render.ts VNode}</a-form-item></a-col>
  │               └─ rowFormItem
  │                   ├─ Row     → <a-row>{renderChildren}</a-row>
  │                   ├─ Card    → <a-card>{renderChildren}</a-card>
  │                   ├─ Tab     → <a-tabs>{renderChildren}</a-tabs>
  │                   ├─ Collapse → <a-collapse>{renderChildren}</a-collapse>
  │                   ├─ TableGrid → <table> (原生 HTML table 网格布局)
  │                   └─ Table   → → 委托给 colFormItem (因为 InputTable 组件自带渲染)
  │
  └─ render.ts (VNode Factory, 108 lines)
      ├─ buildDataObject(): JSON → Vue component props
      │   ├─ 遍历 confClone 所有 key
      │   ├─ __vModel__ → buildVModel(dataObject, defaultValue)
      │   │   └─ 创建 on['update:value'] 双向绑定
      │   ├─ table → 注入 formData, relations, vModel
      │   ├─ relationForm/popupSelect → 计算 field identifier
      │   └─ clearAttrs(): 删除 __config__, __slot__, __methods__, on
      ├─ componentMap.get(upperFirst(jnpfKey)) → Comp
      │   └─ 'Table' → 映射为 'InputTable'
      └─ h(Comp, realDataObject) → VNode
```

### 3.3 两种 Schema 对比

| 维度 | System A (FormSchema) | System B (GenItem) |
|---|---|---|
| 定义方式 | TypeScript 对象 | JSON (designer 输出) |
| 组件标识 | `component: 'Input'` | `__config__.jnpfKey: 'input'` |
| 字段名 | `field: 'userName'` | `__vModel__: 'userName'` |
| 联动支持 | ifShow/show/disabled 函数 | relations 系统 (buildRelations) |
| 脚本执行 | 无 (声明式) | getScriptFunc(string) → eval |
| 数据源 | dictionaryType (组件属性) | dataType: static/dictionary/dynamic |
| 嵌套表单 | 不支持 | tab/collapse/card/row/tableGrid |
| 子表 | 不支持 | jnpfKey: 'table' + children |
| 使用场景 | 标准 CRUD | 在线设计 + 流程表单 |

---

## 四、值与事件的绑定机制

### 4.1 System A (BasicForm/FormItem)

```
formModel = reactive<Recordable>({})
  │
  │ v-model:value="formModel[schema.field]"
  │
  ├─ 用户输入 → 组件 emit('update:value') → formModel 更新
  │
  ├─ setFieldsValue({ key: val })
  │   └─ 深层次更新 formModel (支持点号路径)
  │   └─ 调用 validateFields() 重新校验
  │
  └─ submitOnChange (深度 watch formModel)
      └─ 每次值变更 → emit('submit') 自动提交
```

### 4.2 System B (Parser/render.ts)

```
formData = reactive<{}>({})
  │
  │ initFormData() → formData[__vModel__] = config.defaultValue
  │
  ├─ render.ts buildVModel():
  │   dataObject.value = defaultValue
  │   dataObject.on['update:value'] = val => emit('update:value', val)
  │
  ├─ Parser.vue buildListeners():
  │   从 scheme.on.<event> 字符串 → getScriptFunc(str)
  │   listeners['onChange'] → (...arg) => {
  │     const func = eval(string); // ⚠️ eval 执行用户脚本
  │     func({ data, ...getParameter });
  │     handleRelation(scheme.__vModel__); // 触发联动
  │   }
  │
  ├─ setFormData(prop, value):
  │   comSet('defaultValue', prop, value)  // 修改配置树
  │   formData[prop] = value                // 修改数据模型
  │   → nextTick → handleRelation(prop)     // 触发联动链
  │
  └─ handleSubmit():
      beforeSubmit() → formElRef.validate()
      → checkTableData() (子表数据提取)
      → emit('submit', formData, afterSubmit)
```

### 4.3 事件绑定安全发现

```typescript
// render.ts:37-45 — 事件字符串 → 运行时函数
['on', 'nativeOn'].forEach(attr => {
  const eventKeyList = Object.keys(confClone[attr] || {});
  eventKeyList.forEach(key => {
    confClone[attr][key] = (...arg) => emit(key, arg);
  });
});

// Parser.vue:274 — eval 执行用户脚本
const func: any = getScriptFunc(str); // getScriptFunc 内部使用 eval
func({ data, ...unref(getParameter) });
```

**风险:** `eval()` 执行用户编写的脚本函数（onLoad/beforeSubmit/afterSubmit/组件事件），存储在数据库中。恶意管理员可通过表单设计注入任意 JS 代码。

---

## 五、校验规则的动态生成

### 5.1 System A (BasicForm/FormItem)

```typescript
// FormItem.vue: handleRules()
function handleRules() {
  const rules: Rule[] = [];
  // 1. required 规则
  if (schema.required) {
    rules.push({ required: true, message: `${schema.label}不能为空` });
  }
  // 2. 组件类型 → rule.type
  //    DateRange→array, InputNumber→number
  setComponentRuleType(component, rules);
  // 3. whitespace 规则 (Input 组件)
  if (useInputComponents.includes(schema.component)) {
    rules.push({ whitespace: true, message: `${schema.label}不能为空` });
  }
  // 4. 动态规则函数
  if (schema.dynamicRules && typeof schema.dynamicRules === 'function') {
    const dynamicRules = schema.dynamicRules({ schema, formModel, ... });
    rules.push(...dynamicRules);
  }
  // 5. 自定义规则 (schema.rules)
  if (schema.rules && schema.rules.length) {
    rules.push(...schema.rules);
  }
  return rules;
}
```

### 5.2 System B (Parser)

```typescript
// Parser.vue: buildRules()
function buildRules(componentList) {
  componentList.forEach(cur => {
    const config = JSON.parse(JSON.stringify(cur.__config__));
    // 1. required 规则
    if (config.required) {
      const required = { required: true, message: cur.placeholder };
      if (Array.isArray(config.defaultValue)) {
        required.type = 'array';
        required.message = `请至少选择一个${config.label}`;
      }
      config.regList.push(required);
    }
    // 2. 自定义正则 → eval 转换
    state.formRules[cur.__vModel__] = config.regList.map(item => {
      item.pattern && isRegExp(item.pattern) && (item.pattern = eval(item.pattern));
      item.trigger = config.trigger || 'blur';
      return item;
    });
  });
}
```

**安全发现:** `buildRules()` 使用 `eval(item.pattern)` 将字符串正则转为 RegExp 对象。虽然 `isRegExp()` 先验证，但 `eval()` 调用本身是安全风险。

---

## 六、表单联动（show/disabled/rules 动态切换）

### 6.1 System B Relations 系统（核心联动引擎）

```
buildRelations(componentList, relations)
  │
  │ 扫描所有组件，建立 field → dependent[] 映射
  │
  ├─ 动态数据源联动 (dataType: 'dynamic')
  │   templateJson 中有 relationField + sourceType=1
  │   → relations[relationField].push({ ..., opType: 'setOptions' })
  │
  ├─ 用户选择联动 (userSelect + selectType: dep/pos/role/group)
  │   relationField 指定依赖字段
  │   → relations[relationField].push({ ..., opType: 'setUserOptions' })
  │
  ├─ 日期/时间范围联动 (datePicker/timePicker)
  │   startRelationField / endRelationField
  │   → relations[relationField].push({ ..., opType: 'setStartTime/setEndTime' })
  │
  └─ 弹窗选择联动 (popupSelect)
      → relations[relationField].push({ ..., opType: 'setPopupOptions' })
```

### 6.2 联动执行流程

```
字段 A 值变更
  │
  ├─ buildListeners() → onChange handler
  │   func({ data, ...getParameter })  // 用户脚本
  │   handleRelation(fieldA)
  │
  ▼
handleRelation(fieldA)
  │
  │ 遍历 relations[fieldA] 中的每个依赖项:
  │
  ├─ 清空依赖字段值 (级联清空)
  │
  ├─ opType === 'setOptions'
  │   └─ getDataInterfaceRes(url, query) → setFieldOptions(field, data)
  │       → comSet('options', field, newVal)
  │
  ├─ opType === 'setUserOptions'
  │   └─ comSet('ableRelationIds', field, value)
  │
  ├─ opType === 'setStartTime'
  │   └─ comSet('startTime', field, value)
  │
  └─ opType === 'setEndTime'
      └─ comSet('endTime', field, value)
```

### 6.3 comSet() — 配置树直接变异

```typescript
// Parser.vue:431-465
function comSet(field, prop, value) {
  // 递归遍历 formConfCopy.fields 树
  const loop = list => {
    for (let i = 0; i < list.length; i++) {
      let item = list[i];
      if (item.__vModel__ === prop) {
        switch (field) {
          case 'disabled':
            item[field] = value;           // 直接修改 GenItem
            break;
          case 'options':
            if (dyOptionsList.includes(...)) item.options = value;
            break;
          default:
            item.__config__[field] = value; // 修改 __config__
            break;
        }
        item.__config__.renderKey = +new Date() + item.__vModel__; // 强制重渲染
        break;
      }
      if (item.__config__.children) loop(item.__config__.children);
    }
  };
  loop(state.formConfCopy.fields);
}
```

**关键设计:** 通过修改 `renderKey` (时间戳) 强制 Vue 重新渲染组件，而非使用响应式系统。

### 6.4 子表 (Table) 联动

子表字段通过 `parentVModel-childVModel` 格式标识，联动委托给子表组件：

```typescript
if (vModel.includes('-')) {
  const tableVModel = vModel.split('-')[0];
  unref(state.tableRefs[tableVModel])?.tableRef
    ?.handleRelationForParent(e, defaultValue);
}
```

### 6.5 程序化 API (脚本中使用)

提供给用户脚本的回调函数：

| API | 功能 |
|---|---|
| `setFormData(prop, value)` | 设置字段值 + 触发联动 |
| `setShowOrHide(prop, value)` | 控制字段显隐 |
| `setRequired(prop, value)` | 控制必填 |
| `setDisabled(prop, value)` | 控制禁用 |
| `onlineUtils` | 在线工具函数集合 |

---

## 七、工作流动态表单集成

### 7.1 workflow/flowForm/dynamicForm/index.vue

这个 187 行的组件是 Parser 的工作流包装器：

```
Props: config { id, formConf, formData, flowId, opType, formOperates, ... }
  │
  ├─ init(config):
  │   ├─ formConf = JSON.parse(config.formConf)
  │   ├─ generatorStore.setDynamicModelExtra(extra)
  │   ├─ fillFormData(formConf, formData, isAdd)
  │   │   ├─ 权限控制: formOperates → read/write/required 开关
  │   │   ├─ defaultCurrent: 自动填入当前用户/组织/部门/岗位/角色/分组/签名
  │   │   └─ disabled/readonly 状态传播
  │   └─ nextTick → Parser 重渲染
  │
  ├─ dataFormSubmit(eventType, flowUrgent):
  │   └─ getParser().handleSubmit() → submitForm()
  │       └─ emit('eventReceiver', state.dataForm, eventType)
  │
  └─ Parser (createAsyncComponent 异步加载)
      └─ 共享 componentMap，完全相同的渲染逻辑
```

### 7.2 fillFormData 权限控制

```typescript
// 对每个字段匹配 formOperates 权限
if (config.formOperates && config.formOperates.length) {
  let arr = config.formOperates.filter(o => o.id === fieldId) || [];
  if (arr.length) {
    let obj = arr[0];
    noShow = !obj.read;       // 无读权限→隐藏
    isDisabled = !obj.write;  // 无写权限→禁用
    required = obj.required;  // 权限覆盖必填
  }
}
```

---

## 八、发现汇总

### P0 安全红线

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| Q1-1 | `eval()` 执行用户脚本 (onLoad/beforeSubmit/afterSubmit/组件事件) | Parser.vue:274 | 存储型 XSS，恶意管理员攻击 |
| Q1-2 | `eval(item.pattern)` 将字符串转 RegExp | Parser.vue:527 | 代码注入风险 |
| Q1-3 | Token 嵌入 DataV/外链 URL (已在 Pulse 报告) | routeHelper.ts | Token 泄露 |

### P1 架构问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| Q1-A1 | **双表单系统并存** — BasicForm (CRUD) + FormGenerator (在线) 两套独立代码，共享 componentMap | FormItem.vue + Parser.vue | 维护成本翻倍，行为不一致 |
| Q1-A2 | **FormItem.vue (383行) + Parser.vue (831行) 超长 JSX 组件** | 两个文件 | 难以理解和修改 |
| Q1-A3 | **comSet() 直接变异配置树** — 绕过 Vue 响应式系统，靠 renderKey 时间戳强制重渲染 | Parser.vue:431-465 | 不可调试，性能依赖时间戳 |
| Q1-A4 | **全局 componentMap 无作用域隔离** | componentMap.ts | 多实例冲突风险 |
| Q1-A5 | **Parser:831 + FormGenerator:926 = ~1750 行巨型组件** | FormGenerator.vue + Parser.vue | 违反单一职责 |
| Q1-A6 | `getScriptFunc()` 内部实现不透明，错误静默 | Parser.vue:274 | 调试困难 |
| Q1-A7 | **子表联动使用字符串路径 `parent-child`** — 无类型安全 | Parser.vue:322 | 重构时易遗漏 |
| Q1-A8 | `tempActiveData` 全局变量 — 多设计器实例会相互覆盖 | FormGenerator.vue:189 | 并发 bug |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| Q1-E1 | Designer 组件定义 1966 行，与 render.ts 共享配置格式无文档 | componentMap.ts |
| Q1-E2 | `getRealProps()` 做属性名映射 (clearable→allowClear 等)，API 不一致 | transform.ts:4-30 |
| Q1-E3 | `buildCSS()` 字符串拼接 CSS → 注入 `<style>` 标签 | Parser.vue:468-488 |
| Q1-E4 | FormSchema (System A) 与 GenItem (System B) 无法互通 | 全局 |
| Q1-E5 | `as unknown as` 类型断言在 FormItem.vue 和 render.ts 中泛滥 | FormItem.vue, render.ts |
| Q1-E6 | 子表 tableRefs 通过 `getCurrentInstance()?.refs` 获取，脆弱 | Parser.vue:818-819 |

---

## 九、性能观察

| 环节 | 估算耗时 | 说明 |
|---|---|---|
| FormItem renderComponent | <1ms | componentMap.get() 是 O(1) |
| Parser init (8步) | 10-100ms | 取决于字段数量和远端数据接口 |
| buildRelations (递归扫描) | 1-20ms | O(n) 字段数量 |
| comSet (递归查找) | <5ms | O(n) 最坏情况 |
| handleRelation (联动执行) | 50-500ms | 取决于远端接口响应 |
| renderKey 强制重渲染 | +16ms | 每个受影响的组件一次完整渲染周期 |
| FormGenerator 拖拽 | <16ms | vue-draggable 性能可接受 |

---

## 十、数据格式与转换矩阵

### 10.1 System A: FormSchema 格式

```typescript
interface FormSchema {
  field: string;           // 字段名 'userName'
  label: string;           // 标签 '用户名'
  component: ComponentType; // 组件 'Input'
  componentProps?: any;    // 组件属性或函数
  defaultValue?: any;      // 默认值
  required?: boolean;      // 必填
  rules?: Rule[];          // 自定义规则
  ifShow?: Function;       // 条件显示函数
  dynamicDisabled?: Function; // 动态禁用函数
  dynamicRules?: Function; // 动态规则函数
  colProps?: ColEx;        // 栅格属性
}
```

### 10.2 System B: GenItem 格式

```typescript
interface GenItem {
  __config__: {
    jnpfKey: string;       // 'input', 'select', 'datePicker', ...
    label: string;
    layout: 'colFormItem' | 'rowFormItem';
    span: number;
    required: boolean;
    defaultValue: any;
    regList: Array<{ pattern: string; message: string }>;
    trigger: string;
    renderKey: number;     // 强制重渲染的 key
    formId: number;        // 唯一 ID
    children?: GenItem[];  // 嵌套组件
    noShow: boolean;
    visibility: string[];  // ['pc', 'app']
    dataType?: 'static' | 'dictionary' | 'dynamic';
    // ... 20+ 更多属性
  };
  __vModel__: string;      // 字段名
  __slot__?: any;
  disabled?: boolean;
  placeholder?: string;
  on?: Record<string, string>; // 事件脚本字符串
  options?: any[];
  // 组件特有属性...
}
```

### 10.3 Designer JSON → Runtime VNode 转换链

```
Designer drawingList (GenItem[])
  │ assembleFormData()
  ▼
formConf = { fields: GenItem[], ...config }
  │ Parser.init()
  ▼
formConfCopy (深拷贝)
  │ renderFormItem()
  ▼
layouts.colFormItem(GenItem) / layouts.rowFormItem(GenItem)
  │ render.ts → buildDataObject(confClone, dataObject, formData)
  │ componentMap.get(upperFirst(jnpfKey))
  │ h(Comp, realProps)
  ▼
Vue VNode → DOM
```

---

## 十一、改进建议 (未纳入本阶段范围)

1. **统一表单系统** — BasicForm 和 FormGenerator 合并为一个引擎，通过 adapter 处理两种 Schema 格式
2. **移除 eval()** — 用户脚本改为沙箱执行（Web Worker / sandboxed iframe / Function constructor with CSP）
3. **Parser 拆分** — 将 831 行拆分为 useInit、useRelations、useValidation、useLayout 四个 composable
4. **componentMap 作用域化** — 支持 scope 参数，避免多实例冲突
5. **GenItem ↔ FormSchema 互转层** — 允许 Generated 表单使用 BasicForm 渲染，CRUD 表单使用 Parser 渲染
6. **renderKey 机制改为响应式** — 用 `reactive()` 包装 GenItem，自动触发重渲染而非手动改 key
