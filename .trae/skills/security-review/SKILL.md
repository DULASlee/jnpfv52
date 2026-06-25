---
name: "security-review"
description: "Executes security scan for SQL injection, XSS, sensitive data leakage, auth bypass. Invoke when user asks for security review, before merging sensitive modules (auth/payment/file upload), or mentions security concerns."
---

# Security Review — 安全审查

针对 JNPF v5.2 技术栈执行定向安全扫描，覆盖 OWASP Top 10 在本项目的实际风险点。

> **与 full-review 的区别：** full-review 关注代码质量（编译/规范/重复），security-review 关注安全漏洞（注入/越权/泄露）。

## 执行步骤

### Step 1: 收集审查范围

```bash
# 已提交 + 未提交的变更
git diff --name-only HEAD~1 HEAD
git diff --name-only
```

合并去重，得到本次审查的文件清单。

如果变更文件**全部**是 `*.md`/`docs/**`/配置文件 → 输出"无需安全审查"报告，结束。

### Step 2: SQL 注入检查（SqlSugar + Dapper）

JNPF 使用 SqlSugar ORM，主要风险点：

```bash
# 查找拼接 SQL（高危）
git diff | grep -iE "\+\+.*string\s+sql\s*=.*\\\$|string\.Format.*SELECT|string\.Format.*INSERT|string\.Format.*UPDATE|string\.Format.*DELETE"
```

```bash
# 查找 .SqlQueryable 或 .Sql 的原始字符串拼接
git diff | grep -iE "\+\+.*\.Sql\s*\(\s*\\\$|\.SqlQueryable.*\\\$"
```

**判定规则：**
- ❌ 高危：`db.Sql($"SELECT * FROM X WHERE id = {id}")` — 直接拼接用户输入
- ✅ 安全：`db.Sql("SELECT * FROM X WHERE id = @id", new { id })` — 参数化查询
- ⚠️ 警告：`db.Sql($"SELECT * FROM {tableName}")` — 表名拼接（需白名单校验）

### Step 3: XSS 检查（Vue 3 前端）

```bash
# 查找 v-html（高危，可能 XSS）
git diff | grep -E "\+\+.*v-html"
```

```bash
# 查找 innerHTML 直接赋值
git diff | grep -E "\+\+.*innerHTML\s*="
```

**判定规则：**
- ❌ 高危：`<div v-html="userInput" />` — 用户输入直接渲染
- ✅ 安全：`<div v-html="sanitizedHtml" />` — 经过 DOMPurify 等库过滤
- ⚠️ 警告：`v-html="trustedContent"` — 需确认 content 来源可信

### Step 4: 敏感信息泄露检查

```bash
# 查找硬编码的密钥/密码/Token
git diff | grep -iE "\+\+.*(password|secret|apikey|api_key|token|connectionstring)\s*=\s*[\"'][^\"']{8,}"
```

```bash
# 查找提交到仓库的配置文件（应使用 .env 或 user-secrets）
git diff --name-only | grep -iE "\.env$|appsettings\.Development\.json|appsettings\.Production\.json"
```

**判定规则：**
- ❌ 高危：`Password = "abc123"` — 硬编码密码
- ❌ 高危：提交 `.env` 文件（应在 .gitignore）
- ✅ 安全：`Password = Environment.GetEnvironmentVariable("DB_PWD")` — 环境变量
- ✅ 安全：`appsettings.json` 只包含非敏感配置（连接字符串用占位符）

### Step 5: 认证与授权检查（JWT + 权限）

```bash
# 查找缺少 [Authorize] 的敏感 API
git diff --name-only | grep -E "Service\.cs$"
```

对每个变更的 Service 文件，检查：
- 涉及用户数据/订单/支付的接口是否有 `[Authorize]` 或权限标识
- 是否有 `AllowAnonymous` 滥用（应仅限登录/注册接口）
- 租户隔离：查询是否包含 `ITenantFilter`（见 CLAUDE.md R4）

```bash
# 查找可能的越权（未带租户/用户过滤的查询）
git diff | grep -E "\+\+.*ISugarQueryable|Updateable|Deleteable" | grep -v "ITenantFilter\|Where.*UserId\|Where.*TenantId"
```

### Step 6: 文件上传检查

```bash
# 查找文件上传相关代码
git diff --name-only | grep -iE "upload|file|attachment"
```

对变更的文件上传代码，检查：
- 是否校验文件类型（白名单，非黑名单）
- 是否校验文件大小
- 是否校验文件内容（Magic Number，非仅扩展名）
- 上传路径是否可遍历（`../` 攻击）

### Step 7: 输出安全审查报告

```
## 安全审查报告

### 审查范围
- 变更文件数：[N]
- 审查维度：SQL 注入 / XSS / 敏感信息 / 认证授权 / 文件上传

### 发现的问题

| # | 严重程度 | 类别 | 文件 | 行号 | 问题 | 修复建议 |
|---|---------|------|------|------|------|---------|
| 1 | 🔴 高危 | SQL 注入 | XxxService.cs | 45 | 拼接用户输入 | 改用参数化查询 |
| 2 | 🟡 警告 | XSS | xxx.vue | 12 | v-html 未过滤 | 使用 DOMPurify |

### 统计
- 🔴 高危：[N] 个
- 🟡 警告：[N] 个
- 🟢 通过：[N] 项检查

### 结论
- ❌ 禁止合并：存在高危问题，必须修复
- ⚠️ 建议修复后合并：仅有警告
- ✅ 可以合并：全部通过

### 修复指引
- [针对每个高危问题给出具体修复代码]
```

### Step 8: 记录到 memory

如果发现高危问题，将漏洞模式追加到 `.claude/memory/lessons-learned.md`，避免重复踩坑。
