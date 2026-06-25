# Foundry 联调指南

## 概述

本文档描述 Studio（JNPF v5.2 Phase 6）与 Foundry（Baobab-Foundry 独立部署）之间的集成对接方式。

## 通信架构

```
┌─────────────────────────┐         HTTPS + 签名 zip        ┌──────────────────────────┐
│  Studio (JNPF v5.2)     │◄────────────────────────────────►│  Foundry (Baobab-Foundry) │
│                         │                                  │                          │
│  ┌───────────────────┐  │  POST /api/InteAssistant/       │  ┌────────────────────┐  │
│  │ FoundryConnector   │◄─┤  KnowledgePatch/Receive         ├──┤ KnowledgePatch      │  │
│  │ Service            │  │  (multipart/form-data)          │  │ Generator           │  │
│  ├───────────────────┤  │                                  │  ├────────────────────┤  │
│  │ KnowledgePatch     │  │  POST /api/InteAssistant/       │  │ Self-Play Engine    │  │
│  │ Service            │◄─┤  KnowledgePatch/Verify          │  │ (Attacker/Builder/  │  │
│  │ (验签 + 合并)       │  │  (JSON body)                   │  │  Judge 三体)        │  │
│  ├───────────────────┤  │                                  │  └────────────────────┘  │
│  │ FounderService     │──┼─► POST /api/founder/selfplay/   │                          │
│  │ (自博弈开关)        │  │  toggle                        │                          │
│  └───────────────────┘  │                                  └──────────────────────────┘
└─────────────────────────┘
```

## 1. 签名密钥分发

### 密钥生成

```bash
# 在 Studio 侧生成签名密钥
# 方式 1: 通过 API
curl -X POST http://localhost:5000/api/founder/config/signing-key

# 方式 2: 手动生成（OpenSSL）
openssl rand -base64 32
```

### 密钥配置

**Studio 侧** (`appsettings.json` 或环境变量):
```json
{
  "KnowledgePatch": {
    "SignatureKey": "<shared-secret-key>"
  }
}
```

**Foundry 侧** (部署时注入):
```bash
export KNOWLEDGE_PATCH_SIGNATURE_KEY="<shared-secret-key>"
```

### 密钥轮换流程

1. Studio 通过 `FoundryConnectorService.GenerateSigningKey()` 生成新密钥
2. 通过安全通道（如 HashiCorp Vault）同步到 Foundry
3. Foundry 更新签名密钥，新 Patch 使用新密钥
4. Studio 保持旧密钥有效（宽限期 24h），接受两种签名的 Patch
5. 宽限期后删除旧密钥

## 2. KnowledgePatch 数据格式

### Zip 包结构

```
knowledge-patch-v{version}.zip
├── manifest.json          # 元数据
│   {
│     "version": 1,
│     "generatedAt": "2026-06-15T10:30:00Z",
│     "foundryInstance": "baobab-foundry-prod-1",
│     "nodeCount": 10,
│     "edgeCount": 5
│   }
└── knowledge-patch.json   # 实际知识内容
    {
      "nodes": [
        {
          "label": "entity",
          "name": "User",
          "properties": "{\"domain\":\"auth\",\"confidence\":0.95}"
        }
      ],
      "edges": [
        {
          "sourceNodeId": "node-1",
          "targetNodeId": "node-2",
          "relationType": "depends-on",
          "properties": "{\"weight\":0.8}"
        }
      ]
    }
```

### 字段对齐检查清单

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `nodes[].label` | string | ✅ | entity / rule / pattern / anti-pattern |
| `nodes[].name` | string | ✅ | 节点唯一名称 |
| `nodes[].properties` | string (JSON) | ❌ | 扩展属性，JSON 字符串格式 |
| `edges[].sourceNodeId` | string | ✅ | 源节点 ID |
| `edges[].targetNodeId` | string | ✅ | 目标节点 ID |
| `edges[].relationType` | string | ✅ | depends-on / references / similar-to / conflicts-with |
| `edges[].properties` | string (JSON) | ❌ | 关系扩展属性 |

### 签名生成（Foundry 侧参考实现）

```python
import hashlib, hmac, json

def sign_knowledge_patch(content_json: str, secret_key: str) -> tuple[str, str]:
    """返回 (package_hash, signature)"""
    package_hash = hashlib.sha256(content_json.encode()).hexdigest()
    signature = hmac.new(
        secret_key.encode(),
        package_hash.encode(),
        hashlib.sha256
    ).hexdigest()
    return package_hash, signature
```

## 3. HTTPS 证书

### 开发环境
- 使用自签名证书
- Studio 配置: Kestrel 绑定 HTTPS 端口
- Foundry 配置: `NODE_TLS_REJECT_UNAUTHORIZED=0` (仅开发)

### 生产环境
- 使用 CA 签发证书（Let's Encrypt / 公司 CA）
- 双向 TLS (mTLS): Studio 和 Foundry 互相验证证书
- 证书有效期监控和自动续期

## 4. 端到端联调步骤

### Step 1: 启动 Studio
```bash
cd backend && dotnet run --project application/JNPF.API.Entry
```

### Step 2: 配置 Foundry BaseUrl
```json
// appsettings.json
{ "Foundry": { "BaseUrl": "https://baobab-foundry:8443" } }
```

### Step 3: 验证连通性
```bash
curl -H "X-Founder-Token: <token>" http://localhost:5000/api/founder/health/foundry
# 预期响应: { "status": "healthy", "signingKeyFingerprint": "a1b2c3d4" }
```

### Step 4: 端到端 KnowledgePatch 接收测试
```bash
# Foundry → Studio: 发送签名 zip
curl -X POST http://localhost:5000/api/InteAssistant/KnowledgePatch/Receive \
  -F "zip=@test-knowledge-patch.zip" \
  -F "signature=<hmac-sha256-signature>"
# 预期响应: { "success": true, "nodesInserted": 10, "edgesInserted": 5, "patchVersion": 1 }
```

### Step 5: 验证知识图谱更新
```bash
curl -H "X-Founder-Token: <token>" http://localhost:5000/api/InteAssistant/KnowledgePatch/stats
# 预期响应: { "nodeCount": 10, "edgeCount": 5, ... }
```

## 5. 错误处理与重试

| HTTP 状态 | 含义 | Foundry 侧行为 |
|---|---|---|
| 200 | 成功 | - |
| 400 | 请求格式错误 | 检查 zip/JSON 格式 |
| 401 | 缺少 token | 检查 X-Founder-Token header |
| 403 | 签名验证失败 | 检查签名密钥和算法 |
| 500 | 服务器内部错误 | 指数退避重试 (1s → 2s → 4s → 8s，最多 3 次) |
| 不可达 | 网络故障 | 本地缓存 + 达后批量推送 |

## 6. 环境变量清单

| 变量 | 默认值 | 必需 | 说明 |
|---|---|---|---|
| `Foundry:BaseUrl` | (空) | 是 | Foundry 服务地址 (含协议和端口) |
| `Foundry:TimeoutSeconds` | 30 | 否 | HTTP 请求超时秒数 |
| `KnowledgePatch:SignatureKey` | jnpf-default-signing-key | 是 | HMAC-SHA256 签名密钥 (与 Foundry 共享) |
| `App:FounderPhase` | 0 | 是 | 设为 4+ 启用真实认证 |
| `App:FounderJwtKey` | 派生自 AesKey | 否 | founder_token JWT 签名密钥 |
