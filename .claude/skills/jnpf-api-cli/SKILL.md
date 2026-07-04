---
name: jnpf-api-cli
description: JNPF 无浏览器登录与 API 自动测试闭环。scripts/lib/jnpf-auth.mjs 或 jnpf_auth.py 获取 Token，调 Studio/IR/Skill 接口；失败时走 systematic-debugging 自动修复重跑。开发-部署-debug 禁止手点浏览器登录。
---

# JNPF API CLI + 自动测试修复闭环

## 何时使用（MUST）

- 测后端 API、IR 事件、Skill 链路
- Agent 需要 Token，**禁止**打开浏览器登录
- 开发-部署-debug 自动循环、CI、数据驱动调试
- Bug 修复后重跑同一 HTTP 断言

## 标准闭环

```
编码 → dotnet build [/ pnpm type-check]
     → node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser
     → node scripts/phase2-skills-e2e.mjs  (或领域脚本)
     → FAIL: systematic-debugging → 修 → 重跑 (≤3)
     → PASS: 声称该层验证通过
```

前端 UI 交付仍用 Playwright；**后端/API 任务不得因未开浏览器跳过验证**。

## 一键登录

```bash
node scripts/lib/jnpf-auth.mjs --json
python scripts/jnpf_auth.py login --json
```

Token 缓存：`scripts/.jnpf-session.json`（JWT 过期前复用，已 gitignore）

## 调接口

```bash
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser
node scripts/jnpf-api.mjs POST /api/studio/skills/pm/{id}/run "{}"
```

## 脚本 import

```javascript
import { login, apiRequest, isJnpfOk, jnpfData, pick } from '../scripts/lib/jnpf-auth.mjs';
```

## 登录协议

`POST /api/oauth/Login` · form-urlencoded · 密码 = AES(MD5(pwd)) · Header `jnpf-origin: pc`

详见 `scripts/README-api-cli.md` · Cursor 常驻规则 `.cursor/rules/auto-test-fix-loop.mdc`

## 禁止

- ❌ `/api/auth/login`（不存在）
- ❌ JSON body 直传明文密码
- ❌ 重复 `Bearer Bearer`
- ❌ 测试失败不读响应体就改代码
