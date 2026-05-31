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

---

## Git 工作铁律（精简版）

> **一句核心：任何操作前，保证工作区干净、已提交、已推送。**

### 铁律三条

1. **绝不带未提交改动切分支、合并或变基。**  
   凡有改动，先 `add` → `commit` → `push`，哪怕提交信息写 `WIP`。
2. **新建或修改文件后，立即 `add` 并提交。**  
   文档与代码同等重要，没有例外。未跟踪文件最易丢失，stash 也救不了。
3. **合并/切分支前，执行三步检查：**  
   - `git status` 必须 clean  
   - `git stash list` 必须为空  
   - `git log origin/当前分支..当前分支` 必须空（本地已全推送）

**记住：远程仓库是你唯一的不可丢失备份。每 30 分钟至少 commit + push 一次。**

> Cursor Agent 永久规则：`.cursor/rules/git-workflow.mdc`（`alwaysApply: true`）

---

### 事故备忘：2026-05-31 stash drop 导致文档丢失

**事件：** `git stash drop` 删除 SHA `56d3c660`，导致 `docs/架构迭代/` 下 4 个架构文档（5、6、7、8号深潜文档）从磁盘消失。

**根因：** 文件为 untracked 状态，stash 仅保存了 index 中的部分引用，drop 后工作区副本被后续操作清理。

**恢复：** 通过 `git fsck --unreachable --no-reflogs` 从 git 对象库找回全部 4 个文件（共 4929 行）。

**教训：**
1. Untracked 文件是 stash 的盲区 — stash 不保存 untracked 内容（除非用 `-u`）
2. `git stash drop` 是不可逆操作，执行前必须确认内容已提交
3. 任何文件一旦确认需要保留，**立即 commit**，不要留在 untracked 状态
