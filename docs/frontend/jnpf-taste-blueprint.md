# JNPF Vue3 前端品味蓝图 v2.1

> 本文件是 JNPF 前端手工开发的唯一权威参考。
> 每次开发新页面前必须先 Read 本文件。
> **禁止复制本文件中的骨架片段作为完整页面使用。**
> **必须 Read 黄金页面索引中的真实文件作为完整参照。**

---

## 一、技术栈全景

| 维度 | 实际使用 |
|------|---------|
| UI 框架 | Ant Design Vue 3.2.20 |
| 样式 | Less + WindiCSS 工具类 |
| 主题 | Less modifyVars（@primary-color 等） |
| 骨架类 | jnpf-content-wrapper 系列（定义在 common.less） |
| 表格 | BasicTable（封装 a-table） |
| 表单 | BasicForm（schema 驱动）或手写 a-form |
| 弹窗 | BasicPopup / BasicModal |
| 自定义组件 | 72+ 个 jnpf-* 全局注册组件 |
| 路由 | 后端菜单动态注入 |
| 状态管理 | Pinia |
| 路径别名 | /@/ 指向 src |

---

## 二、黄金页面索引（开发前必须 Read 对应文件）

| 页面类型 | 参照文件 | 说明 |
|----------|---------|------|
| 标准列表页 | `jnpf-web-vue3/src/views/system/billRule/index.vue` | BasicTable + TableAction + 搜索内嵌 |
| 左树右表 | `jnpf-web-vue3/src/views/permission/user/index.vue` | BasicLeftTree + 右侧 BasicTable |
| 弹窗表单 A | `jnpf-web-vue3/src/views/permission/user/Form.vue` | BasicPopup + BasicForm(FormSchema) |
| 弹窗表单 B | `jnpf-web-vue3/src/views/system/billRule/Form.vue` | BasicModal + BasicForm(FormSchema) |
| 全页表单 | `jnpf-web-vue3/src/views/extend/formDemo/fieldForm1/index.vue` | a-divider 分区 + ScrollContainer |
| 复杂业务表单 | `jnpf-web-vue3/src/views/extend/saleOrder/Form.vue` | a-row 嵌套 + 子表 |

**使用方式：开发前根据场景选择上表对应的文件，Read 后参照其完整代码。**
**弹窗表单有两条成熟路径（BasicPopup 和 BasicModal），优先参照同类场景。**

---

## 三、骨架决策树

### 场景 A：标准列表页 / CRUD

```
jnpf-content-wrapper
  └── jnpf-content-wrapper-center
        └── jnpf-content-wrapper-content
              └── BasicTable（搜索内嵌，通过 useSearchForm: true）
```

搜索通过 BasicTable 的 `useSearchForm` + `formConfig.schemas` 配置，
不在外层手写 a-form 搜索栏。

操作列通过 `#bodyCell` slot + `TableAction` 组件实现。
useTable 中需配置 actionColumn：

```typescript
const [registerTable, { reload, getSelectRows }] = useTable({
  api: getListApi,
  columns: [
    { title: '名称', dataIndex: 'name', width: 180, ellipsis: true },
    { title: '操作', dataIndex: 'action', width: 160, fixed: 'right' },
  ],
  actionColumn: { width: 160, title: '操作', dataIndex: 'action' },
  useSearchForm: true,
  formConfig: {
    schemas: [
      { field: 'keyword', label: '关键词', component: 'Input' },
    ],
  },
  pagination: { pageSize: 20 },
  rowSelection: {},
})
```

模板中操作列写法：

```html
<BasicTable @register="registerTable">
  <template #bodyCell="{ column, record }">
    <template v-if="column.key === 'action'">
      <TableAction :actions="getTableActions(record)" />
    </template>
  </template>
</BasicTable>
```

### 场景 B：左树右表

```
jnpf-content-wrapper
  └── jnpf-content-wrapper-left
  │     └── BasicLeftTree
  └── jnpf-content-wrapper-center
        └── jnpf-content-wrapper-content
              └── BasicTable
```

### 场景 C：弹窗表单（最常用模式）

两条成熟路径，优先参照同类场景的已有页面：

**路径 A — BasicPopup + BasicForm：**

```
BasicPopup
  └── BasicForm :schemas="formSchemas"
```

**路径 B — BasicModal + BasicForm：**

```
BasicModal
  └── BasicForm :schemas="formSchemas"
```

**路径 C — 手写 a-form（字段较多或布局复杂时）：**

```
BasicPopup 或 BasicModal
  └── a-form
        └── a-row :gutter="20"
              ├── a-col :span="12"（表单项）
              └── a-col :span="12"（表单项）
```

**优先使用 BasicPopup 或 BasicModal + BasicForm(FormSchema)，这是 JNPF 最高频的编辑模式。**

### 场景 D：全页表单

```
jnpf-content-wrapper.jnpf-content-wrapper-form
  └── jnpf-content-wrapper-form-body
        └── ScrollContainer
              └── a-form :labelCol="{ style: { width: '110px' } }"
                    ├── a-divider orientation="left" — 第一分区标题
                    ├── a-row > a-col 表单项
                    ├── a-divider orientation="left" — 第二分区标题
                    └── a-row > a-col 表单项
```

**表单分区用 a-divider，不用 a-card。**

### 场景 E：Dashboard / 监控看板（唯一允许 a-card 的场景）

```
jnpf-content-wrapper
  └── jnpf-content-wrapper-center
        └── jnpf-content-wrapper-content
              ├── a-row > a-col 指标卡（可以用 a-card）
              └── a-row > a-col 图表区（可以用 a-card）
```

**注意：普通业务页面不允许使用此模式。列表页不要加 KPI 卡片。**

---

## 四、布局优先级（强制）

1. **列表/CRUD**：jnpf-content-wrapper + BasicTable，搜索通过 BasicTable 内嵌配置
2. **弹窗编辑**：BasicPopup 或 BasicModal + BasicForm(FormSchema)，这是最常见模式
3. **全页表单**：jnpf-content-wrapper-form + ScrollContainer + a-divider 分区
4. **a-card**：仅用于 Dashboard/监控页，普通业务页禁用
5. **禁止自造 CSS 类名**，只用 common.less 中已有的类

---

## 五、组件映射表

### 封装组件

| 功能 | 使用 | 不要用 |
|------|------|--------|
| 表格 | BasicTable（`/@/components/Table`） | 原生 a-table |
| 表单 | BasicForm + FormSchema | — |
| 弹窗 A | BasicPopup | 原生 a-modal |
| 弹窗 B | BasicModal | 原生 a-modal |
| 表格操作列 | TableAction + #bodyCell slot | 手写 a-button 列 |
| 左侧树 | BasicLeftTree | — |
| 滚动容器 | ScrollContainer | 原生 overflow: auto |

### jnpf-* 全局组件

| 功能 | 组件名 | 说明 |
|------|--------|------|
| 下拉选择 | jnpf-select | props: :options, fieldNames；字典需先通过 baseStore.getDictionaryData 加载 |
| 日期选择 | jnpf-date-picker | — |
| 树选择 | jnpf-tree-select | — |
| 用户选择 | jnpf-user-select | — |
| 部门选择 | jnpf-dep-select | — |
| 文件上传 | jnpf-upload-file | — |
| 图片上传 | jnpf-upload-img | — |
| 上传按钮 | jnpf-upload-btn | — |
| 数字输入 | jnpf-input-number | — |
| 弹窗选择 | jnpf-popup-select | — |
| 组织选择 | jnpf-organize-select | — |

### 字典数据加载方式

jnpf-select 没有 dictCode 属性。字典数据的正确加载方式：

```typescript
import { useBaseStore } from '/@/store/modules/base'
const baseStore = useBaseStore()

const dictList = ref([])
const loadDict = async () => {
  dictList.value = await baseStore.getDictionaryData('yourDictCode')
}
```

模板中：

```html
<jnpf-select v-model:value="form.status" :options="dictList" />
```

### 确认组件是否存在的方法

```bash
# 检查全局注册
grep -r "registerGlobComp" jnpf-web-vue3/src/main.ts jnpf-web-vue3/src/components/registerGlobComp.ts

# 检查组件目录
find jnpf-web-vue3/src/components -name "*关键词*"
```

---

## 六、TableAction 操作列模式

### 单行操作

```typescript
function getTableActions(record) {
  return [
    {
      label: '编辑',
      onClick: handleEdit.bind(null, record),
    },
    {
      label: '删除',
      color: 'error',
      popConfirm: {
        title: '确认删除该记录？',
        confirm: handleDelete.bind(null, record),
      },
    },
  ]
}
```

### 批量操作

```typescript
import { Modal } from 'ant-design-vue'

function handleBatchDelete() {
  const rows = getSelectRows()
  if (!rows.length) return createMessage.warning('请先选择记录')
  Modal.confirm({
    title: '确认删除',
    content: `确认删除选中的 ${rows.length} 条记录？`,
    onOk: async () => {
      await deleteApi(rows.map(r => r.id))
      createMessage.success('删除成功')
      reload()
    },
  })
}
```

---

## 七、样式规范

### Less 变量（主题色通过 modifyVars 注入）

```less
@primary-color       // 主色
@success-color       // 成功色
@warning-color       // 警告色
@error-color         // 错误色
@heading-color       // 标题色
@text-color          // 正文色
@text-color-secondary // 次要文字
@border-color-base   // 边框色
@component-background // 组件背景（白底）
@body-background     // 页面背景（灰底）
```

### WindiCSS 工具类（布局优先使用）

```
布局：flex, flex-col, flex-1, items-center, justify-between, justify-end
间距：gap-4(16px), gap-2(8px), p-4(16px), px-10px, mb-4(16px), mt-4
文字：text-sm(13px), text-base(14px), text-lg(16px), font-600, font-bold
颜色：text-gray-400, text-gray-600, bg-white, bg-gray-50
其他：w-full, h-full, overflow-hidden, overflow-auto, rounded
```

### 样式编写优先级

1. WindiCSS 工具类（布局、间距、文字）
2. `<style lang="less" scoped>` 中的 Less 样式（业务特殊样式）
3. 禁止 inline style、!important、硬编码十六进制色值

### 防塌陷规则

- 页面容器：height: 100%，flex 布局
- 内容区：flex: 1; overflow-y: auto
- 禁止双滚动条
- 禁止 min-height: 100vh（会撑爆 MDI 页签）

---

## 八、视觉自检清单

代码写完后，如果 dev server 在运行：

1. 用 Playwright MCP 导航到页面并截图
2. 自检以下问题：
   - 页面有没有白屏、大面积错位？
   - 有没有出现双滚动条？
   - 按钮颜色层级是否主次分明？
   - 表格文本是否溢出？是否设置了 ellipsis？
   - 表单项是否对齐？
   - 页面是否像 JNPF 原生页面一样紧凑、白底、分区清晰？
3. 发现问题立刻修改，重新截图

---

## 九、禁止清单

| 禁止 | 原因 |
|------|------|
| 用 a-card 做表单/列表分区 | JNPF 用 a-divider + 白底 div，不用 card |
| 自造 CSS 类名 | 必须用 common.less 已有的类 |
| 用原生 a-table | 必须用 BasicTable |
| jnpf-select 的 dictCode 属性 | 不存在，字典需 baseStore 加载后传 :options |
| jnpf-dictionary-select | 组件不存在 |
| 路径 /src/components/ | 必须用 /@/components/ |
| inline style | 用 class 或 scoped less |
| 在列表页加 KPI 卡片 | 列表页就是纯 BasicTable |
| 复制本文件骨架片段作为完整页面 | 必须 Read 黄金页面索引中的真实文件 |
