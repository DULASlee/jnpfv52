---
name: jnpf-ui-enhance
description: JNPF 前端 UI 品味提升桥接技能。在框架组件体系内提升视觉质量，不破坏 jnpf-* 组件约定。Use when modifying custom pages, dashboard, or any handcrafted Vue3 frontend code in jnpf-web-vue3.
---

# JNPF UI Enhance — 框架内的品味提升

## 核心原则

**组件结构不可动，视觉层面可提升。**

JNPF 的 jnpf-* 组件体系（BasicTable, BasicForm, BasicPopup, jnpf-content-wrapper 等）是架构骨架，设计技能无权修改。
设计技能的作用域仅限于：颜色、间距、字体、动效、布局细节 — 即"皮肤层"。

## 触发条件

| 场景 | 是否使用设计技能 | 说明 |
|---|---|---|
| .vm 模板生成的页面 | **禁止** | 生成代码不属于自定义范畴 |
| 标准 CRUD 列表页 | **微调** | 仅调整间距、配色、hover 效果 |
| 自定义页面（dashboard、工作台、特殊表单） | **积极使用** | 完整应用美学方向 |
| 嵌入的第三方组件 | **禁止** | 不动第三方库内部 |

## 增强层次（由浅到深）

### Level 1: 微调（所有自定义页面可用）
- 表格行 hover 背景色（用 WindiCSS 或 scoped less）
- 按钮间距和圆角统一
- 文字层级：标题/正文/辅助文字的字号和颜色对比
- 空状态插图和文案

### Level 2: 中度增强（dashboard、工作台）
- 卡片阴影和边框的微妙层次
- 数据卡片的渐变背景或纹理
- 页面加载的淡入动画（CSS only）
- 图标和数字的排版节奏

### Level 3: 完整美学（特殊页面、营销页）
- 完整的美学方向（参考 frontend-design 技能）
- 自定义字体搭配（需确认 CDN 可用）
- 复杂动效（需评估性能影响）
- 非常规布局（需确认响应式）

## 操作规范

### DO — 可以做的

```vue
<!-- ✅ 在 scoped less 中增强表格视觉 -->
<style lang="less" scoped>
.jnpf-table-wrapper {
  :deep(.ant-table-thead > tr > th) {
    background: #fafbfc;
    font-weight: 600;
    border-bottom: 2px solid #e8e8e8;
  }
  :deep(.ant-table-tbody > tr:hover > td) {
    background: #f0f5ff;
  }
}
</style>

<!-- ✅ 用 WindiCSS 工具类微调间距 -->
<div class="p-4 rounded-lg shadow-sm bg-white mb-4">

<!-- ✅ 给数据卡片加微妙的渐变背景 -->
<style lang="less" scoped>
.stat-card {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  color: #fff;
}
</style>
```

### DON'T — 禁止做的

```vue
<!-- ❌ 不要替换 jnpf 组件为原生 Ant Design -->
<!-- 错误：用 a-table 替代 BasicTable -->

<!-- ❌ 不要引入新的 UI 框架 -->
<!-- 错误：npm install tailwindcss / element-plus -->

<!-- ❌ 不要修改组件库源码 -->
<!-- 错误：编辑 node_modules/jnpf-* 中的文件 -->

<!-- ❌ 不要使用 !important -->
<!-- 错误：background: red !important -->

<!-- ❌ 不要内联样式 -->
<!-- 错误：<div style="color: red"> -->
```

## 与设计技能的配合

当需要提升 UI 品味时，按此顺序调用：

1. **先读本技能**（确定增强层次和约束）
2. **读 jnpf-frontend-rules.md**（确认组件选择）
3. **读 jnpf-taste-blueprint.md**（确认页面骨架）
4. **参考 frontend-design 技能**获取美学方向（但只应用皮肤层）
5. **参考 taste-skill** 调整激进程度（JNPF 项目默认用 "controlled" 模式）

## 字体规范

JNPF 项目字体由 common.less 控制，不要引入外部字体 CDN。
如需增强字体层级，使用 Ant Design Vue 的 Typography 组件或 less 变量：

```less
// 在 scoped less 中调整字体层级
.page-title {
  font-size: 20px;
  font-weight: 600;
  color: #1f1f1f;
  letter-spacing: -0.02em;
}
.page-subtitle {
  font-size: 14px;
  color: #8c8c8c;
  margin-top: 4px;
}
```

## 配色规范

以 Ant Design Vue 默认色板为基础，可通过 CSS 变量微调：

```less
:root {
  --jnpf-primary: #1890ff;      // 保持框架主色
  --jnpf-bg-page: #f5f7fa;      // 页面背景可调
  --jnpf-bg-card: #ffffff;      // 卡片背景
  --jnpf-border: #e8e8e8;       // 边框色
  --jnpf-text-primary: #1f1f1f; // 主文字
  --jnpf-text-secondary: #8c8c8c; // 辅助文字
}
```

## 验证清单

每次 UI 增强后：
- [ ] 页面在 1440px 和 1920px 宽度下正常显示
- [ ] 无横向滚动条
- [ ] 按钮、链接可正常点击（z-index 无冲突）
- [ ] 深色/浅色主题切换无异常（如支持主题）
- [ ] 移动端 H5 不受影响（如有 uni-app 共用组件）
