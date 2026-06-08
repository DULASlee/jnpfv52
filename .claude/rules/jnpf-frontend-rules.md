# JNPF Vue3 Frontend Custom Page Development Rules

This file governs handcrafted custom page development that the code generation engine cannot cover. Auto-generated pages are managed by backend .vm templates.

---

## Iron Law: Read Before Write

Every custom page development MUST follow this sequence:
1. Read `docs/frontend/jnpf-taste-blueprint.md` (golden page index, skeleton decision tree, component mapping, layout rules)
2. Find a similar mature page under `jnpf-web-vue3/src/views/` and Read it as reference
3. Select the correct skeleton pattern per the blueprint decision tree
4. Then write code
NEVER skip the above steps and start coding directly.

---

## Component Selection Decision Table (NEVER Deviate)

| Need | Correct Approach | NEVER |
|---|---|---|
| Standard CRUD list page | jnpf-content-wrapper + BasicTable | Wrap list in a-card |
| Form section divider | a-divider or BasicForm GroupTitle | Use a-card for sections |
| Popup form | BasicPopup/BasicModal + BasicForm(FormSchema) | Build custom popup |
| KPI cards / mixed statistics layout | a-card allowed, MUST follow views/dashboard/ pattern | Use a-card in regular pages |
| Table | BasicTable | Raw a-table |
| Form | BasicForm (schema-driven) or hand-coded a-form | BasicForm without schema |
| Popup modal | BasicPopup or BasicModal | Hand-coded Modal logic |
| Styles | common.less existing classes + WindiCSS | Invent class names (.search-wrapper etc.) |
| Dictionary | baseStore.getDictionaryData → jnpf-select :options | Hardcoded options |
| Custom component | Prefer jnpf-* global components | Build existing functionality |
| Routing | Backend menu dynamic injection | Frontend static routes |
| Path alias | /@/ → src | /src/ |
| Action column | TableAction + #bodyCell slot | Hand-coded a-button loop |

---

## Frontend Code Pattern Quick Reference

### BasicTable Standard Pattern

    const [registerTable] = useTable({
      api: getListApi,
      columns: [
        { title: 'Name', dataIndex: 'fullName', width: 200 },
        { title: 'Status', dataIndex: 'status',
          customRender: ({ text }) => text === 1 ? 'Active' : 'Inactive' },
      ],
      actionColumn: [
        { label: 'Edit', onClick: handleEdit },
        { label: 'Delete', popConfirm: { title: 'Confirm delete?', onConfirm: handleDelete } },
      ],
    });

### BasicForm Schema Standard Pattern

    const schemas: FormSchema[] = [
      { field: 'userName', label: 'Username', component: 'Input', required: true },
      { field: 'status', label: 'Status', component: 'Select',
        componentProps: { options: statusOptions } },
    ];

### Detail Echo

Use `formState` or `setFieldsValue` to populate forms. NEVER manually bind each field with v-model.

---

## Tech Stack Lock

- UI: Ant Design Vue (a- prefix)
- Styles: Less + WindiCSS, no SCSS
- Vue 3 `<script setup>` + Composition API
- `<style lang="less" scoped>`
- Max 300 lines per file
- No inline style / !important / console.log

---

## Visual Self-Check

1. Use Playwright MCP to take screenshot while local dev server is running
2. Must look like native JNPF page. No white screen, no double scrollbar, no overflow, clear button hierarchy
3. Fix immediately if issues found, re-screenshot

---

## UI Design Skills Usage

已安装 5 个前端设计技能，用于在 JNPF 框架内提升视觉品味。

### 何时使用

| 页面类型 | 设计增强 | 理由 |
|---|---|---|
| .vm 生成页面 | 禁止 | 生成代码不可改 |
| 标准 CRUD 列表 | 微调（间距、hover、配色） | 骨架固定，皮肤可调 |
| Dashboard / 工作台 | 积极使用 | 自定义页面，设计空间大 |
| 特殊页面（落地页、报告） | 完整美学 | 用户可见，值得投入 |

### 如何使用

1. 先读 `jnpf-ui-enhance/SKILL.md` 确认约束
2. 参考设计技能获取美学方向（`/frontend-design`、`/ui-ux-pro-max`）
3. 仅应用**皮肤层**：颜色、间距、字体层级、阴影、动效
4. 不动**骨架层**：组件选择、API 调用、数据流

### 设计增强示例

```less
// ✅ 增强表格视觉（scoped less）
:deep(.ant-table-thead > tr > th) {
  background: #fafbfc;
  font-weight: 600;
}

// ✅ 卡片层次感
.stat-card {
  box-shadow: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
  transition: box-shadow 0.2s ease;
  &:hover { box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
}

// ❌ 禁止替换组件
// 不要用 a-table 替代 BasicTable
// 不要引入新的 UI 框架
```
