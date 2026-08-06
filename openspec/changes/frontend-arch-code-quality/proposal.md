# Change: frontend-arch-code-quality

## Why

前端 `jnpf-web-vue3` 九维度全量扫描（1566 文件 / 21 万行）显示：**87% 的组件体量正常、LCP 性能健康、API 层类型干净**，但存在四类**结构性**问题，且每类都有「治一处活一片」的高 ROI 切入点：

- **架构循环依赖**：**182 个环，89%（162 个）是 utils/router/store/api/hooks 基础设施互咬**。核心环 `store/user→router→views→axios→store/user` 串起整个 app——tree-shaking 失效、整环无法独立单测。
- **Bundle 肿瘤**：JS 14.8MB，**`vendor-common` 单块 7.8MB 占 55%**，未按路由分包；叠加 4 类功能重叠库（echarts+highcharts、codemirror+monaco）和 196 个多版本包。
- **死代码堆积**：**104 个未用文件**（49 个死组件）+ 14 个装了从没 import 的依赖——低代码平台长期迭代遗留。
- **a11y 系统性缺失**：293 处 `@click` 绑 div（键盘不可达）+ 75 处 img 无 alt + `aria-label` 全仓 0 处；运行时 axe 发现 3 critical + 5 serious。

## What

分四个阶段，严格按依赖顺序（死代码先清 → 核心环先断 → 再拆 god 组件）：

1. **清死代码 + 收敛依赖**（P2，让后续分析更准）：删除 104 个 Knip 确认的死文件 + 12 个整目录死组件；`pnpm remove` 14 个未用依赖（含死链 `@logicflow/*`）；`pnpm dedupe` 收敛多版本包。
2. **打破核心环**（P0，解锁 tree-shake/可测试性）：`utils/http/axios` 不再反向 import `store/modules/user`，抽 `IUserContext` 接口注入——一举切断 109 个环。路由懒加载切断 `router→views` 静态 import。
3. **拆 Bundle + god 组件**（P1）：拆 `vendor-common`（7.8MB）按路由懒加载；拆 `ColumnDesign/Main.vue`(CC 96) 和 `AiChatPanel.vue`(2682行)——**拆前必须先补组件测**。
4. **a11y + CSS + 类型收敛**（P3）：修全页面 a11y（lang/viewport/role 三处）；治本 293 处 @click 绑 div；`pnpm lint:stylelint --fix` 自动修 1957 空行；清 261 处 computed 副作用。

## Scope

| 纳入 | 排除 |
|------|------|
| 104 个 Knip 确认死文件 + 14 未用依赖删除 | 动态 import/反射注册的死代码（需人工复核） |
| `utils/http/axios` 抽 `IUserContext` 反转依赖 | axios 拦截器业务逻辑变更（行为不变） |
| 路由懒加载（router→views 改 dynamic import） | 路由表结构/权限模型变更 |
| `vendor-common` 按路由分包 | Vite/Rollup 升级 |
| `ColumnDesign/Main.vue` + `AiChatPanel.vue` 拆解 | 这两个组件的 UI 行为/外观变更 |
| 重叠库收敛（echarts vs highcharts 二选一等） | 重叠库替换期间的双库共存过渡（单独 PR） |
| 全页面 a11y 修正（lang/viewport/role） | 全量 WCAG 2.1 AA 合规（长期目标） |
| 293 处 @click 绑 div → button/role | 设计系统级 a11y 组件库建设 |
| `stylelint --fix` + computed 副作用清理 | WindiCSS → Tailwind 迁移 |

## 数据锚定（诊断证据）

| 维度 | 数值 | 来源 |
|------|------|------|
| 循环依赖 | **182 环**（89% 基础设施互咬；黑洞 store/user 126 环） | [`design-quality-frontend-ct-report.md`](../../docs/architecture/v52/design-quality-frontend-ct-report.md) §5.1 |
| Bundle | JS 14.8MB；**vendor-common 7.8MB 占 55%** | [`design-quality-frontend-static-deep.md`](../../docs/architecture/v52/design-quality-frontend-static-deep.md) §4 |
| 死代码 | **104 文件 + 14 依赖 + 214 导出** | 同上 §1 |
| 重复依赖 | 4 类重叠库 + **196 多版本包** | 同上 §2 |
| 最高复杂度 | CC 232（dynamicModel/list）；20 个巨型文件 | CT 报告 §4.1 |
| a11y | 3 critical + 5 serious（运行时）；293 键盘不可达（静态） | [`design-quality-frontend-runtime.md`](../../docs/architecture/v52/design-quality-frontend-runtime.md) §1 |
| 性能 | LCP max 1020ms（健康）；home/workStation 各 2-3 长任务 | 同上 §2 |

## Status

- [x] 九维度诊断完成（CT报告 + 静态深度 + 运行时 三份报告）
- [x] 统一路线图完成（[`design-quality-frontend-remediation-roadmap.md`](../../docs/architecture/v52/design-quality-frontend-remediation-roadmap.md)）
- [ ] spec 草稿（本 proposal + `design.md` + `tasks.md`）
- [ ] 用户审批
- [ ] 实施

## 关联

- CT 报告：[`design-quality-frontend-ct-report.md`](../../docs/architecture/v52/design-quality-frontend-ct-report.md)
- 静态深度：[`design-quality-frontend-static-deep.md`](../../docs/architecture/v52/design-quality-frontend-static-deep.md)
- 运行时：[`design-quality-frontend-runtime.md`](../../docs/architecture/v52/design-quality-frontend-runtime.md)
- 路线图：[`design-quality-frontend-remediation-roadmap.md`](../../docs/architecture/v52/design-quality-frontend-remediation-roadmap.md)
- 后端对照：[`../backend-arch-code-quality/proposal.md`](../backend-arch-code-quality/proposal.md)
