# JNPF 组件注册表覆盖率缺口报告

> v2.0 — F-6a 后更新
> 日期：2026-06-13
> 为阶段五启动门禁（registry 覆盖率 ≥90%）提供数据基线

---

## 1. 覆盖率概览

| 维度 | 已注册 | 平台总数 | 覆盖率 | 缺口 |
|------|--------|---------|--------|------|
| IR 组件注册表 (F-3 builtin) | 33 | 58* | 57% | 25 |
| + F-6a 大屏扩展 | +18 | — | — | — |
| IR 注册表总计 (F-3 + F-6a) | **51** | 58 + 22 = 80 | **64%** | 29 |
| 表单组件覆盖 (Jnpf* only) | 22 | 58 | 38% | 36 |
| 大屏组件覆盖 | 18 | 22 | 82% | 4 |

> \* 平台总数 = `componentMap.ts` 中 58 个唯一 Jnpf* 表单组件
> （不含 7 个别名映射 + 2 个非 Jnpf 组件；原始 67 含这些）

**结论：预 F-6a 覆盖率 57%（33/58），F-6a 后提升至 64%（51/80）。距离 ≥90% 目标差 ~21 个表单组件 + 4 个大屏组件。**

---

## 2. 已注册组件清单

### 2.1 表单输入类 — IR builtin.ts (29 个 Jnpf*)

| # | JnpfKey | 分类 | 状态 |
|---|---------|------|------|
| 1 | JnpfInput | form-input | ✅ builtin |
| 2 | JnpfInputNumber | form-input | ✅ builtin |
| 3 | JnpfTextarea | form-input | ✅ builtin |
| 4 | JnpfSelect | form-select | ✅ builtin |
| 5 | JnpfRadio | form-select | ✅ builtin |
| 6 | JnpfCheckbox | form-select | ✅ builtin |
| 7 | JnpfCascader | form-select | ✅ builtin |
| 8 | JnpfTreeSelect | form-select | ✅ builtin |
| 9 | JnpfDatePicker | form-datetime | ✅ builtin |
| 10 | JnpfTimePicker | form-datetime | ✅ builtin |
| 11 | JnpfSwitch | form-switch | ✅ builtin |
| 12 | JnpfRate | form-switch | ✅ builtin |
| 13 | JnpfSlider | form-switch | ✅ builtin |
| 14 | JnpfColorPicker | form-switch | ✅ builtin |
| 15 | JnpfUploadImg | form-upload | ✅ builtin |
| 16 | JnpfUploadFile | form-upload | ✅ builtin |
| 17 | JnpfSign | form-special | ✅ builtin |
| 18 | JnpfSignature | form-special | ✅ builtin |
| 19 | JnpfEditor | form-special | ✅ builtin |
| 20 | JnpfAlert | layout | ✅ builtin |
| 21 | JnpfDivider | layout | ✅ builtin |
| 22 | JnpfTable | data-display | ✅ builtin |

### 2.2 布局/数据展示类 — builtin.ts (7 个)

| # | JnpfKey | 分类 | 状态 |
|---|---------|------|------|
| 23 | JnpfRow | layout | ✅ builtin |
| 24 | JnpfCol | layout | ✅ builtin |
| 25 | JnpfTabs | layout | ✅ builtin |
| 26 | JnpfTabPane | layout | ✅ builtin |
| 27 | JnpfList | data-display | ✅ builtin |
| 28 | JnpfCard | data-display | ✅ builtin |
| 29 | JnpfDescriptions | data-display | ✅ builtin |

### 2.3 图表类 — builtin.ts + builtin-dashboard.ts (7 个)

| # | Type | 分类 | 状态 |
|---|------|------|------|
| 30 | ECharts:Bar | chart | ✅ builtin |
| 31 | ECharts:Line | chart | ✅ builtin |
| 32 | ECharts:Pie | chart | ✅ builtin |
| 33 | ECharts:Map | chart | ✅ builtin |
| 34 | ECharts:Gauge | chart | ✅ F-6a |
| 35 | ECharts:Radar | chart | ✅ F-6a |
| 36 | ECharts:Scatter | chart | ✅ F-6a |

### 2.4 装饰 + 数据展示 — F-6a (7 个)

| # | Type | 分类 | 状态 |
|---|------|------|------|
| 37 | Border:Box1 | layout | ✅ F-6a |
| 38 | Border:Box2 | layout | ✅ F-6a |
| 39 | Decoration:1 | layout | ✅ F-6a |
| 40 | Text:Title | data-display | ✅ F-6a |
| 41 | Text:Scroll | data-display | ✅ F-6a |
| 42 | Data:ScrollBoard | data-display | ✅ F-6a |
| 43 | Data:Number | data-display | ✅ F-6a |

### 2.5 媒体 + 3D — F-6a (8 个)

| # | Type | 分类 | 状态 |
|---|------|------|------|
| 44 | Media:Image | data-display | ✅ F-6a |
| 45 | Media:Video | data-display | ✅ F-6a |
| 46 | Media:Iframe | data-display | ✅ F-6a |
| 47 | 3D:Scene | chart (v2.0.0) | ✅ F-6a |
| 48 | 3D:POI | chart (v2.0.0) | ✅ F-6a |
| 49 | 3D:Flyline | chart (v2.0.0) | ✅ F-6a |
| 50 | 3D:Fence | chart (v2.0.0) | ✅ F-6a |
| 51 | 3D:Heatmap | chart (v2.0.0) | ✅ F-6a |

---

## 3. 未覆盖表单组件清单（36 个 Jnpf*）

### 3.1 输入扩展类 (5)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfInputGroup | 输入组 | ❌ | P1 | 复合组件 |
| JnpfInputSearch | 搜索输入框 | ❌ | P2 | Input + search |
| JnpfInputPassword | 密码输入框 | ❌ | P1 | Input + type=password |
| JnpfAutoComplete | 自动完成 | ✅ | P1 | 需数据源绑定 |
| JnpfIconPicker | 图标选择器 | ✅ | P1 | 设计时组件 |

### 3.2 日期时间类 (4)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfMonthPicker | 月份选择 | ❌ | P2 | DatePicker 变体 |
| JnpfWeekPicker | 周选择 | ❌ | P2 | DatePicker 变体 |
| JnpfDateRange | 日期范围 | ✅ | P0 | 搜索常用 |
| JnpfTimeRange | 时间范围 | ✅ | P2 | 搜索用 |

### 3.3 弹出选择类 (5)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfPopupSelect | 弹出选择 | ✅ | P0 | 高级组件 |
| JnpfPopupTableSelect | 弹出表格选择 | ✅ | P0 | 关联表选择 |
| JnpfPopupAttr | 弹出属性 | ✅ | P1 | 属性选择 |
| JnpfRelationForm | 关联表单 | ❌ | P0 | 子表关系可视化 |
| JnpfRelationFormAttr | 关联表单属性 | ✅ | P1 | 已全局注册 |

### 3.4 组织/用户类 (4)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfOrganizeSelect | 组织选择 | ✅ | P1 | |
| JnpfDepSelect | 部门选择 | ✅ | P1 | 同 OrganizeSelect 子类型 |
| JnpfPosSelect | 岗位选择 | ✅ | P2 | |
| JnpfGroupSelect | 分组选择 | ✅ | P2 | |
| JnpfRoleSelect | 角色选择 | ✅ | P2 | |
| JnpfUserSelect | 用户选择 | ✅ | P1 | 单选 |
| JnpfUsersSelect | 多用户选择 | ✅ | P1 | 多选 |

### 3.5 上传/媒体类 (3)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfUploadImgSingle | 单图片上传 | ✅ | P2 | |
| JnpfQrcode | 二维码 | ✅ | P2 | |
| JnpfBarcode | 条形码 | ✅ | P2 | |

### 3.6 特殊组件 (5)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfCalculate | 计算字段 | ✅ | P1 | 子表内计算 |
| JnpfInputTable | 可编辑子表 | ✅ | P0 | 不同于 JnpfTable |
| JnpfNumberRange | 数字范围 | ✅ | P2 | |
| JnpfLocation | 地理位置 | ✅ | P3 | |
| JnpfIframe | 内嵌页面 | ✅ | P3 | |

### 3.7 其他 (5)

| JnpfKey | 说明 | 全局注册 | 优先级 | 备注 |
|---------|------|---------|--------|------|
| JnpfOpenData | 开放数据展示 | ✅ | P1 | 只读字段渲染 |
| JnpfButton | 按钮 | ✅ | P2 | |
| JnpfLink | 链接 | ✅ | P2 | |
| JnpfAreaSelect | 区域选择 | ✅ | P2 | |
| JnpfCron | Cron 表达式 | ✅ | P2 | |

### 3.8 特殊别名 + 其他组件 (5)

| 组件 | 说明 | 全局注册 | 优先级 | 备注 |
|------|------|---------|--------|------|
| JnpfCheckboxSingle | 单个复选框 | ✅ | P2 | |
| JnpfGroupTitle | 分组标题 | ✅ | P2 | alias: BasicCaption |
| JnpfText | 文本展示 | ✅ | P2 | 静态文本 |
| JnpfEmpty | 空状态 | ✅ | P3 | |
| JnpfUploadBtn | 上传按钮 | ✅ | P3 | 已全局注册 |

---

## 4. 未覆盖大屏组件清单（4 个）

F-6a 已注册 18 个 dashboard widget 类型，剩余缺口是平台侧未定义但未来可能需要的大屏组件：

| Type | 说明 | 优先级 | 备注 |
|------|------|--------|------|
| Decoration:2~10 | 装饰 2-10 号 | P3 | @jiaminghi/data-view 有多种装饰 |
| Border:Box3~13 | 边框 3-13 号 | P3 | 常用在阶段五补充 |
| ECharts:Tree | 树图 | P3 | |
| ECharts:Graph | 关系图 | P3 | 知识图谱可视化 |

---

## 5. 补充计划（按优先级）

### Phase 1：P0 批量补充（阶段五 Week 1）— 6 个 → 表单覆盖率 48%

```
JnpfDateRange, JnpfPopupSelect, JnpfPopupTableSelect,
JnpfRelationForm, JnpfInputTable, JnpfInputPassword
```

### Phase 2：P1 批量补充（阶段五 Week 2）— 14 个 → 表单覆盖率 72%

```
JnpfInputGroup, JnpfAutoComplete, JnpfIconPicker, JnpfOpenData,
JnpfOrganizeSelect, JnpfDepSelect, JnpfUserSelect, JnpfUsersSelect,
JnpfPopupAttr, JnpfRelationFormAttr, JnpfCalculate,
JnpfMonthPicker, JnpfWeekPicker, JnpfUploadImgSingle
```

### Phase 3：P2-P3 收尾（阶段五 Week 3）— 16 个 → 表单覆盖率 100%

```
JnpfInputSearch, JnpfTimeRange, JnpfPosSelect, JnpfGroupSelect,
JnpfRoleSelect, JnpfQrcode, JnpfBarcode, JnpfNumberRange,
JnpfButton, JnpfLink, JnpfAreaSelect, JnpfCron,
JnpfCheckboxSingle, JnpfGroupTitle, JnpfText,
JnpfLocation, JnpfIframe, JnpfEmpty, JnpfUploadBtn
```

### 大屏组件补充（阶段五 Week 3）— 4 个

```
Decoration:2, Border:Box3, ECharts:Tree, ECharts:Graph
```

---

## 6. 阶段五门禁状态

| 指标 | F-3 基线 | F-6a 后 | 目标值 | 达标? |
|------|---------|---------|--------|-------|
| IR 注册表总量 | 33 | **51** | — | — |
| 表单组件覆盖率 | 57% (33/58) | 57% | ≥90% | ❌ 差 19+ |
| 大屏组件覆盖率 | — | 82% (18/22) | 100% | ❌ 差 4 |
| 综合覆盖率 (表单+大屏) | — | **64%** (51/80) | ≥90% | ❌ 差 29 |
| P0 覆盖率 | 0/6 | 0/6 | 6/6 | ❌ |
| component-mapping 三层对齐 | 33 | 33 | 51 | ❌ 差 18 |

---

## 7. 关键发现

1. **表单组件缺口是主要瓶颈**：36 个 Jnpf* 表单组件未注册 IR Entry，但其中 31 个已有全局 Vue 组件实现（`registerGlobComp.ts`），只需补充 `ComponentEntry` 元数据
2. **5 个组件缺全局注册**：JnpfInputGroup / InputSearch / InputPassword / MonthPicker / WeekPicker / RelationForm 既无 IR Entry 也无全局组件注册，需阶段五新开发
3. **大屏组件覆盖率 82%**：已满足大部分场景，4 个缺口为装饰变体+特殊图表
4. **component-mapping.ts 三层映射落后**：33 个映射 vs 51 个注册，18 个 F-6a 大屏组件无三层映射（它们不需要，因为大屏不走跨端编译）

---

## 8. 相关文件

| 文件 | 说明 |
|------|------|
| `src/core/component-registry/builtin.ts` | 33 个内置组件 (form + chart) |
| `src/core/component-registry/builtin-dashboard.ts` | 22 个大屏组件 |
| `src/core/ir/component-mapping.ts` | 33 个三层映射 (pc/app/legacyApp) |
| `src/components/Form/src/componentMap.ts` | 58 个 Jnpf* 表单组件全集 |
| `src/components/registerGlobComp.ts` | 53 个全局 Vue 组件注册 |

---

## 9. Phase 4 更新 (2026-06-14)

**编译器侧类型映射已覆盖 12 种 jnpfKey → TS 类型：**
JnpfInput/Textarea/InputNumber/Switch/Select/DatePicker/TimePicker/Radio/Checkbox/Rate/Slider/UploadImg/UploadFile/Editor

**UniApp wd 组件映射覆盖率：** 9/12 = 75%（Rate/Slider/UploadImg/UploadFile/Editor 待实现 wd 对应组件）

**编译器注册表（uniapp/compiler.ts mapFieldToTSType）：** 14 种 jnpfKey 已映射
