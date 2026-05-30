# JNPF V5.2 Git 工作流

## 分支策略

| 分支 | 用途 | 保护规则 |
|------|------|----------|
| `main` | 生产就绪代码 | 禁止直接 push，仅接受 dev 合并，需架构师审核 |
| `dev` | 日常集成 | 禁止直接 push，接受 feat/fix 分支 PR |
| `feat/{模块}-{简述}` | 新功能 | 从 dev 创建，完成后 PR 合回 dev |
| `fix/{问题描述}` | Bug 修复 | 从 dev 创建 |
| `hotfix/{描述}` | 生产紧急修复 | 从 main 创建，修复后同时合回 main 和 dev |

## 分支命名规范

```
feat/iot-device-crud        ← 新功能
fix/tenant-filter-leak      ← Bug 修复
hotfix/jwt-expire-check     ← 生产热修复
docs/naming-convention      ← 文档变更
chore/update-dependencies   ← 工具/依赖
```

## 提交信息规范（Conventional Commits）

```
<type>(<scope>): <description>
```

### Type

| 类型 | 用途 |
|------|------|
| feat | 新功能 |
| fix | Bug 修复 |
| docs | 文档 |
| style | 代码格式（不影响逻辑） |
| refactor | 重构 |
| test | 测试 |
| chore | 构建/工具/依赖 |

### Scope（模块缩写）

`system` / `workflow` / `engine` / `iot` / `mes` / `web` / `datascreen` / `app`

### 示例

```
feat(iot): 新增设备注册 Service
fix(system): 修复租户过滤器越权问题
docs(conventions): 补充命名约定文档
chore(deps): 升级 MQTTnet 至 4.x
refactor(workflow): 提取工单状态机为独立类
```

## PR 规则

- PR 模板见 `.github/pull_request_template.md`
- 至少通过 `dotnet build` 零错误
- 涉及数据库变更时必须附 Migration 脚本
- 涉及 API 变更时必须通知前端同步
