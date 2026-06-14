# Founder Console 使用指南

## 概述

创始人控制台是 JNPF v5.2 Phase 6 的核心交付，提供 AI 模型配置、自博弈管理、知识图谱审核和安全认证功能。

## 访问方式

- URL: `{JNPF_PC_BASE}/founder/console`
- 需要创始人 TOTP 认证
- 默认 Phase 由 `App:FounderPhase` 配置控制：0=未开放, 3=需认证(地桩), 4+=真实 TOTP 认证

## 认证流程

1. **设置 TOTP**: 输入创始人邮箱 → 获取 Base32 密钥和 Google Authenticator 二维码
2. **扫描二维码**: 使用 Google Authenticator / Authy / 1Password 扫描
3. **验证登录**: 输入 6 位动态码 → 签发 `founder_token` (有效期 12 小时)
4. **后续请求**: 所有 `/api/founder/*` 请求自动携带 `X-Founder-Token` header

## 功能模块

### 1. 模型配置
- 主模型选择 (默认: `deepseek-v4-pro`)
- 备用模型 (降级方案)
- Temperature (0.0-2.0)
- Max Tokens (256-32768)

### 2. Prompt 模板配置
- API: `POST /api/founder/config/prompt`
- 分类管理 (codegen / review / test / security)

### 3. 自博弈管理
- 启动/暂停自博弈引擎
- 查看状态: 轮次、通过率、知识节点数
- API: `POST /api/founder/selfplay/toggle`

### 4. 知识图谱审核
- 节点浏览（按标签/域过滤）
- 节点详情（Properties JSON 解析）
- 关系边查看（按关系类型过滤）
- 统计面板（节点数/边数/版本/标签分布）

### 5. 安全审计
- 认证日志: 查看所有 `/api/founder/*` 访问记录
- 按结果筛选: allow / deny / missing_token / invalid_token
- 时间、用户、IP、User-Agent 完整记录

### 6. 沙箱管理
- 创建沙箱: 指定租户、CPU、内存、超时
- 沙箱列表: 实时状态 (creating/ready/testing/destroying/destroyed/error)
- 销毁沙箱: 单个或批量
- 部署: 上传 zip 文件到沙箱

## 环境变量

| 变量 | 默认值 | 说明 |
|---|---|---|
| `App:FounderPhase` | 0 | 创始人功能阶段 (4+ 启用真实认证) |
| `App:FounderJwtKey` | 派生自 AesKey | founder_token JWT 签名密钥 |
| `App:FounderTotpIssuer` | JNPF-Founder | TOTP 二维码中显示的发行方 |
| `KnowledgePatch:SignatureKey` | jnpf-default-signing-key | KnowledgePatch 签名密钥 |
| `Foundry:BaseUrl` | (空) | Foundry 服务地址 |
| `Foundry:TimeoutSeconds` | 30 | Foundry 接口超时 |
| `Sandbox:ConnectionStringTemplate` | 内置默认 | 沙箱数据库连接字符串模板 |
