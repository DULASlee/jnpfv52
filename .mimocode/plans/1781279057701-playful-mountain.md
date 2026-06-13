# Plan: 修复并确保所有 Skills 和 MCP 工具可调用

## 当前状态

### Skills（✅ 已可用）
6 个 skill 全部可通过 `skill` 工具加载：
- `jnpf-ui-enhance` - JNPF 前端 UI 品味提升
- `frontend-design` - 前端设计
- `ui-ux-pro-max` - UI/UX 设计指南
- `frontend-design-pro` - 前端设计专业版
- `taste-skill` - 设计品味
- `bencium-controlled-ux-designer` - UX 设计

### MCP 工具（❌ 不可用）
`.mcp.json` 配置了 3 个 MCP 服务，但二进制文件不在 PATH 中：
- `graphify` — 代码图谱分析
- `serena` — LSP 符号级代码操作
- `episodic-memory` — 跨会话记忆

## 修复步骤

### Step 1: 诊断 MCP 工具安装情况
- 检查 `npm list -g` / `pip list` 看是否已安装
- 检查 `.mcp.json` 中的 `command` 字段是否指向正确路径
- 检查是否有本地 `node_modules/.bin/` 或虚拟环境中安装

### Step 2: 安装缺失的 MCP 工具
根据诊断结果：
- 如果是 npm 包：`npm install -g graphify serena episodic-memory`（或项目本地安装）
- 如果是 pip 包：`pip install graphify serena episodic-memory`
- 如果是自定义二进制：确认下载路径并添加到 PATH

### Step 3: 验证 MCP 服务可启动
```bash
graphify --version
serena --version
episodic-memory --version
```

### Step 4: 更新 .mcp.json（如需要）
如果命令路径需要调整，更新 `.mcp.json` 中的 `command` 和 `args`。

### Step 5: 验证集成
在实际任务中测试：
- `serena`：查找 C# 符号声明
- `graphify`：生成代码依赖图
- `episodic-memory`：搜索跨会话记忆

## 关键文件
- `/mnt/d/JNPF-v52/.mcp.json` — MCP 服务配置
- `/mnt/d/JNPF-v52/CLAUDE.md` — 开发规则和工具链说明

## 验证方式
- 运行每个 MCP 工具的 `--version` 或 `--help` 命令确认可用
- 在一个实际代码任务中调用 `serena` 进行符号查找
- 在 `.claude/skills/` 中加载任意 skill 确认无报错
