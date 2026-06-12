# 08 — 组件架构扫描

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 (主)

---

## jnpf-web-vue3 组件体系

### 核心封装组件（JNPF 框架基石）

| 组件 | 模式 | 用途 |
|---|---|---|
| **BasicTable** | useTable() → register | Schema 驱动表格 + 搜索 + 分页 |
| **BasicForm** | useForm() → register | Schema 驱动表单渲染 |
| **BasicModal** | useModal() → register | 统一弹窗管理 |
| **BasicDrawer** | useDrawer() → register | 抽屉式表单 |
| **TableAction** | Props: ActionItem[] | 操作列按钮/下拉渲染 |
| **FormItem** | componentMap 动态渲染 | Schema → 组件映射 |

### Schema 驱动模式

```typescript
// 表单
const schemas: FormSchema[] = [
  { field: 'userName', label: '用户名', component: 'Input', required: true }
];
const [registerForm, { getFieldsValue }] = useForm({ schemas });

// 表格
const [registerTable, { reload }] = useTable({
  api: getListAPI,
  columns: [{ title: '名称', dataIndex: 'fullName' }],
  actionColumn: [{ label: '编辑', onClick: handleEdit }]
});
```

### 组件映射表 (componentMap)

60+ 字符串到组件的映射：`Input`, `Select`, `DatePicker`, `OrganizeSelect`, `PopupSelect`, `RelationForm`, `InputTable`, `Editor`, `Sign`, `Signature`, `Upload`...

### JNPF 领域组件 (44 个子目录)

表单: Input / Select / DatePicker / Cascader / TreeSelect / ColorPicker / Rate / Slider / Editor / Upload / Sign / Signature / Barcode / Qrcode

组织: OrganizeSelect / DepSelect / PosSelect / GroupSelect / RoleSelect / UserSelect / UsersSelect

复合: PopupSelect / PopupTableSelect / RelationForm / InputTable / NumberRange / Cron / Location

### 通信模式

- **mitt** 事件总线: 路由变更通知、全局事件
- **provide/inject**: TableContext、FormContext、ModalContext
- **@register 事件**: 子组件 emit → 父组件 hook 接收 → 类型安全的方法调用

### 公共组件清单 (31 个子目录)

布局: Container/ (Scroll/Collapse/Lazy), Scrollbar, Page

UI: Application/, Basic/, Button/, ClickOutSide/, ContextMenu/, Dropdown/, CountDown/, CountTo/

编辑: CodeEditor/ (Monaco+CodeMirror), Chart/, Cropper/, CardList/, Description/

流: FlowChart/, FlowProcess/

工具: Excel/, PrintDesign/, Icon/, StrengthMeter/, CommonModal/, Preview/

---

## jnpf-web-datascreen 组件体系

### 大屏组件 (35 个包)

图表: bar, line, pie, scatter, funnel, gauge, radar, map, wordCloud, pictorialBar

展示: table, text, data, datetime, flop, progress, img, svg, html, iframe

媒体: video (Clappr), audio, swiper

容器: group, tabs, borderBox, decoration, rectangle

### 设计器

完整的拖拽式大屏设计器，包含: 画布(`container.vue`), 标尺(`vue3-sketch-ruler`), 右击菜单, 数据源配置, 动画设置, 组件面板, 图层管理

### 屏幕适配

CSS `transform: scale()` 方案（1920×1080 基准 → 等比缩放）

---

## jnpf-app-vue3 组件体系

### UI 框架

**双框架:** vk-uview-ui (uView Vue3 fork) + @dcloudio/uni-ui (52 个 uni_modules)

### JNPF 自定义组件 (~30 个)

通过 easycom 自动注册: `Jnpf*` → `@/components/Jnpf/$1/index.vue`

主要组件: Input, Select, DatePicker, Cascader, Checkbox, Radio, Switch, Slider, Upload, Sign, Signature, Barcode, Qrcode, Editor, OrganizeSelect, DepSelect, UserSelect...

---

## 关键发现 (组件层)

| # | 发现 | 严重度 |
|---|---|---|
| 1 | web-vue3 组件体系成熟 — Schema 驱动 + 注册模式，设计良好 | ✅ |
| 2 | datascreen 组件 100% Options API (Vue 2)，迁移困难 | 高 |
| 3 | app-vue3 双 UI 框架冗余 — uView + uni-ui 功能重叠 | 中 |
| 4 | web-vue3 IconPicker 数据文件 12,588 行 — 应考虑按需加载 | 中 |
| 5 | 三项目组件库零共享 — 每个项目独立实现同类功能 | 高 |
