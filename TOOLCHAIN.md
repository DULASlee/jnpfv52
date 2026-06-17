# 工具链（本仓库）

本仓库已安装 **Superpowers + OpenSpec + episodic-memory**。

| 文档 | 说明 |
|------|------|
| [docs/toolchain/SETUP.md](docs/toolchain/SETUP.md) | 安装、迁移、验证 |
| `.cursor/toolchain.manifest.json` | 项目 ID：`jnpf-v52` |

## 验证命令

```bash
node scripts/verify-toolchain.mjs
node scripts/episodic-sync.mjs --stats
```

## Git Hooks 启用

本仓库使用 `.githooks/` 目录管理 git hooks（如 post-commit 自动刷新知识库）。
`core.hooksPath` 是本地配置，不会通过 git 共享，克隆后需手动启用：

```bash
git config core.hooksPath .githooks
```

macOS/Linux 用户还需确保 hook 有执行权限：

```bash
chmod +x .githooks/post-commit
```
