# JNPF V5.2 CI/CD Pipeline Guide

## Overview

3 条 GitHub Actions 流水线覆盖从 PR 到生产的全流程。

| 流水线 | 文件 | 触发条件 | 用途 |
|---|---|---|---|
| CI | `.github/workflows/ci.yml` | push/PR to main, develop | 编译、测试、配置校验 |
| Staging CD | `.github/workflows/cd-staging.yml` | push to develop / 手动 | 部署到 staging 环境 |
| Production CD | `.github/workflows/cd-production.yml` | release created / 手动 | 部署到生产环境 |

---

## CI 流水线 (ci.yml)

### Job 结构

```
backend (Build & Test)
  ├── dotnet restore
  ├── dotnet build (/p:CI_BUILD=true)
  ├── Analyzer check (grep "error JNPF" → block)
  ├── dotnet test (+ coverage collection)
  ├── Security scan (non-blocking, 报告漏洞)
  └── Build warning stats (non-blocking, 统计警告)

frontend-web (PC Build)
  ├── pnpm install --frozen-lockfile
  ├── pnpm lint (non-blocking)
  └── pnpm build

frontend-datascreen (DataV Build)
  ├── pnpm install --frozen-lockfile
  └── pnpm build

docker-validate → config-validate (并行收尾)
```

### 质量门禁

| 门禁 | 阻断? | 说明 |
|---|---|---|
| Analyzer check | 阻断 | 匹配 `error JNPF` 即失败 |
| Security scan | 非阻断 | 输出 warning，需人工排查 |
| Build warnings | 非阻断 | 统计数量到 `warnings` env var |

### 验证方式

- **正常提交**：所有 job 绿色通过
- **违规提交**：含 `JNPF001` 等 error 时，Analyzer check 失败 (exit 1)
- **修复后**：Analyzer check 恢复通过

---

## Staging CD 流水线 (cd-staging.yml)

### Job 结构

```
test (条件执行)
  ├── dotnet restore
  ├── Analyzer quality gate (阻断)
  └── dotnet test

build (矩阵构建)
  ├── api (Dockerfile.staging)
  ├── web (Dockerfile.staging)
  └── datascreen (Dockerfile)

deploy
  ├── SSH deploy
  ├── docker compose pull + up -d
  ├── Health check (12×5s retry loop)
  └── Notify success/failure
```

### 质量门禁

| 门禁 | 位置 | 阻断? |
|---|---|---|
| Analyzer gate | test job | 阻断 |
| Health check | deploy job | 阻断 (12 次重试后失败) |

### 健康检查

```bash
# retry loop: 12 attempts × 5s = 60s total
for i in $(seq 1 12); do
  if curl -sf http://localhost:5000/health; then
    exit 0
  fi
  sleep 5
done
exit 1
```

### 跳过测试

手动触发时可勾选 `skip_tests` 跳过测试阶段，但仍需通过 build + health check。

---

## Production CD 流水线 (cd-production.yml)

### Job 结构

```
validate → quality-gate → build → deploy

validate: 手动触发确认字检查
quality-gate:
  ├── Analyzer quality gate
  ├── Security scan (Critical 阻断)
  └── dotnet test

build (矩阵): api + web + datascreen

deploy:
  ├── Backup (DB + config)
  ├── docker compose pull + up -d
  ├── Health check (18×5s retry loop)
  ├── Health verify (health-check.sh)
  └── Notify success/failure
```

### 质量门禁

| 门禁 | 阻断? | 说明 |
|---|---|---|
| 手动确认 | 阻断 | 必须输入 `deploy-production` |
| Analyzer gate | 阻断 | `error JNPF` 即失败 |
| Security scan | 阻断 | 任何 Critical 漏洞阻断部署 |
| dotnet test | 阻断 | 任何测试失败阻断 |
| Health check | 阻断 | 18×5s=90s 后仍未 healthy 则失败 |
| Health verify | 阻断 | 运行 `scripts/health-check.sh` |

### 生产特有：备份

部署前自动备份：
- SQL Server 数据库 (BACKUP DATABASE)
- docker compose 状态 (`docker compose ps`)
- `docker-compose.production.yml` + `.env.production` 副本

备份目录：`/opt/jnpf/backups/YYYYMMDD_HHMMSS/`

---

## 环境变量与密钥

| 变量/密钥 | 流水线 | 用途 |
|---|---|---|
| `secrets.GITHUB_TOKEN` | ALL | 容器仓库认证 |
| `secrets.STAGING_HOST/USER/SSH_KEY` | Staging CD | SSH 部署 |
| `secrets.PRODUCTION_HOST/USER/SSH_KEY` | Production CD | SSH 部署 |
| `secrets.DB_PASSWORD` | CD | 数据库密码 |
| `secrets.REDIS_PASSWORD` | CD | Redis 密码 |
| `secrets.JWT_SECRET_KEY` | CD | JWT 签名密钥 |
| `vars.DEPLOY_PATH` | CD | 部署路径 (默认 /opt/jnpf) |
| `vars.STAGING_URL` | Staging CD | 环境 URL |
| `vars.PRODUCTION_URL` | Production CD | 环境 URL |

---

## 添加新分析器规则

当 Task 7.6 创建 Roslyn Analyzer 后：

1. Analyzer 规则以 `JNPF` 前缀命名 (e.g. `JNPF001`, `JNPF002`)
2. 在 `.editorconfig` 中将规则设为 `error`
3. CI 的 `grep "error JNPF"` 会自动捕获并阻断流水线
4. 确认规则覆盖范围后，可调整阻断阈值（当前：任何 JNPF error 都阻断）

---

## 常见问题

**Q: Analyzer gate 当前没有任何 JNPF error，如何验证？**
A: 在代码中临时加入已知的违规模式（如 `throw new Exception()`），触发 JNPF001 error，推送后确认流水线失败，然后修复。

**Q: Staging Health check 端口是 5000，是否正确？**
A: 正确。JNPF API 默认监听 5000 端口，`/health` 端点由 `LogHealthCheckService` 提供。

**Q: 生产 Security scan 阻拦 Critical 漏洞，低危漏洞呢？**
A: 仅 Critical 阻断部署。High/Moderate/Low 输出 warning 但不阻断，需定期审查。
