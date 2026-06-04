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
