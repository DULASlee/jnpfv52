# 工具链安装与迁移（Superpowers + OpenSpec + episodic-memory）

> 模板仓库：`d:\liu202505v2`  
> 生产仓库示例：`d:\JNPF-v52`

## 1. 前置条件（本机一次）

| 组件 | 安装方式 | 验证 |
|------|----------|------|
| **OpenSpec CLI** | `npm i -g @fission-ai/openspec` | `openspec --version` |
| **episodic-memory 插件** | Cursor：安装 Superpowers marketplace 的 episodic-memory MCP | MCP 列表可见 `search` / `read` |
| **episodic CLI**（可选，供 hooks sync） | 随 Claude 插件缓存，或设 `EPISODIC_MEMORY_CLI` | `node scripts/episodic-sync.mjs --stats` |
| **摘要 API**（可选） | 复制 `.env.toolchain.example` → `.env.toolchain` 并填 `EPISODIC_MEMORY_API_*` | sync 日志无 `Summary generation failed` |

## 2. 一键迁移到新代码库

在**模板仓库**执行：

```powershell
cd d:\liu202505v2
.\scripts\install-toolchain.ps1 `
  -TargetPath "d:\JNPF-v52" `
  -EpisodicProjectId "D--JNPF-v52" `
  -ProjectSlug "JNPF-v52" `
  -DisplayName "JNPF v5.2 clean workspace"
```

然后验证：

```powershell
cd d:\JNPF-v52
node scripts\verify-toolchain.mjs
node scripts\episodic-sync.mjs --stats
```

## 3. 各组件职责（勿混用）

| 工具 | 职责 | 目录 |
|------|------|------|
| **Superpowers** | 开发推进：brainstorm → plan → execute → verify | `.cursor/skills/` |
| **OpenSpec** | 知识库：`openspec/specs/` 归档真相；changes 草稿 → archive | `openspec/` |
| **episodic-memory** | 跨会话 WHY；hooks 自动 sync + Agent search | `.cursor/episodic/`、`scripts/episodic-sync.mjs` |

**禁止**：用 `/opsx:apply` 或 `/opsx:explore` 替代日常开发（见 `.cursor/rules/toolchain-division.mdc`）。

## 4. 项目身份（迁移必改）

唯一配置文件：**`.cursor/toolchain.manifest.json`**

```json
{
  "episodic_project_id": "D--JNPF-v52",
  "project_slug": "JNPF-v52"
}
```

规则：

- `episodic_project_id` 与 Cursor 工作区路径对应（`D--` + 盘符路径，如 `d:\JNPF-v52` → `D--JNPF-v52`）
- **勿**使用小写 `d--`
- 迁移后检查 `.cursor/episodic/search-templates.yaml` 中 `project_id` 与 query 里的 slug

## 5. OpenSpec 在新库中的用法

```powershell
cd d:\JNPF-v52
openspec list                    # 无 active change 为正常
# 可选起草
# /opsx:propose "capability-name"
# 定稿后 /opsx:archive → 写入 openspec/specs/<capability>/spec.md
```

首次可为 v5.2 建 capability，例如 `openspec/specs/jnpf-v52-demo/spec.md`。

## 6. episodic 日常

```powershell
# 手动同步（与 sessionStart/stop hook 相同）
node scripts/episodic-sync.mjs

# MCP 检索（Agent 会话首轮）
# project = manifest.episodic_project_id
# query 见 .cursor/episodic/search-templates.yaml
```

## 7. 迁移清单

- [ ] `install-toolchain.ps1` 执行完成
- [ ] `node scripts/verify-toolchain.mjs` 全部 OK
- [ ] `toolchain.manifest.json` 的 `episodic_project_id` 已改
- [ ] `openspec/specs/` 至少有 1 份与本项目相关的 spec
- [ ] Cursor 重启后 `/opsx:propose` 等命令可用
- [ ] episodic MCP `search` 使用新 project id 有命中（sync 后）

## 本节核心表清单

无数据库表。

## 本节关键代码路径索引

| 路径 | 说明 |
|------|------|
| `.cursor/toolchain.manifest.json` | 项目身份 |
| `scripts/install-toolchain.ps1` | 迁移脚本 |
| `scripts/verify-toolchain.mjs` | 健康检查 |
| `scripts/episodic-sync.mjs` | 会话索引 sync |
| `openspec/config.yaml` | OpenSpec 配置 |
| `.cursor/rules/toolchain-division.mdc` | 三分工铁律 |
