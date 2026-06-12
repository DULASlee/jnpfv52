# JNPF 组件注册表覆盖率缺口报告

> 首版手工编制，为阶段五启动门禁（registry 覆盖率 ≥90%）提供数据基线
> 日期：2026-06-12

---

## 1. 覆盖率概览

| 维度 | 已注册 (F-3) | 平台总数 | 覆盖率 | 缺口 |
|------|-------------|---------|--------|------|
| PC 端 (Form/componentMap.ts) | 35 | 67 | 52% | 32 |
| F-1 IR component-mapping | 33 | 67 | 49% | 34 |

**结论：当前覆盖率 52%，距离 ≥90% 目标差 28 个组件。**

---

## 2. 未覆盖组件清单（32 个）

### 2.1 输入类 (5)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfInputGroup | 输入组 | P1 | 复合组件，需检查子组件 |
| JnpfInputSearch | 搜索输入框 | P2 | 本质是 Input + search 属性 |
| JnpfInputPassword | 密码输入框 | P1 | 常用 |
| JnpfAutoComplete | 自动完成 | P1 | 需数据源绑定 |
| JnpfIconPicker | 图标选择器 | P2 | 设计时组件 |

### 2.2 日期时间类 (2)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfMonthPicker | 月份选择 | P2 | DatePicker 的变体 |
| JnpfWeekPicker | 周选择 | P2 | DatePicker 的变体 |

### 2.3 弹出选择类 (4)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfPopupSelect | 弹出选择 | P0 | 常用高级组件 |
| JnpfPopupTableSelect | 弹出表格选择 | P0 | 关联表选择 |
| JnpfPopupAttr | 弹出属性 | P1 | 需要 |
| JnpfRelationForm | 关联表单 | P0 | 子表关系的可视化 |
| JnpfRelationFormAttr | 关联表单属性 | P1 | |

### 2.4 组织/用户类 (3)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfOrganizeSelect | 组织选择 | P1 | 已注册 DepSelect/PosSelect |
| JnpfGroupSelect | 分组选择 | P2 | |
| JnpfRoleSelect | 角色选择 | P2 | |
| JnpfUsersSelect | 多用户选择 | P1 | 已注册 UserSelect（单选） |

### 2.5 时间范围类 (2)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfDateRange | 日期范围 | P0 | 搜索常用 |
| JnpfTimeRange | 时间范围 | P2 | |

### 2.6 上传/媒体类 (4)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfUploadImgSingle | 单图片上传 | P2 | 已注册 UploadImg（多图） |
| JnpfQrcode | 二维码 | P2 | |
| JnpfBarcode | 条形码 | P2 | |
| JnpfSign | 签名 | ✅ 已注册 | |

### 2.7 特殊组件 (5)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfCalculate | 计算字段 | P1 | 子表内计算 |
| JnpfInputTable | 可编辑子表 | P0 | 与 JnpfTable 不同 |
| JnpfNumberRange | 数字范围 | P2 | 搜索用 |
| JnpfLocation | 地理位置 | P3 | |
| JnpfIframe | 内嵌页面 | P3 | |

### 2.8 其他 (3)

| JnpfKey | 说明 | 优先级 | 备注 |
|---------|------|--------|------|
| JnpfOpenData | 开放数据展示 | P1 | 只读字段渲染 |
| JnpfButton | 按钮 | P2 | |
| JnpfLink | 链接 | P2 | |

---

## 3. 补充计划（按优先级）

### Phase 1：P0 批量补充（本周）- 9 个 → 覆盖率 66%

```
JnpfPopupSelect, JnpfPopupTableSelect, JnpfRelationForm,
JnpfInputTable, JnpfDateRange, JnpfInputPassword,
JnpfAutoComplete, JnpfInputGroup, JnpfOrganizeSelect
```

### Phase 2：P1 批量补充（下周）- 12 个 → 覆盖率 84%

```
JnpfPopupAttr, JnpfRelationFormAttr, JnpfCalculate,
JnpfOpenData, JnpfUsersSelect, JnpfMonthPicker, JnpfWeekPicker,
JnpfTimeRange, JnpfUploadImgSingle, JnpfButton, JnpfLink, JnpfIconPicker
```

### Phase 3：P2-P3 收尾（阶段五前）- 11 个 → 覆盖率 100%

```
JnpfInputSearch, JnpfGroupSelect, JnpfRoleSelect,
JnpfQrcode, JnpfBarcode, JnpfNumberRange,
JnpfLocation, JnpfIframe
```

---

## 4. 阶段五门禁状态

| 指标 | 当前值 | 目标值 | 达标? |
|------|--------|--------|-------|
| Registry 覆盖率 | 52% | ≥90% | ❌ 差 28 个 |
| P0 覆盖率 | 0% (P0 组件均未注册) | 100% | ❌ |
| IR 组件映射对齐 | 部分 (33/67) | 全部 | ❌ |

**预计达标时间：** Phase 1 + Phase 2 = 2 周内达到 84%，Phase 3 达到 100%

---

## 5. 相关文档

- `src/core/component-registry/builtin.ts` — 35 内置组件注册
- `src/core/component-registry/builtin-dashboard.ts` — 22 大屏组件注册
- `jnpf-web-vue3/src/components/Form/src/componentMap.ts` — 平台 67 组件全集
- `src/core/ir/component-mapping.ts` — IR 33 组件映射
