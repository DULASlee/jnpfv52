# API 验收分层（对照业界工具）

> **原则：** 不要用一个 `.mjs` 脚本干所有事。按场景选工具，共享 `scripts/lib/jnpf-auth.mjs` 鉴权。

## 你的痛点 vs 工具映射

| 痛点 | 旧做法 | 新做法 | 命令 |
|------|--------|--------|------|
| 手工调几个 API、看 Response | 写 `jnpf-api.mjs` 一行行敲 | **REST Client `.http`** | `pnpm sync:http-env` → 打开 `api-tests/http/studio-s2-chain.http` |
| 断言交付物 / IR 事件 | `phase-sup-s2-e2e.mjs verify` 裸脚本 | **Vitest 结构化测试** | `E2E_PIPELINE_ID=311 pnpm test:api` |
| 等 Skill 跑完（分钟级） | `phase-sup-s2-e2e.mjs` 分步 | **保留分步 mjs**（含 watch/heartbeat） | `node scripts/phase-sup-s2-e2e.mjs analyst` |
| LLM 输出质量 | 正则匹配 | **promptfoo**（已有） | `npx promptfoo@latest eval -c promptfoo/promptfooconfig.yaml` |
| 页面控件 / 门控 UI | 手点浏览器 | **Playwright**（已有） | `pnpm e2e:studio:gate` |
| 单接口秒级冒烟 | curl | **dotnet xUnit**（已有） | `dotnet test ... --filter StudioApiSmokeTests` |
| sa-service 单元 | 裸脚本 | **Vitest**（sa-service 已有） | `cd sa-service && npm test` |

## 三层 Dev Loop（推荐日常顺序）

```
1. pnpm sync:http-env          # Token → http-client.env.json（探针按需）
2. .http 点 Send               # 快速探 API / confirm 物化
3. E2E_PIPELINE_ID=311 pnpm test:api   # ★ 结构化断言（~10s，日常默认）
4. node scripts/phase-sup-s2-e2e.mjs verify   # 仅 evidence / 长链（按需，勿替代 3）
```

> **知识库：** `openspec/specs/studio-e2e-toolchain/spec.md` · **禁止**日常只跑 4 而跳过 3。

## REST Client 快测

JNPF 登录是 **MD5+AES form-urlencoded**，`.http` 无法直接登录。流程：

```powershell
node scripts/lib/jnpf-auth.mjs --json
pnpm sync:http-env
# VS Code 打开 api-tests/http/studio-s2-chain.http，选 dev 环境，Send Request
```

`http-client.env.json` 已 gitignore，勿提交 Token。

## Vitest API 测试

```powershell
# 快测（需已有 pipeline，~10s）
$env:E2E_PIPELINE_ID="311"
pnpm test:api

# watch 模式开发断言
pnpm test:api:watch
```

无 `E2E_PIPELINE_ID` 且无 `scripts/.sup-e2e-state.json` 时，仅跑「API 可达」冒烟。

## 仍用 `.mjs` 的场景（不要删）

| 脚本 | 保留原因 |
|------|----------|
| `phase-sup-s2-e2e.mjs` | 分步 + Skill watch + evidence 落盘 |
| `lib/jnpf-auth.mjs` | 唯一正确登录协议 |
| `lib/phase-sup-api.mjs` | Vitest / mjs 共享业务 API |
| `lib/e2e-runner.mjs` | 重试 / 锁 / poll-once（可逐步迁入 Vitest setup） |

## 不要用的

- `scripts/studio-e2e.mjs` 全链（已废弃）
- 为每个生成 API 写 curl 清单
- 用 `.http` 替代 Skill 等待（无轮询能力）

详见 `e2e/README.md`（Playwright）· `scripts/README-api-cli.md`（CLI）
