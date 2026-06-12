# CT Scan 3.1: 组件架构分析报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 (Jnpf 组件库) / jnpf-web-datascreen (echart 工厂) / jnpf-app-vue3 (移动端 Jnpf)

---

## 一、jnpf-web-vue3 组件生态

### 1.1 组件总量

| 类别 | 数量 | 说明 |
|---|---|---|
| 全局组件 (components/) | 618 文件, ~50 目录 | 全量注册 |
| Jnpf 核心库 | 140 文件 | 表单/表格/弹窗/选择器等 |
| FormGenerator | 78 文件 | Schema 驱动的表单生成器 |
| VisualPortal | 64 文件 | 可视化门户设计器 |
| Table | 42 文件 | 表格相关组件 |
| FlowProcess | 22 文件 | 流程设计器 |
| IntegrateProcess | 20 文件 | 集成流程 |
| 其他 | ~252 文件 | 上传/预览/菜单/弹窗等 |

### 1.2 Jnpf 核心组件库 (140 文件)

JNPF 框架的"骨架"组件，用法不可更改 (前端重构铁律 #3):

| 组件 | 说明 |
|---|---|
| `BasicTable` / `JnpfTable` | 核心表格 — API驱动 + 列配置 + 操作列 + 插槽系统 |
| `BasicForm` / `JnpfForm` | Schema 驱动表单 — FormSchema[] → 完整表单 |
| `BasicPopup` / `BasicModal` | 弹窗容器 — 表单弹窗标准容器 |
| `jnpf-content-wrapper` | 页面容器 — 标准 CRUD 页面外层包裹 |
| `JnpfSelect` | 选择器 — 字典数据/用户/组织/角色等 |
| `JnpfUpload` | 上传组件 — 文件/图片上传 |
| `JnpfDatePicker` | 日期选择器 |
| `JnpfTreeSelect` | 树形选择器 |
| `TableAction` | 表格操作列按钮组 |

### 1.3 组件注册机制

**全局注册** (registerGlobComp):
- 所有 `components/` 下组件通过 `app.component()` 全局注册
- Jnpf 组件通过统一入口注册 (无需手动 import)

```typescript
// 伪代码: 全局注册模式
Object.keys(components).forEach(key => {
  app.component(key, components[key])
})
```

### 1.4 FormGenerator Schema 模式

FormGenerator 是 JNPF 的核心表单引擎，78 个文件实现:

```typescript
// Schema 驱动的表单定义
interface FormSchema {
  field: string;          // 绑定字段名
  label: string;          // 标签
  component: string;      // 组件类型: 'Input' | 'Select' | 'DatePicker' | ...
  componentProps?: any;   // 组件属性
  required?: boolean;     // 必填
  rules?: Rule[];         // 验证规则
  ifShow?: (values) => boolean; // 条件显示
  // ... 更多
}
```

**支持的字段类型 (45+):**
Input, Textarea, Number, Select, Cascader, DatePicker, TimePicker, Switch, Radio, Checkbox, Rate, Slider, Upload, TreeSelect, PopupSelect, OrganizeSelect, UserSelect, RoleSelect, GroupSelect, DepSelect, PosSelect, AreaSelect, Sign, Editor, ColorPicker, Calculate, Barcode, Qrcode, Location, Divider, GroupTitle, Link, AutoComplete, ...

---

## 二、jnpf-web-datascreen 组件工厂

### 2.1 EChart 组件自动发现

```javascript
// src/echart/index.js
const modules = import.meta.globEager('./packages/**/*.vue')
Object.keys(modules).forEach(key => {
  const name = key.replace(/(.*\/)*([^.]+).*/ig, "$2")
  Vue.component(`avue-echart-${name}`, modules[key].default)
})
```

**34 个已注册图表组件:**

| 类别 | 组件 |
|---|---|
| 基础图表 | bar, line, pie, scatter, radar, funnel, gauge, pictorialBar |
| 数据展示 | table, text, data, progress, flop |
| 地图 | map |
| 媒体 | img, imgBorder, video, audio, svg, iframe, html |
| 布局 | borderBox, decoration, rectangle, tabs, group |
| 时间 | datetime, time |
| 其他 | clappr, common, datav, graph, swiper, vue, wordCloud |

### 2.2 组件工厂架构

每个 echart 组件遵循:
1. **Vue 组件** (`echart/packages/<name>/`) — 渲染组件
2. **配置面板组件** (`option/components/<name>/`) — 属性编辑器
3. **配置数据** (`public/config.js` — `baseList[]`) — 默认配置/属性定义
4. **通用逻辑** (`echart/index.js`) — 数据提取/组件注册/公共方法

### 2.3 配置驱动架构

所有组件通过 `public/config.js` 中的 `baseList` 数组定义:
- `name`: 组件名称
- `icon`: 图标
- `option`: 默认配置对象 (尺寸/样式/数据/动画)
- `prop`: 属性定义 (每个属性的类型/默认值/编辑器)
- `wh`: 宽高比

---

## 三、jnpf-app-vue3 移动端组件生态

### 3.1 组件总量

| 来源 | 数量 | 说明 |
|---|---|---|
| components/Jnpf/ | 49 组件 | JNPF 移动端核心库 |
| uni_modules/vk-uview-ui/ | 90+ 组件 | 第三方 UI 库 |
| uni_modules/uni-ui/ | 47 模块 | UniApp 官方组件库 |
| 其他自定义 | 8 组件 | assistantMsg, CommonTabs, CustomButton, ... |

### 3.2 Jnpf 移动端组件 (49 个)

完整表单控件 + 业务组件:
`Alert`, `AreaSelect`, `AutoComplete`, `Barcode`, `Button`, `Calculate`, `Cascader`, `Checkbox`, `ColorPicker`, `DatePicker`, `DateRange`, `DepSelect`, `Divider`, `Editor`, `GroupSelect`, `GroupTitle`, `Input`, `InputNumber`, `Link`, `Location`, `NumberRange`, `OpenData`, `OrganizeSelect`, `Parser`, `PopupAttr`, `PopupSelect`, `PosSelect`, `Qrcode`, `Radio`, `Rate`, `RelationForm`, `RelationFormAttr`, `RoleSelect`, `Select`, `Sign`, `Signature`, `Slider`, `Steps`, `Switch`, `Text`, `Textarea`, `TimePicker`, `TimeRange`, `TreeSelect`, `UploadFile`, `UploadFileComment`, `UploadImg`, `UserSelect`, `UsersSelect`

### 3.3 easycom 自动注册

```javascript
// pages.json
"easycom": {
  "^Jnpf(.*)": "@/components/Jnpf/$1/index.vue",
  "^jnpf-(.*)": "@/components/Jnpf/$1/index.vue"
}
```

UniApp 的 easycom 机制自动将 `JnpfXxx` 标签解析为对应组件，无需手动 import/注册。

---

## 四、三项目组件对比矩阵

| 功能 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 | 共享? |
|---|---|---|---|---|
| 表格 | BasicTable (API驱动) | avue-echart-table (配置驱动) | — | ❌ |
| 表单输入 | JnpfInput / FormGenerator | — | JnpfInput | ❌ |
| 选择器 | JnpfSelect | — | JnpfSelect | ❌ |
| 日期选择 | JnpfDatePicker | avue-echart-datetime | JnpfDatePicker | ❌ |
| 上传 | JnpfUpload | — | JnpfUploadFile/UploadImg | ❌ |
| 弹窗 | BasicPopup/BasicModal | Element Plus Dialog | uni-popup | ❌ |
| 树选择 | JnpfTreeSelect | — | JnpfTreeSelect | ❌ |
| 按钮 | (Ant Design a-button) | Element Plus el-button | JnpfButton | ❌ |
| 布局 | jnpf-content-wrapper | page/index.vue | — | ❌ |

**结论:** 三项目组件零共享。同名组件 (如 JnpfSelect) 在不同项目中是**完全独立**的实现。

---

## 五、代码生成集成

### 5.1 jnpf-web-vue3: onlineDev 动态页面

- `views/onlineDev/` (18 文件): 在线开发功能的配置界面
- `views/generator/` (6 文件): 代码生成器界面
- `store/modules/generator.ts`: 代码生成状态管理
- **动态组件**: `dynamicModel/`, `dynamicDictionary/`, `dynamicDataReport/`, `dynamicPortal/`
- 这些组件加载**后端 .vm 模板生成**的配置 JSON, 在前端动态渲染表格/表单

### 5.2 jnpf-app-vue3: dynamicModel

- `pages/apply/dynamicModel/`: 动态表单/列表/详情/扫码表单
- 同样是加载后端配置 JSON 动态渲染
- 与 web-vue3 的 dynamicModel 功能对应但实现完全不同

### 5.3 模板边界

**铁律 R3**: 代码生成模板 (`.vm` 文件) 产生的文件不可直接修改。生成页面的 UI 组件使用 Jnpf 核心组件库, 但**页面结构由模板决定**。

---

## 六、组件架构问题

1. **组件重复**: 三项目独立实现同功能组件, 无共享
2. **组件发现困难**: jnpf-web-vue3 的 618 个组件文件, 无 barrel export 索引
3. **废弃 API**: datascreen 使用 `import.meta.globEager` (Vite 2.x 废弃 API)
4. **全局注册膨胀**: web-vue3 注册所有组件为全局, 无法 tree-shake
5. **命名不一致**: 同一组件在不同项目中命名不同 (JnpfTable vs avue-echart-table)
6. **移动端组件过重**: app 同时依赖 vk-uview-ui (90+) + uni-ui (47模块) + Jnpf (49), 三套组件共存
