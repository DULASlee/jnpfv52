# JNPF V5.2 部署指南

---

## 1. 环境变量清单

| 变量名 | 说明 | 必填 | 默认值 |
|---|---|---|---|
| `JWT_SECRET` | JWT 签名密钥 (Base64) | 是 | — |
| `DB_CONNECTION_STRING` | 数据库连接串 | 是 | — |
| `REDIS_CONNECTION_STRING` | Redis 连接串 | 是 | `127.0.0.1:6379,password=,defaultDatabase=0` |
| `RABBITMQ_CONNECTION_STRING` | RabbitMQ 连接串 | 否 | — |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Jaeger OTLP 端点 | 否 | `http://localhost:4317` |
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | 否 | `Production` |
| `ASPNETCORE_URLS` | 监听地址 | 否 | `http://0.0.0.0:5000` |

---

## 2. CI/CD 流水线

### 三条流水线

| 流水线 | 文件 | 触发条件 |
|---|---|---|
| CI | `.github/workflows/ci.yml` | PR / push to main, develop |
| Staging CD | `.github/workflows/cd-staging.yml` | push to develop / 手动 |
| Production CD | `.github/workflows/cd-production.yml` | release created / 手动 |

### 质量门禁

- **Analyzer gate:** `grep "error JNPF"` 零容忍
- **Security scan:** Critical 级别阻塞 Staging+
- **Health check retry:** Staging 12×5s, Production 18×5s

> 详细配置见 [ci-cd-guide.md](ci-cd-guide.md)

---

## 3. 数据库迁移

### 执行迁移

```bash
cd backend/tools/JNPF.Database.Migrations

# CLI 参数
dotnet run -- --connection "Server=localhost;Database=jnpf;User Id=sa;Password=xxx;TrustServerCertificate=True"

# 环境变量
$env:JNPF_CONNECTION_STRING="Server=localhost;Database=jnpf;User Id=sa;Password=xxx;TrustServerCertificate=True"
dotnet run
```

### 迁移原则

- 脚本使用 `IF NOT EXISTS` 保证幂等
- 已执行脚本不会重复运行（`SchemaVersions` 表追踪）
- 建议在部署前先执行迁移

---

## 4. 健康检查端点

| 端点 | 用途 | 返回 | K8s 配置 |
|---|---|---|---|
| `/health` | 完整健康检查 | `{"status":"Healthy","checks":[...]}` | — |
| `/health/live` | 存活探针 | `Healthy` / `Unhealthy` | `livenessProbe` |
| `/health/ready` | 就绪探针 | `{"status":"Healthy",...}` | `readinessProbe` |

### K8s 配置示例

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5000
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 5
```

---

## 5. Jaeger 部署

```bash
docker run -d \
  --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  -e COLLECTOR_OTLP_ENABLED=true \
  jaegertracing/all-in-one:latest
```

- **UI:** http://localhost:16686
- **OTLP gRPC:** localhost:4317

配置应用连接：
```json
// appsettings.json
{
  "Observability": {
    "OtlpEndpoint": "http://localhost:4317",
    "ServiceName": "jnpf-api"
  }
}
```

---

## 6. 限流配置

三策略自动加载：

| 策略 | 适用场景 | 默认限制 |
|---|---|---|
| `fixed` | 通用 API | 100 req/s per IP |
| `login` | 登录接口 | 5 req/min per IP |
| `export` | 导出接口 | 2 req/min per IP |

配置位置：`appsettings.json` → `IpRateLimiting`

---

## 7. 部署前检查清单

- [ ] `appsettings.json` 配置就绪
- [ ] `ConnectionStrings.json` 连接串正确
- [ ] Redis 可连接
- [ ] RabbitMQ 可连接（如启用）
- [ ] Jaeger 已部署（如启用可观测）
- [ ] 数据库迁移已执行
- [ ] SSL 证书配置（生产环境）
- [ ] `/health` 返回 200
- [ ] `/health/live` 返回 Healthy
- [ ] `/health/ready` 返回 Healthy

---

## 8. 目录结构

```
部署目录/
├── backend/
│   └── JNPF.API.Entry.dll       # 后端入口
├── frontend/
│   └── jnpf-web-vue3/dist/      # PC 前端构建产物
├── datascreen/
│   └── jnpf-web-datascreen/dist/ # 数字大屏构建产物
└── mobile/
    └── jnpf-app-vue3/dist/       # 移动端构建产物
```
