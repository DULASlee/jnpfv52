# Qi 2: 代码生成器预览（web-vue3）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，标注每个环节的数据格式和转换方式
> 诊断范围: jnpf-web-vue3 代码生成器可视化配置全链路

---

## 一、整体架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      代码生成器 (前端 - 可视化设计器)                     │
│                                                                         │
│  Step 0: 基础设置          Step 1: 表单设计       Step 2: 列表设计      │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │ 模板名称/编码     │  │ FormGenerator     │  │ BasicColumnDesign    │  │
│  │ 模板分类/类型     │  │ (drag-drop 设计器) │  │ (列配置 + 搜索 +     │  │
│  │ 数据连接(DB)      │  │                   │  │  按钮 + 权限)        │  │
│  │ 选择数据表        │  │ → formData JSON   │  │ → columnData JSON    │  │
│  │ (主表+从表)      │  │                   │  │                      │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────────┘  │
│           │                     │                       │               │
│           └─────────────────────┴───────────────────────┘               │
│                                 │                                       │
│                                 ▼                                       │
│                    handleRequest() 保存到后端                            │
│                    POST/PUT /api/visualdev/Base                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      代码生成器 (后端 - .vm 模板引擎)                     │
│                                                                         │
│  PreviewModal                    DownloadModal                           │
│  ┌──────────────────────┐       ┌──────────────────────┐               │
│  │ codePreview(id, data)│       │ downloadCode(id, data)│               │
│  │ POST /Generater/     │       │ POST /Generater/     │               │
│  │   {id}/CodePreview   │       │   {id}/DownloadCode  │               │
│  │                      │       │                      │               │
│  │ 返回: 文件树 + 代码  │       │ 返回: ZIP 下载 URL   │               │
│  │ Monaco Editor 预览   │       │ 浏览器下载 ZIP       │               │
│  └──────────────────────┘       └──────────────────────┘               │
└─────────────────────────────────────────────────────────────────────────┘
```

**核心发现: 前端不执行模板渲染。实际的代码生成（.vm 模板 + Velocity 引擎）全部在后端完成。前端是可视化配置数据采集器。**

---

## 二、双模式向导

### 2.1 webForm (3 步向导，普通/流程表单)

**文件:** `views/generator/webForm/Form.vue` (528 lines)

```
Step 0: 基础设置 → Step 1: 表单设计 → Step 2: 列表设计
  │                    │                    │
  │ BasicForm          │ FormGenerator      │ BasicColumnDesign
  │ (6 个字段)          │ (drag-drop)        │ (Main + MainApp)
  │                    │                    │
  │ 数据表选择          │ 产生 formData      │ 产生 columnData
  │ (主表+从表)         │ (GenItem[] JSON)   │ (搜索+列+按钮)
```

**配置字段 (Step 0):**
| 字段 | 类型 | 说明 |
|---|---|---|
| fullName | Input | 模板名称 |
| enCode | Input | 模板编码 (字母+数字) |
| category | Select | 模板分类 |
| enableFlow | Radio | 普通表单(0) / 流程表单(1) |
| description | Textarea | 模板说明 |
| dbLinkId | Select | 数据连接 |

**数据表配置:**
| 字段 | 说明 |
|---|---|
| typeId | '1'=主表, '0'=从表 |
| table | 数据库表名 |
| tableField | 从表外键字段 (关联到主表) |
| relationField | 主表被关联字段 |
| fields | 表字段列表 (从后端加载) |

### 2.2 flowForm (2 步向导，纯流程表单)

**文件:** `views/generator/flowForm/Form.vue` (490 lines)

与 webForm 几乎完全相同，差异：
- `enableFlow` 默认值为 `1`（流程表单）
- `webType` 默认值为 `1`（纯表单，无列表）
- 无 Step 2 列表设计

### 2.3 代码重复度分析

webForm/Form.vue 和 flowForm/Form.vue 的重复率 ~85%。差异仅在于：
- `dataForm.enableFlow` 默认值 (0 vs 1)
- `dataForm.webType` 默认值 (2 vs 1)
- flowForm 无 `dataForm.enableFlow` 的 Radio 字段
- webForm 多了 `toggleWebType` 函数

**代码异味:** 两个 ~500 行文件仅 15% 差异，应抽取公共组件。

---

## 三、数据采集与转换

### 3.1 Step 0 → Step 1 转换

```typescript
// Form.vue:413-420
const subTable = state.tables.filter(o => o.typeId == '0');
generatorStore.setHasTable(true);           // 标记有数据表
generatorStore.setAllTable(state.tables);   // 全部表信息
generatorStore.setSubTable(subTable);        // 从表列表
generatorStore.setFormItemList(state.mainTableFields); // 主表字段
```

**generatorStore 是跨步骤数据总线:**
```typescript
// store/modules/generator.ts
interface BaseState {
  hasTable: boolean;       // FormGenerator 判断是否显示子表配置
  allTable: any[];         // 所有选中的表
  subTable: any[];         // 从表列表
  formItemList: any[];     // 主表字段 (作为表单设计器的可选字段源)
  relationData: any;       // 表单联动数据
  dynamicModelExtra: any;  // 动态模型额外参数
}
```

### 3.2 Step 1 → Step 2 转换

```typescript
// Form.vue:423-431
generatorRef.getData().then(res => {
  state.formData = res.formData;  // FormGenerator 的完整输出
  // formData = { fields: GenItem[], ...formConf }
  state.dataForm.formData = JSON.stringify(state.formData);
  state.activeStep += 1;
});
```

### 3.3 保存数据格式

```typescript
// Form.vue:505-513
const query = {
  ...state.dataForm,          // 基础信息
  tables: JSON.stringify(state.tables),           // 表配置 → JSON 字符串
  formData: JSON.stringify(state.formData),       // 表单设计 → JSON 字符串
  columnData: JSON.stringify(state.columnData),   // 列表设计 → JSON 字符串
  appColumnData: JSON.stringify(state.appColumnData), // 移动端列表设计 → JSON 字符串
};
```

**数据存储层级:**
```
Backend Database Row:
  ├── fullName, enCode, category, enableFlow, ...
  ├── tables: TEXT (JSON array string)
  ├── formData: TEXT (JSON object string, GenItem tree)
  ├── columnData: TEXT (JSON object string)
  └── appColumnData: TEXT (JSON object string)
```

---

## 四、代码预览 (PreviewModal)

### 4.1 预览请求

```typescript
// PreviewModal.vue:63-78
function init(data) {
  const tablesList = data.tables ? JSON.parse(data.tables) : [];
  let dataForm = {
    module: 'system',
    description: '',
    subClassName: '',
    className: ''
  };
  // 解析主表名和从表名
  for (let i = 0; i < tablesList.length; i++) {
    if (tablesList[i].typeId == '1') {
      dataForm.className = tablesList[i].table;    // 主表 = className
      dataForm.description = tablesList[i].table;
    } else {
      subClassName.push(tablesList[i].table);       // 从表名列表
    }
  }
  dataForm.subClassName = subClassName.join();       // 逗号拼接
  
  codePreview(data.id, dataForm).then(res => {
    // res.data.list: 文件树 [{ fileName, children: [{ id, fileContent, fileType }] }]
  });
}
```

### 4.2 预览渲染

```
PreviewModal 布局:
┌──────────────────────────────────────────────────────────┐
│ [jnpf logo] · 代码预览                    [取消]         │
├───────────────┬──────────────────────────────────────────┤
│ BasicLeftTree │        Monaco Editor                     │
│ (文件树)       │        (只读模式)                        │
│               │                                          │
│ ├─ web/       │   <generated code content>               │
│ │  ├─ index.vue│                                         │
│ │  └─ ...     │                                          │
│ ├─ app/       │                                          │
│ └─ java/      │                                          │
│    ├─ Controller.java                                    │
│    ├─ Service.java                                       │
│    └─ ...                                                │
└───────────────┴──────────────────────────────────────────┘
```

**树选择逻辑:**
```typescript
// 默认选中第一个文件的第一个子节点
state.currentId = state.treeData[0].children[0].id;
state.currentContent = state.treeData[0].children[0].fileContent;
// 根据 fileType 切换编辑器语言
state.editorLanguage = ['web', 'app'].includes(fileType) ? 'html' : 'java';
```

---

## 五、代码下载 (DownloadModal)

### 5.1 下载请求

```typescript
// DownloadModal.vue:91-111
async function handleSubmit() {
  const values = await formElRef.value?.validate();
  const subClassName = state.dataForm.subClassName.map(o => o.fullName);
  const query = {
    module: state.dataForm.module,           // 模块 (如 'system')
    className: state.dataForm.className,     // 类名 (主表名)
    subClassName: subClassName.join(','),    // 从表类名 (逗号分隔)
    description: state.dataForm.description, // 功能描述
    modulePackageName: state.dataForm.modulePackageName, // 包名
  };
  downloadCode(state.id, query).then(res => {
    downloadByUrl({ url: res.data.url });    // 直接下载 ZIP
  });
}
```

### 5.2 配置选项

| 选项 | 说明 | 默认值 |
|---|---|---|
| module | 模块命名 (下拉选择，数据字典 'createModule') | 第一个选项 |
| modulePackageName | 模块包名 (仅 Package 模式) | 'jnpf' |
| description | 功能描述 | 主表表名 |
| className | 功能类名 | 主表表名 |
| subClassName[N] | 子表类名 (每个从表一个输入框) | 从表表名 |

---

## 六、ColumnDesign 列表设计器

### 6.1 设计器结构

**文件:** `components/ColumnDesign/src/BasicColumnDesign.vue` (57 lines)

```
BasicColumnDesign
├─ PC 端: Main.vue (columns/search/buttons/funcs)
├─ App 端: MainApp.vue (移动端列配置)
└─ 切换按钮: PC / App
```

### 6.2 默认配置

**文件:** `components/ColumnDesign/src/helper/config.ts` (145 lines)

`defaultColumnData` 包含:
```
{
  ruleList: {},           // 过滤规则 (matchLogic: 'and', conditionList: [])
  searchList: [],         // 查询字段配置
  hasSuperQuery: true,    // 高级查询
  columnList: [],         // 列配置 (显示哪些列 + 列宽 + 对齐 + 固定)
  columnOptions: [],      // 可选列
  defaultColumnList: [],  // 所有可选字段
  type: 1,                // 列表类型
  defaultSortConfig: [],  // 默认排序
  hasPage: true,          // 分页
  pageSize: 20,           // 分页条数
  hasTreeQuery: false,    // 左侧树查询
  // 树配置...
  groupField: '',         // 分组字段
  parentField: '',        // 父级字段
  useColumnPermission: false,
  useFormPermission: false,
  useBtnPermission: false,
  useDataPermission: false,
  customBtnsList: [],     // 自定义按钮
  btnsList: [{ value: 'add', ... }],      // 默认按钮
  columnBtnsList: [                        // 行操作按钮
    { value: 'edit', label: '编辑' },
    { value: 'remove', label: '删除' },
    { value: 'detail', label: '详情' },
  ],
  funcs: {                // 脚本函数 (eval 执行)
    afterOnload: '({ data, tableRef, onlineUtils }) => { ... }',
    rowStyle: '({ row, rowIndex }) => { ... }',
    cellStyle: '({ row, column, rowIndex, columnIndex }) => { ... }',
  },
}
```

### 6.3 查询字段配置

搜索字段类型映射 (config.ts:25-36):
```typescript
const getSearchType = item => {
  // 1 = 精确, 2 = 模糊, 3 = 范围
  if (RangeList.includes(jnpfKey)) return 3;  // date/time/number等 → 范围查询
  if (fuzzyList.includes(jnpfKey)) return 2;  // input/textarea → 模糊查询
  return 1;                                    // 其他 → 精确查询
};
```

---

## 七、前后端代码生成契约

### 7.1 前端 → 后端数据流

```
Frontend (保存视觉设计配置)
  │
  │ POST/PUT /api/visualdev/Base
  │ Body: { ...基础字段, tables, formData, columnData, appColumnData }
  │
  ▼
Backend (存储到数据库)
  │
  │ GET /api/visualdev/Generater/{id}/Actions/CodePreview
  │ POST /api/visualdev/Generater/{id}/Actions/DownloadCode
  │
  │ 后端执行:
  │ 1. 读取 .vm 模板 (wwwroot/Template/)
  │ 2. Velocity 引擎渲染
  │ 3. 输出代码文件
  │
  ▼
Response to Frontend
```

### 7.2 模板变量来源

前端采集的变量最终映射到后端 .vm 模板的 Velocity 变量：

| 前端数据 | 后端模板变量 (推测) | 用途 |
|---|---|---|
| dataForm.className | `$entity.Name` | 实体类名、Service 名 |
| dataForm.description | `$entity.Description` | 注释/文档 |
| tables[].table | `$entity.TableName` | 数据库表名 |
| tables[].fields[] | `$entity.Fields` | 实体字段列表 |
| formData.fields[] | `$formConfig` | 表单渲染配置 |
| columnData.columnList[] | `$columnConfig` | 列表渲染配置 |
| tables[].typeId | 主表/从表判断 | 生成不同代码 |

### 7.3 模板目录结构 (后端)

```
wwwroot/Template/
├── 1-SingleTable/          # 单表模板
│   ├── Service.cs.vm
│   ├── InlineEditor/
│   │   └── Service.cs.vm
│   └── ...
├── 2-MainBelt/             # 主从表模板
│   ├── Service.cs.vm
│   └── ...
├── 5-PrimarySecondary/     # 主子表模板
│   ├── Service.cs.vm
│   └── ...
└── ...
```

---

## 八、发现汇总

### P0 安全红线

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| Q2-1 | ColumnDesign `funcs` 使用 `eval()` 执行脚本 | config.ts:46-50 | 存储型 XSS |
| Q2-2 | 与 Qi 1 相同的 eval 问题 (FormGenerator 的 onLoad/beforeSubmit) | FormGenerator | 已覆盖 |

### P1 架构问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| Q2-A1 | **webForm/Form.vue 与 flowForm/Form.vue 85% 代码重复** | generator/webForm + generator/flowForm | 维护成本翻倍 |
| Q2-A2 | **前端无法独立验证模板渲染结果** — 预览必须调用后端 API | PreviewModal.vue | 离线不可用，调试缓慢 |
| Q2-A3 | **JSON.stringify 嵌套存储** — formData/columnData 序列化为字符串再存入数据库 | Form.vue:509-512 | JSON→字符串→JSON 容易损坏 |
| Q2-A4 | **generatorStore 全局单例** — 同页面多个设计器实例会状态冲突 | generator.ts | 并发 bug |
| Q2-A5 | **dbType 从 dataForm.dbLinkId 反向查找** — 脆弱的数据类型推断 | Form.vue:313-328 | 多数据源时易出错 |
| Q2-A6 | **预览时 className 硬编码为 'system'** — module 不参与预览请求 | PreviewModal.vue:67 | 预览与下载使用不同模块名 |
| Q2-A7 | **tableField/relationField 无类型安全** — 纯字符串匹配 | webForm/Form.vue | 重构时易遗漏 |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| Q2-E1 | ColumnDesign 的 `getData()` 返回空时 reject `{ msg: '', target: 2 }` — 错误信息为空 | BasicColumnDesign.vue:46 |
| Q2-E2 | `state.dataForm` 类型为 `Recordable` — 无类型约束 | Form.vue |
| Q2-E3 | Monaco Editor language 硬编码为 'html' / 'java' — 不支持其他语言 | PreviewModal.vue:83,95 |
| Q2-E4 | DownloadModal 的 `downloadByUrl` 依赖后端返回 URL — 无进度条/断点续传 | DownloadModal.vue:106 |
| Q2-E5 | 表字段加载使用 await 循环 (串行) — 多表时性能差 | Form.vue:288-296 |
| Q2-E6 | `toggleWebType` 切换 webType=1/2 时仅前端的 confirm 提示 — 无后端验证 | Form.vue:270-278 |

---

## 九、数据格式与转换矩阵

### 9.1 完整的可视化配置数据流

```
用户操作
  │
  ├─ Step 0: 填写基础信息 + 选择数据表
  │   → dataForm: { fullName, enCode, ... }
  │   → tables: [{ typeId, table, tableField, relationField, fields[] }]
  │   → generatorStore 更新 (跨步骤共享)
  │
  ├─ Step 1: 拖拽设计表单
  │   → FormGenerator.getData()
  │   → formData: { fields: GenItem[], ...formConf }
  │   → JSON.stringify → 存储
  │
  ├─ Step 2: 配置列表视图
  │   → ColumnDesign.getData()
  │   → columnData: { searchList[], columnList[], btnsList[], funcs, ... }
  │   → appColumnData: { /* 移动端配置 */ }
  │
  └─ handleRequest():
      POST/PUT /api/visualdev/Base
      Body: { ...dataForm, tables: "[...]", formData: "{...}", columnData: "{...}" }
```

### 9.2 预览/下载数据流

```
列表页 → 点击"预览" → PreviewModal.init(data)
  │
  │ data.id → visualDev record ID
  │ data.tables → JSON string → parse → extract className/subClassName
  │
  ├─ codePreview(id, { module, description, subClassName, className })
  │   → POST /api/visualdev/Generater/{id}/Actions/CodePreview
  │   → 后端: 读取 visualDev 记录 → 解析 JSON → .vm 模板渲染 → 返回文件列表
  │   → 前端: 文件树 + Monaco Editor 展示
  │
  └─ downloadCode(id, { module, description, subClassName, className, modulePackageName })
      → POST /api/visualdev/Generater/{id}/Actions/DownloadCode
      → 后端: 同预览 + 打包 ZIP + 返回下载 URL
      → 前端: downloadByUrl(res.data.url)
```

---

## 十、改进建议 (未纳入本阶段范围)

1. **合并 webForm 和 flowForm** — 抽取公共组件 FormWizard，通过 props 控制差异
2. **预览独立于后端** — 前端保留 .vm 模板副本，提供离线预览能力
3. **generatorStore 改为 scoped provide/inject** — 支持多设计器实例共存
4. **ColumnDesign 添加类型定义** — TypeScript interface 代替 Recordable
5. **表字段并行加载** — Promise.all 替代 for-await 循环
6. **预览 module 参数化** — 支持切换不同模块查看生成的包路径差异
