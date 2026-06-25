# Pulse 2: 列表查询 → 表单提交 → 数据刷新（web-vue3）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，标注每个环节的数据格式和转换方式
> 参考页面: permission/user (用户管理) — 典型 CRUD 页面，含树形筛选+搜索表单+表格+弹窗表单

---

## 一、完整调用链路图

```
[Tree]              [SearchForm]          [BasicTable]           [useDataSource]         [API]               [Backend]
  │                      │                     │                       │                    │                    │
  │                      │                     │  1. onMounted()      │                    │                    │
  │                      │                     │     immediate?       │                    │                    │
  │                      │                     │     → fetch() ──────→│                    │                    │
  │                      │                     │                     │ 2. merge params    │                    │
  │                      │                     │                     │    { currentPage,  │                    │
  │                      │                     │                     │      pageSize,     │                    │
  │                      │                     │                     │      ...formValues, │                    │
  │                      │                     │                     │      ...searchInfo, │                    │
  │                      │                     │                     │      ...sortInfo }  │                    │
  │                      │                     │                     │ 3. beforeFetch?    │                    │
  │                      │                     │                     │ 4. api(params) ────→ GET /api/permission/Users ─→│
  │                      │                     │                     │ ←── { code, data:  │                    │
  │                      │                     │                     │       { list:[],   │                    │
  │                      │                     │                     │         pagination │                    │
  │                      │                     │                     │         .total } }  │                    │
  │                      │                     │                     │ 5. dataSourceRef   │                    │
  │                      │                     │                     │    = list          │                    │
  │                      │                     │                     │ 6. pagination      │                    │
  │                      │                     │                     │    .total = total   │                    │
  │                      │                     │ ←── render ──────── │                    │                    │
  │                      │                     │                     │                    │                    │
  │                      │  7. 用户搜索/重置    │                     │                    │                    │
  │                      │     @submit ────────→│ 8. handleSearch     │                    │                    │
  │                      │                     │    InfoChange()     │                    │                    │
  │                      │                     │    → fetch({page:1})──→ (back to step 2)  │                    │
  │                      │                     │                     │                    │                    │
  │  9. 树节点选择       │                     │                     │                    │                    │
  │     @select ────────→│ searchInfo          │                     │                    │                    │
  │                      │ .organizeId = id    │                     │                    │                    │
  │                      │ getForm().          │                     │                    │                    │
  │                      │ resetFields()       │                     │                    │                    │
  │                      │                     │                     │                    │                    │
  │                      │                     │ 10. 分页/排序变化    │                    │                    │
  │                      │                     │     @change ───────→│ handleTableChange  │                    │
  │                      │                     │                     │ → setPagination()  │                    │
  │                      │                     │                     │ → fetch() ────────→ (back to step 2)  │
  │                      │                     │                     │                    │                    │

[Parent]                [usePopup]            [dataTransferRef]     [usePopupInner]       [Form.vue]          [API]
  │                         │                      │                    │                    │                    │
  │ 11. addOrUpdate(id)    │                      │                    │                    │                    │
  │     openFormPopup ────→│ 12. setPopupProps    │                    │                    │                    │
  │                         │     + dataTransfer   │                    │                    │                    │
  │                         │     Ref[uid] = data ─→│                    │                    │                    │
  │                         │                      │ 13. watchEffect   │                    │                    │
  │                         │                      │     fires ───────→│ 14. callbackFn    │                    │
  │                         │                      │                    │     = init(data) ─→│                    │
  │                         │                      │                    │                    │ 15. id存在?        │
  │                         │                      │                    │                    │     getUserInfo()──→│
  │                         │                      │                    │                    │ ←── user data      │
  │                         │                      │                    │                    │ 16. setFieldsValue │
  │                         │                      │                    │                    │     (data)         │
  │                         │                      │                    │                    │                    │
  │                         │                      │                    │                    │ 17. 用户编辑+提交   │
  │                         │                      │                    │                    │     handleSubmit()  │
  │                         │                      │                    │                    │ 18. validate()     │
  │                         │                      │                    │                    │ 19. create/update──→│
  │                         │                      │                    │                    │ ←── { code, msg }  │
  │                         │                      │                    │                    │ 20. closePopup()   │
  │                         │                      │                    │                    │ 21. emit('reload')─→ (parent reloads table)│
```

---

## 二、各环节详细分析

### 2.1 页面初始化与首次加载（index.vue:265-268）

```typescript
onMounted(() => {
  initData(true);   // ← isInit=true: 先加载树，树加载完成后触发首次列表查询
  initOptions();    // ← 加载字典数据（性别下拉选项）
});
```

**initData 流程 (index.vue:203-211):**
```
initData(isInit=true)
  → treeLoading = true
  → if isInit: setLoading(true)          // 显示表格 loading
  → getDepartmentSelectorByAuth()        // GET 组织树 API
    → treeData = res.data.list
    → treeLoading = false
    → if isInit: reload({ page: 1 })    // ← 首次列表查询的触发点
```

**关键发现:** 首次数据加载强依赖组织树 API。如果 `getDepartmentSelectorByAuth()` 失败或超时，`reload()` 永远不会被调用，用户看到永久 loading。

### 2.2 列表查询 — fetch() 核心逻辑（useDataSource.ts:230-320）

```typescript
async function fetch(opt?: FetchParams) {
  const { api, searchInfo, defSort, fetchSetting, beforeFetch, afterFetch, useSearchForm, pagination } = unref(propsRef);
  
  // 1. 构建分页参数
  let pageParams = {};
  pageParams[pageField]  = (opt && opt.page) || current;   // currentPage = 1
  pageParams[sizeField]  = pageSize;                        // pageSize = 20
  
  // 2. 合并所有参数源 (优先级从低到高)
  let params = merge(
    pageParams,              // { currentPage: 1, pageSize: 20 }
    getFieldsValue(),        // 搜索表单当前值 { keyword, gender, enabledMark }
    searchInfo,              // 外部注入的搜索条件 { organizeId }
    opt?.searchInfo ?? {},   // fetch() 调用时传入的额外条件
    defSort,                 // 默认排序
    sortInfo,                // 当前排序状态
    filterInfo,              // 当前筛选状态
  );
  
  // 3. beforeFetch 钩子（可修改参数）
  if (beforeFetch) params = (await beforeFetch(params)) || params;
  
  // 4. 调用 API
  fetchParams.value = params;
  const res = await api(params);   // → defHttp.get({ url, params })
  const data = res.data;
  
  // 5. 解构响应
  let resultItems = get(data, listField);     // data.list
  let resultTotal = get(data, totalField);    // data.pagination.total
  
  // 6. 页码越界修正（潜在递归）
  if (current > Math.ceil(resultTotal / pageSize)) {
    setPagination({ current: currentTotalPage });
    return await fetch(opt);  // ← 递归调用!
  }
  
  // 7. afterFetch 钩子（可修改数据）
  if (afterFetch) resultItems = (await afterFetch(resultItems)) || resultItems;
  
  // 8. 更新状态
  dataSourceRef.value = resultItems;
  setPagination({ total: resultTotal });
  emit('fetch-success', { items, total });
}
```

**参数合并优先级 (merge 为 lodash merge，后者覆盖前者):**
```
pageParams < formValues < searchInfo < opt.searchInfo < defSort < sortInfo < filterInfo < opt.sortInfo
```

**配置映射 (componentSetting.ts → FETCH_SETTING → 后端接口):**

| 前端配置项 | 值 | 传给后端的字段 |
|---|---|---|
| pageField | `'currentPage'` | `params.currentPage = 1` |
| sizeField | `'pageSize'` | `params.pageSize = 20` |
| listField | `'list'` | 从 `res.data.list` 取列表 |
| totalField | `'pagination.total'` | 从 `res.data.pagination.total` 取总数 |

### 2.3 搜索表单交互（useTableForm.ts）

```
BasicTable 内部嵌入 BasicForm 作为搜索表单
  │
  ├── formConfig.schemas → BasicForm.schemas (搜索字段定义)
  ├── @submit → handleSearchInfoChange
  │     └── fetch({ searchInfo: formValues, page: 1 })  // 搜索始终回到第1页
  ├── @reset → handleSearchInfoChange  
  │     └── fetch({ searchInfo: {}, page: 1 })          // 重置清除搜索条件
  └── @field-value-change → debounce(redoHeight, 300ms) // 字段变化时重新计算表格高度
```

**搜索表单与表格的参数耦合点 (index.vue:216-221):**
```typescript
function handleTreeSelect(id, _node, nodePath) {
  if (!id || searchInfo.organizeId === id) return;
  searchInfo.organizeId = id;       // ← 修改 searchInfo (reactive)
  organizeIdTree.value = nodePath.map(o => o.id);
  getForm().resetFields();          // ← 清空搜索表单，但 searchInfo.organizeId 保留
}
```
**问题:** `searchInfo.organizeId` 作为隐藏参数混入每次 fetch，但用户看不到这个筛选条件。如果用户从搜索框输入关键词后再切换树节点，搜索表单被清空但 organizeId 已更新，行为不一致。

### 2.4 表格列配置与渲染

**列定义 (index.vue:82-91):**
```typescript
const columns: BasicColumn[] = [
  { title: '账号', dataIndex: 'account', width: 100 },
  { title: '性别', dataIndex: 'gender', width: 90, align: 'center' },
  { title: '创建时间', dataIndex: 'creatorTime', width: 150, format: 'date|YYYY-MM-DD HH:mm:ss' },
  // ...
];
```

**自定渲染通过 #bodyCell 插槽:**
```html
<template #bodyCell="{ column, record }">
  <template v-if="column.key === 'enabledMark'">
    <a-tag :color="...">{{ statusText }}</a-tag>
  </template>
  <template v-if="column.key === 'action' && !record.isAdministrator">
    <TableAction :actions="getTableActions(record)" />
  </template>
</template>
```

**操作列使用 TableAction 组件** — 传入 `ActionItem[]`，支持：
- `onClick`: 点击回调
- `modelConfirm`: 弹出确认框（popConfirm）
- `ifShow`: 条件显示
- `disabled`: 禁用状态

### 2.5 弹窗表单 — Popup + Form 数据流

**这是 JNPF 最复杂的组件交互模式。** 完整追踪如下：

**Step 1: 父组件注册 Popup (index.vue:77)**
```typescript
const [registerForm, { openPopup: openFormPopup }] = usePopup();
// 模板中: <Form @register="registerForm" @reload="reload" />
```

**Step 2: 打开弹窗 (index.vue:222-223)**
```typescript
function addOrUpdateHandle(id = '') {
  openFormPopup(true, { id, organizeIdTree: organizeIdTree.value || [] });
}
```

**Step 3: usePopup.openPopup() 写入共享状态 (usePopup.ts:61-76)**
```typescript
openPopup: (visible = true, data, openOnSet = true) => {
  getInstance()?.setPopupProps({ visible, confirmLoading: false });
  if (!data) return;
  // data → dataTransferRef[uid] （模块级 reactive 对象！）
  dataTransferRef[unref(uid)] = toRaw(data);
}
```

**Step 4: usePopupInner 监听数据变化 (usePopup.ts:115-122)**
```typescript
watchEffect(() => {
  const data = dataTransferRef[unref(uidRef)];
  if (!data) return;
  if (!callbackFn) return;
  nextTick(() => { callbackFn(data); });  // → init(data)
});
```

**Step 5: Form.vue init() 处理 (Form.vue:234-260)**
```typescript
function init(data) {
  changeLoading(true);
  resetFields();
  id.value = data.id;
  
  if (id.value) {
    // 编辑模式：获取详情 → 填充表单
    getUserInfo(id.value).then(res => {
      // 数据转换：逗号分隔字符串 → 数组
      const data = {
        ...res.data,
        roleId: res.data.roleId ? res.data.roleId.split(',') : [],
        positionId: res.data.positionId ? res.data.positionId.split(',') : [],
      };
      setFieldsValue(data);
      changeLoading(false);
    });
  } else {
    // 新增模式：设置默认值
    organizeIdTree.value = data.organizeIdTree?.length ? [data.organizeIdTree] : [];
    setFieldsValue({ organizeIdTree: organizeIdTree.value });
    changeLoading(false);
  }
}
```

**Step 6: 表单提交 (Form.vue:294-317)**
```typescript
async function handleSubmit() {
  const values = await validate();  // Ant Design Vue 表单校验
  if (!values) return;              // 校验失败 → 停止
  
  changeOkLoading(true);
  // 数据转换：organizeIdTree → organizeId (逗号分隔)
  const organizeIds = values.organizeIdTree.map(o => o[o.length - 1]);
  const query = {
    ...values,
    id: id.value,
    organizeId: organizeIds.join(),                                  // 数组→字符串
    positionId: values.positionId?.length ? values.positionId.join() : '',
    roleId: values.roleId?.length ? values.roleId.join() : '',
  };
  
  const formMethod = id.value ? updateUser : createUser;
  formMethod(query).then(res => {
    createMessage.success(res.msg);
    closePopup();
    emit('reload');  // → 父组件的 reload() → fetch()
  }).catch(() => { changeOkLoading(false); });
}
```

**数据格式转换汇总 (编辑 vs 新增):**

| 字段 | 后端存储格式 | 编辑回显转换 | 提交转换 |
|---|---|---|---|
| roleId | `"id1,id2,id3"` | `.split(',')` → 数组 | `.join()` → 字符串 |
| positionId | `"id1,id2"` | `.split(',')` → 数组 | `.join()` → 字符串 |
| organizeIdTree | 后端返回 `[[id], [parent, id]]` | 原样使用 | `.map(o=>o[o.length-1]).join()` |

### 2.6 表单校验机制（useFormEvents.ts:254-269）

```
validate(nameList?)
  │
  └── formElRef.validate(nameList)  → Ant Design Vue Form.validate()
        │
        ├── 成功 → 返回 { ...fullValueRef.value, ...values }
        │
        └── 失败
              ├── error.errorFields.length > 0 → return false  (校验错误)
              └── error.errorFields.length = 0 → return { ...fullValueRef, ...error.values }
                   ↑ 这个分支处理的是没有 errorFields 的"失败"，
                     例如异步校验返回的错误，静默当作成功处理!
```

**校验规则来源 (Form.vue:38-41):**
```typescript
rules: [
  { required: true, trigger: 'blur', message: '必填' },
  { validator: formValidate('fullName', '不能含有特殊符号'), trigger: 'blur' },
]
```

### 2.7 删除操作数据流 (index.vue:225-229)

```
handleDelete(id)
  → delUser(id)                        // DELETE /api/permission/Users/{id}
    → defHttp.delete({ url })          // VAxios → axios.delete
  ← { code: 200, msg: "删除成功" }
  → createMessage.success(res.msg)     // 成功提示
  → reload()                           // 刷新列表
```

**前置确认通过 `modelConfirm` 属性实现 (index.vue:163-165):**
```typescript
{
  label: '删除',
  color: 'error',
  modelConfirm: {
    onOk: handleDelete.bind(null, record.id),
  },
}
```
TableAction 组件内部渲染为 `a-popconfirm`，点击确认后执行 onOk。

---

## 三、核心架构剖析

### 3.1 组件层级

```
jnpf-content-wrapper (布局容器)
  ├── jnpf-content-wrapper-left
  │     └── BasicLeftTree (组织树)
  ├── jnpf-content-wrapper-center
  │     └── jnpf-content-wrapper-content
  │           └── BasicTable
  │                 ├── BasicForm (搜索表单, 内部创建)
  │                 └── a-table (Ant Design Vue)
  └── Form (弹窗表单, 通过 Popup 包装)
        └── BasicPopup
              └── BasicForm
```

### 3.2 组件间通信方式

| 通信方向 | 方式 | 实现 |
|---|---|---|
| 父→Table | `@register` + `useTable()` | BasicTable emit('register', tableAction) → useTable 闭包捕获 |
| 父→Popup | `@register` + `usePopup()` | BasicPopup emit('register', popupInstance, uid) → usePopup 闭包捕获 |
| 父→Popup(数据) | `openPopup(true, data)` | 数据写入模块级 `dataTransferRef[uid]` |
| Popup→子(Form) | `usePopupInner(callback)` | watchEffect 监听 dataTransferRef 变化 → callback(data) |
| 子(Form)→父 | `emit('reload')` | Form emit → 父组件 `@reload="reload"` |
| Table→父 | `@register` methods | tableAction 暴露 reload/getForm/getDataSource 等方法 |

### 3.3 dataTransferRef — 全局共享状态的隐患

```typescript
// usePopup.ts:9
const dataTransferRef = reactive<any>({});
```

这是模块级单例，所有 Popup 实例共享同一个 reactive 对象。数据通过组件 `uid` 隔离。潜在问题：

1. **内存泄漏**: 弹窗关闭后 `dataTransferRef[uid]` 未清理。每次打开新弹窗会覆盖旧值，但如果组件 uid 变化（如热更新），旧 key 的数据会残留。
2. **uid 冲突**: uid 由 Vue 的 `getCurrentInstance().uid` 生成，理论唯一但跨组件树重用时可能碰撞。
3. **无类型安全**: `reactive<any>` — 完全放弃类型检查。

---

## 四、数据格式与转换矩阵

### 4.1 API 请求参数 → fetch → 后端

```
用户操作 (搜索/分页/排序)
  │
  ▼
fetch() 合并参数源
  │
  ├── pageParams:    { currentPage: 1, pageSize: 20 }
  ├── formValues:    { keyword: "张三", gender: "1", enabledMark: undefined }
  ├── searchInfo:    { organizeId: "abc123" }
  ├── sortInfo:      { sidx: "creatorTime", sort: "desc" }
  └── filterInfo:    {}
  │
  ▼ merge()
请求参数: { currentPage, pageSize, keyword, gender, organizeId, sidx, sort }
  │
  ▼ defHttp.get({ url, params })
GET /api/permission/Users?currentPage=1&pageSize=20&keyword=张三&gender=1&organizeId=abc123&sidx=creatorTime&sort=desc
```

### 4.2 后端响应 → 数据解构 → 表格渲染

```
HTTP 200
{ code: 200, msg: "成功", data: { list: [...], pagination: { total: 50 } } }
  │
  ▼ transformResponseHook (axios/index.ts:34)
  │ code === 200 → 返回 res.data
  │
  ▼ fetch() 解构
  │ resultItems = get(data, 'list')         → [{ account, realName, ... }, ...]
  │ resultTotal = get(data, 'pagination.total') → 50
  │
  ▼ dataSourceRef.value = resultItems
  │
  ▼ getDataSourceRef (computed)
  │ autoCreateKey → 为每行添加唯一 id
  │
  ▼ a-table :dataSource="dataSource"
  │ columns = getViewColumns (computed, 经 useColumns 处理)
  │   ├── 基础列: { title, dataIndex, width }
  │   ├── format 列: 日期格式化
  │   └── action 列: 操作按钮
  │
  ▼ 渲染: <a-tag> / <TableAction> / 自定义插槽
```

---

## 五、发现汇总

### P0 安全/可靠性

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| CR-1 | dataTransferRef 模块级单例无清理 | usePopup.ts:9 | 内存泄漏，多弹窗数据污染 |
| CR-2 | fetch() 页码越界递归无深度限制 | useDataSource.ts:286 | 极端情况下可能无限递归 |

### P1 架构问题

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| A-1 | 首次加载依赖组织树 API | index.vue:206-210 | 树 API 失败 → 列表永远不加载 |
| A-2 | 搜索表单被树节点切换清空 (resetFields) | index.vue:220 | 用户体验不一致 |
| A-3 | validate() 吞没无 errorFields 的校验失败 | useFormEvents.ts:263-264 | 某些异步校验错误静默放过 |
| A-4 | fetch 参数优先级隐式（9 层 merge） | useDataSource.ts:253-263 | 排序难以排查 |
| A-5 | formModel 使用 reactive 驱动整个表单 | BasicForm.vue:62 | 无局部状态，大表单性能差 |
| A-6 | errorMessageMode 默认 'message' 但个别 API 不设 | axios/index.ts:248 | 部分错误无用户提示 |
| A-7 | ignoreCancelToken: true 无法取消重复请求 | axios/index.ts:245 | 快速连点搜索→多个请求并发 |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| E-1 | `as any` / `as unknown as` 类型断言泛滥 | useDataSource, useTableForm |
| E-2 | 编辑/新增共用一个 Form 组件，逻辑分支复杂 | Form.vue:234-260 |
| E-3 | handleSubmit 中 try-catch 吞所有异常无提示 | Form.vue:314 |
| E-4 | organizeIdTree 格式隐式约定：`[[leafId], [parentId, leafId]]` | 全局 |
| E-5 | 表单字段 roleId/positionId 存储为逗号分隔字符串而非数组 | 后端设计 |

---

## 六、性能观察

| 环节 | 估算耗时 | 说明 |
|---|---|---|
| 首次加载 (树+列表) | 100-800ms | 两次串行 API 调用 |
| 搜索 | 50-300ms | 单次 API + debounce |
| 分页/排序 | 50-300ms | 单次 API |
| 编辑回显 | 50-300ms | getUserInfo API + setFieldsValue |
| 新增/编辑提交 | 50-500ms | validate + createUser/updateUser API |
| 删除 | 50-500ms | delUser API → reload |

**关键瓶颈:** 所有操作都依赖网络 I/O。前端无请求缓存，翻回之前的页码会重新请求。
- 搜索 debounce 仅用于重算高度（300ms），不用于防抖搜索请求本身
- 表单字段变更触发 deep watch（debounce 300ms），大表单可能卡顿

---

## 七、改进建议 (未纳入本阶段范围)

1. **dataTransferRef 增加清理机制** — closePopup 时删除 `dataTransferRef[uid]`
2. **拆分搜索表单与编辑表单** — 独立的 Form 组件，减少条件分支
3. **添加请求去重** — 连续点击搜索取最后一次请求结果（或启用 cancelToken）
4. **列表增加请求缓存** — page+params 作为 key，减少重复请求
5. **validate 错误统一处理** — 无 errorFields 的失败也应明确提示用户
6. **树加载失败降级** — 树 API 失败时仍允许列表加载（不带组织筛选条件）
