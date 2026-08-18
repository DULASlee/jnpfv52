# JNPF 无浏览器 API 工具链

开发 / 部署 / Debug 自动循环：**不打开浏览器**，自动 MD5+AES 登录，带 Token 调任意接口。

## 快速开始

```powershell
# 1. 确保后端 :5000 已启动
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1

# 2. 取 Token（缓存到 scripts/.jnpf-session.json，JWT 过期前自动复用）
node D:\JNPF-v52\scripts\lib\jnpf-auth.mjs

# 3. 调任意 API
node D:\JNPF-v52\scripts\jnpf-api.mjs GET /api/oauth/CurrentUser
node D:\JNPF-v52\scripts\jnpf-api.mjs POST /api/studio/skills/pm/1/run "{}"
```

Python 版（需 `pip install requests pycryptodome`）：

```powershell
python D:\JNPF-v52\scripts\jnpf_auth.py login
python D:\JNPF-v52\scripts\jnpf_auth.py GET /api/studio/ir/1/events
```

## 环境变量

| 变量 | 默认 |
|------|------|
| `JNPF_API_URL` | `http://localhost:5000` |
| `JNPF_ACCOUNT` | `admin` |
| `JNPF_PASSWORD` | `123456` |
| `JNPF_CIPHER_KEY` | `EY8WePvjM5GGwQzn`（与 `App.json` 一致） |
| `JNPF_ORIGIN` | `pc` |

## 领域 E2E（分步执行，避免卡死）

**主入口：** `scripts/studio-e2e.mjs`

```powershell
node scripts/studio-e2e.mjs init
node scripts/studio-e2e.mjs step s0-gate --skip-if-done    # 幂等：已完成则跳过
node scripts/studio-e2e.mjs step s2-wait --poll-once       # 探针，不阻塞
node scripts/studio-e2e.mjs status --json                  # 含 suggestedNext + stepTimings
```

**稳定性特性：**
- API 45s 超时 + 3 次指数退避（502/503/504/429/网络错误）
- `status` 并行拉取 events / runs / deliverables
- 长等待自适应降频（1.5s → 5s），减轻 backend 压力
- `--skip-if-done` 步骤幂等；409 冲突自动视为「已在运行」
- `.e2e-lock.json` 防同一 pipeline 并发 E2E（`--no-lock` 可跳过）

状态：`scripts/.e2e-state.json` · 锁：`scripts/.e2e-lock.json`

| 兼容脚本 | 说明 |
|----------|------|
| `phase2-skills-e2e.mjs` | 转发 studio-e2e |
| `business-p0-e2e.mjs` | P0 门控+PM |

## 在 Agent / CI 脚本里复用

```javascript
import { login, apiRequest } from './scripts/lib/jnpf-auth.mjs';

const { token } = await login();
const res = await apiRequest('GET', '/api/studio/ir/42/events');
console.log(res.json);
```

```python
from jnpf_auth import login, api_request  # 或 subprocess 调 jnpf_auth.py

session = login()
print(api_request("GET", "/api/studio/ir/42/events"))
```

## 阶段二 E2E（无浏览器）

```powershell
node D:\JNPF-v52\scripts\phase2-skills-e2e.mjs
node D:\JNPF-v52\scripts\phase2-dod-verify.mjs
```

## 阶段三 DoD 验收（API 子集）

```powershell
node D:\JNPF-v52\scripts\phase3-dod-verify.mjs
```

自动化项：D1 / D6 / D7 / D10 / D13 / D15 / D19 + budget API + IR-2 快照。  
SKIP 项：D8–D9、D11–D14、D16–D18、D20。  
迁移：`node scripts/run-inte-migration.mjs`（含 `20260801_Phase3_Design_Skills.sql`）  
报告：`.claude/evidence/phase3-dod-verify.json`

## 阶段四 Green path / DoD（D14–D16）

**Python（推荐）：**

```powershell
pip install requests pycryptodome
python scripts/phase4_diagnose.py 209          # 失败时先诊断
python scripts/phase4_green_path.py            # D14 全链路
python scripts/phase4_green_path.py --pipeline-id 209
```

**Node 等价：**
# D14 leave-simple 端到端（需 :5000 存活，developer 编排含 sandbox build，默认 30min 超时）
node D:\JNPF-v52\scripts\phase4-green-path.mjs
node D:\JNPF-v52\scripts\phase4-green-path.mjs --pipeline-id 123

# D15-D16 总 DoD（含 D3/D5/D11/D14/PhaseB/Q4）
node D:\JNPF-v52\scripts\phase4-dod-verify.mjs
node D:\JNPF-v52\scripts\phase4-dod-verify.mjs --skip-host   # 快速回归，正式 D16 勿用
```

报告：`.claude/evidence/phase4-d14-green-path.json` · `.claude/evidence/phase4-dod-verify.json`

Dev simulate 扩展（IR-2）：

```powershell
node scripts/jnpf-api.mjs POST /api/studio/ir/42/simulate "{\"eventType\":\"DDLStabilized\",\"injectLayerViolation\":true}"
```

## 登录原理（与前端一致）

```
plain password → MD5(hex) → AES-128-ECB(PKCS7, App.json AesKey) → hex
POST /api/oauth/Login  (application/x-www-form-urlencoded)
Header: jnpf-origin: pc
```

参考实现：`jnpf-app-vue3/scripts/verify-login-api.mjs`（已有）、`scripts/lib/jnpf-auth.mjs`（本仓库统一版）。

## 与 Playwright 的分工

| 场景 | 工具 |
|------|------|
| API / IR / Skill 断言 | `jnpf-api.mjs` / `jnpf_auth.py` |
| 页面 console / DOM 调试 | Playwright `--headed` 或 `page.on('console')` |
| 手点验收 | 仅 UI 走查 |

Token 缓存文件 `scripts/.jnpf-session.json` 已 gitignore，勿提交。
