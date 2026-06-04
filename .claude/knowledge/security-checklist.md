# JNPF 安全编程检查清单

## SQL 注入
- SqlSugar MUST 参数化查询，NEVER 拼接 SQL
- 输入参数 MUST 类型验证
- 存储过程 MUST 参数绑定

## XSS
- 前端渲染用 v-text 或 {{ }}（自动转义）
- 禁止 v-html 直接渲染用户内容
- API 响应 HTML 内容 MUST 后端编码

## 认证授权
- API 路由 MUST 有 [Authorize]（除非明确 [AllowAnonymous]）
- JWT 过期 ≤ 24h
- Refresh Token MUST 滑动过期
- 多租户查询 MUST 包含 ITenantFilter

## 敏感数据
- 密码 MUST BCrypt/Argon2 哈希
- API 响应 NEVER 含密码/盐值/内部 ID
- 日志 NEVER 记录手机号/身份证/银行卡
- .env NEVER 提交 Git

## 依赖安全
- npm audit moderate+ 漏洞 MUST 修复
- 禁止无维护的依赖
- 新增依赖 MUST 确认 license 兼容性

## 输入验证
- 后端 MUST 二次验证（前端验证可绕过）
- 文件上传 MUST 验证类型/大小/内容
- 批量操作 MUST 限制数量上限
