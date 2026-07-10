# Sandbox 运维指南

## 架构

```
┌─────────────┐     Docker CLI      ┌──────────────────┐
│ SandboxManager│ ◄────────────────► │ Docker Daemon     │
│ (SemaphoreSlim │                   │ ┌──────┐ ┌──────┐│
│  5 并发)      │                   │ │ SBox1│ │ SBox2││
└──────┬──────┘                     │ └──────┘ └──────┘│
       │                            │ ┌──────┐ ┌──────┐│
       │ SQL Server (共享)          │ │ SBox3│ │ SBox4││
       ▼                            │ └──────┘ └──────┘│
┌──────────────┐                    └──────────────────┘
│ JNPF_Sandbox_ │
│ {TenantId}    │  ← per-tenant database
└──────────────┘
```

## 前置条件

1. **Docker** 已安装并运行 (`docker info` 可用)
2. **jnpf-sandbox:latest** 镜像已构建（仓库内 Dockerfile）：

```powershell
# 仓库根目录
powershell -ExecutionPolicy Bypass -File docker/jnpf-sandbox/build.ps1
# 或
docker build -t jnpf-sandbox:latest -f docker/jnpf-sandbox/Dockerfile .
```

3. **SQL Server** 实例可从 Docker 容器访问 (`host.docker.internal`)（后端沙箱连接串场景）
4. `Sandbox:ConnectionStringTemplate` / `Sandbox:Image` 配置正确（默认 `jnpf-sandbox:latest`）
5. `StudioPreview:ProjectPath` 指向本机 `studio-preview` 壳工程（交付预览注入用）

> **说明（30 号 W2）：** 交付预览路径由 `PipelineDeliveryCoordinator` 将生成的 Vue 注入 `studio-preview`，再 `docker cp` 到容器 `/app`，执行 `npm install` + `vite --port 4173`。镜像基于 Node 20，`CMD sleep infinity` 保活。

## 关键配置

| 配置项 | 默认值 | 说明 |
|---|---|---|
| `Sandbox:ConnectionStringTemplate` | `Server=host.docker.internal,1433;Database={DB};...` | 模板中 `{DB}` 会被替换为 `JNPF_Sandbox_{TenantId}` |
| Docker 网络 | `jnpf-sandbox-net` | 自动创建，所有沙箱容器加入此网络 |
| 容器名格式 | `jnpf-sandbox-{Id}` | 便于识别和排查 |
| 资源限制 | 1 核 / 4 GiB (可配) | 单个沙箱的资源配额 |

## 运维命令

```bash
# 查看沙箱容器
docker ps --filter "name=jnpf-sandbox-"

# 查看沙箱网络
docker network inspect jnpf-sandbox-net

# 手动销毁特定沙箱
docker stop -t 10 jnpf-sandbox-{id}

# 查看沙箱资源使用
docker stats --filter "name=jnpf-sandbox-"

# 进入沙箱调试
docker exec -it jnpf-sandbox-{id} /bin/bash
```

## 生命周期

```
创建 (create)
  │
  ▼
就绪 (ready) ◄────────────── 部署 (deploy)
  │                              │
  │                    测试 (testing)
  │                              │
  ▼                              ▼
销毁 (destroying)           就绪 (ready)
  │
  ▼
已销毁 (destroyed)
```

- **创建**: API 调用 → Docker 容器启动 → 30 秒内完成
- **部署**: 上传 zip → 复制到容器 → 解压 → 5 并发限制
- **超时**: 默认 300 秒（5 分钟），超时自动销毁（SandboxCleanupService 每 30 秒检查）
- **销毁**: 立即或超时自动触发，容器自动删除 (`--rm` 标志)

## 并发控制

- `SemaphoreSlim(5, 5)` 限制 5 个并发创建/部署操作
- 超出的请求排队等待

## 故障排查

### 容器创建失败
1. 检查 Docker daemon 状态: `docker info`
2. 检查镜像是否存在: `docker images | grep jnpf-sandbox`
3. 检查端口冲突: `docker ps --filter "publish=8080"`
4. 查看日志: 后端 `SandboxManager` 日志包含详细错误信息

### 沙箱无法访问
1. 确认容器在运行: `docker ps --filter "name=jnpf-sandbox-"`
2. 检查端口映射: `docker port jnpf-sandbox-{id}`
3. 检查网络连通性: `docker exec jnpf-sandbox-{id} curl localhost:8080/health`

### 数据库连接失败
1. 确认 `host.docker.internal` 在容器中可解析
2. 检查 SQL Server 防火墙规则允许 Docker 网络访问
3. 验证连接字符串模板中的凭据

## 监控指标

| 指标 | 来源 | 阈值 |
|---|---|---|
| 活跃沙箱数 | `GET /api/sandbox/list` | < 50 |
| 创建耗时 | 日志 | < 30s |
| 销毁耗时 | 日志 | < 10s |
| 并发队列长度 | SemaphoreSlim 等待数 | < 20 |
| 超时清理数 | SandboxCleanupService 日志 | 监控趋势 |
