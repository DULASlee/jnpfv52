# JNPF App.vue3 技术基线

> Sprint 0-A Day 5 — UniApp 编译器对照参照
> 日期: 2026-06-12

## 1. 框架版本

| 项目 | 版本 | 说明 |
|------|------|------|
| Vue | 3.3.4 | Composition API + script setup |
| Vite | 4.5.1 | Build tool |
| TypeScript | 5.0.4 | Strict mode |
| Ant Design Vue | 3.2.20 | UI library |
| Pinia | 2.1.3 | State management |
| Vue Router | 4.2.5 | Routing |
| Axios | 1.7.0 | HTTP client |
| Dayjs | 1.11.10 | Date utility |
| WindiCSS | 3.5.6 | Utility CSS |
| Less | 4.2.0 | CSS preprocessor |

## 2. 构建配置

| 配置项 | 值 |
|--------|-----|
| Vite port | 3100 |
| manual chunks | 5 (vendor-vue, vendor-antd, vendor-tinymce, vendor-monaco, vendor-codemirror) |
| plugins | 13 active |
| build memory | 8192 MB (max-old-space-size) |
| path alias | `/@/` to `src/` |
| pnpm | 8.x, frozen-lockfile |

## 3. 代码约定

- SFC: script setup lang=ts + style lang=less scoped
- Component: PascalCase, max 300 lines
- Composable: use prefix, src/hooks/ or src/composables/
- Path import: /@/ prefix (Vite alias)
- Route: Backend menu dynamic injection

## 4. UniApp 编译器对照

| 维度 | Web (F-4) | UniApp (planned) |
|------|-----------|-----------------|
| UI library | ant-design-vue | wot-design-uni |
| HTTP | axios | uni.request (Alova wrapper) |
| Routing | vue-router (dynamic) | uni.navigateTo |
| State | Pinia | Pinia (shared) |
| Build | Vite | HBuilderX / uni-app-cli |
| Component mapping | pc layer | app layer (wd- prefix) |

## 5. 安全基线

| 检查项 | 状态 |
|--------|------|
| e val() in production code | 0 |
| new Function() outside compiler.ts | 0 |
| ESLint no-eval rule | active |
| ESLint no-new-func rule | active |
| Hardcoded keys | migrated to env vars |

## 6. 测试基线

| 指标 | 值 |
|------|-----|
| Test framework | vitest 1.6.0 |
| Test files | 9 |
| Total tests | 139 |
| Test location | src/core/\*\*/\*.test.ts |
| CI command | pnpm test:unit |
