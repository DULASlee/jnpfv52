# AGENTS.md

> **Runtime:** GitHub Copilot Agents / Copilot Workspace
>
> 项目完整上下文（架构、模块、命名、工具链、安全规则）统一维护在 CLAUDE.md。
> 本文件仅包含 AGENTS 运行时的差异化配置。

## 项目上下文引用

参见 [CLAUDE.md](./CLAUDE.md)

## AGENTS 运行时特有配置

- 本文件服务对象为 GitHub Copilot Agents / Copilot Workspace
- 项目架构约定、命名规范、工具链矩阵等完整上下文请参见 CLAUDE.md
- Copilot Agent 在 PR/Issue 上下文中加载时，应优先引用 CLAUDE.md 中的规范

## 项目结构参考

```
d:\JNPF-v52\
├── backend\              # .NET solution (zx_lowcode_netcore.sln)
│   ├── framework\        # Core: DynamicApiController, DI, SqlSugar, JWT, Serilog
│   ├── infrastructure\   # Cross-cutting: event bus, OAuth, WebSockets
│   ├── modularity\       # Business modules (15个)
│   ├── application\      # Hosts: JNPF.API.Entry, JNPF.OA.API.Entry
│   └── web\              # SQL init + static assets
├── jnpf-web-vue3\       # PC frontend → :3100
├── jnpf-web-datascreen\ # Data screen → :8100/DataV/
├── jnpf-app-vue3\       # UniApp mobile → :3800 (H5 + proxy)
└── docs\                # Demo manual, architecture, toolchain
```

## Docker 部署命令

```bash
cd d:\JNPF-v52\backend
docker build -f application/JNPF.API.Entry/Dockerfile -t jnpf-api .
```

## Release 构建命令

```bash
cd d:\JNPF-v52\backend
dotnet build -c Release
```

## 关键约束（与 CLAUDE.md 一致）

- **Dynamic API**: 禁止手动创建 Controller，所有 API 由 Service + IDynamicApiController 自动映射
- **Unified Response**: `RESTfulResult<T>` 自动包装，异常用 `Oops.Oh()`
- **Database**: SqlSugar (SQL Server) + Dapper，表名全大写 + 下划线分隔
- **工具链**: 日常开发用 superpowers 技能集，/opsx:apply 严禁用于编码操作
