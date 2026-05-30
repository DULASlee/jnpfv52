# JWT 密钥迁移状态报告

**报告日期：** 2026-05-30
**报告人：** 工程师
**状态：** ❌ 未完成（P0 阻塞项）

---

## 当前状态

### 问题描述

`backend/application/JNPF.API.Entry/Configurations/JWT.json` 中包含硬编码的签名密钥：

```json
{
  "JWTSettings": {
    "IssuerSigningKey": "RkayGi4ltkMWrSQKsQTWic1VnakqsQfaJOmJIBUWE1gxGaS0IrJHxa9anjVAwuew",
    "ValidIssuer": "yinmaisoft",
    "ValidAudience": "yinmaisoft"
  }
}
```

### 风险分析

| 风险项 | 严重程度 | 说明 |
|--------|----------|------|
| 密钥泄露 | 🔴 严重 | JWT.json 未被 .gitignore 排除，密钥已提交到版本库 |
| 旧密钥废弃 | 🔴 严重 | 当前密钥为旧版密钥，需生成新的强密钥 |
| 签发方/签收方 | 🟡 中等 | "yinmaisoft" 为旧品牌名，需更新为 "baobabtech" |

### 当前 .gitignore 状态

- `ConnectionStrings.json` — ✅ 已被 .gitignore 排除
- `JWT.json` — ❌ 未被 .gitignore 排除

---

## 执行计划

### Step 1: 生成新的强密钥

```bash
# 使用 OpenSSL 生成 64 字节随机密钥
openssl rand -base64 64
```

### Step 2: 创建安全的配置文件

创建 `JWT.secrets.json`（gitignored），包含：
- 新生成的强密钥
- 更新后的签发方/签收方（baobabtech）

### Step 3: 修改 JWT.json

将 JWT.json 中的敏感信息替换为占位符：
```json
{
  "JWTSettings": {
    "IssuerSigningKey": "${JWT_SIGNING_KEY:请从环境变量或 JWT.secrets.json 读取}",
    "ValidIssuer": "baobabtech",
    "ValidAudience": "baobabtech"
  }
}
```

### Step 4: 更新 .gitignore

在 .gitignore 中添加：
```
**/Configurations/JWT.secrets.json
**/Configurations/JWT.json
```

### Step 5: 修改代码读取逻辑

修改 `backend/application/JNPF.API.Entry/` 中的 JWT 配置读取逻辑：
- 优先从环境变量 `JWT_SIGNING_KEY` 读取
- 其次从 `JWT.secrets.json` 读取
- 最后从 `JWT.json` 读取（仅用于开发环境）

---

## 阻塞说明

**此项目在 JWT 密钥处理干净之前，不得进入核心架构开发。**

原因：
1. 安全红线：硬编码密钥已提交到版本库
2. 旧密钥废弃：需生成新密钥并更新所有相关配置
3. 品牌更新：签发方/签收方需从 "yinmaisoft" 更新为 "baobabtech"

---

## 所需资源

1. 架构师确认新密钥的生成标准（长度、复杂度要求）
2. 架构师确认签发方/签收方的更新策略
3. 架构师确认代码读取逻辑的优先级顺序

---

**请架构师指示是否可以开始执行 JWT 密钥迁移。**
