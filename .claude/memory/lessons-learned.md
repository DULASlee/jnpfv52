# 踩坑记录与最佳实践

> 团队共享，提交到 Git。避免同一个坑踩两次。

---

## 2026-06-05 | dotnet build 的 NU5026 pack 错误

**场景**：hook 中执行 `dotnet build --no-restore` 触发 NU5026
**后果**：编译失败，hook 误判为代码错误
**教训**：
- `--no-restore` 跳过包恢复但不跳过 pack，XML 文档文件缺失会报 NU5026
- 解决方案：加 `-p:IsPackable=false` 跳过 pack 步骤
- 参考：`.claude/hooks/guard-finish.mjs`

---

## 2026-06-05 | Windows 下 execSync 的 ETIMEDOUT 与 DLL 锁定

**场景**：后端服务运行时，hook 中 dotnet build 因 DLL 锁定阻塞直至超时
**后果**：60 秒超时后报 ETIMEDOUT，hook 误判为编译失败
**教训**：
- DLL 锁定 ≠ 代码错误，应降级为警告
- 超时应设为 30 秒（增量编译正常 5-15 秒，超过 30 秒大概率是锁定）
- 在 catch 中同时检测 `'is being used by another process'` 和 `ETIMEDOUT`

---

## 2026-06-05 | Node.js hook 脚本 Windows 兼容性

**场景**：`readFileSync('/dev/stdin')` 在 Windows 下不工作
**后果**：hook 无法读取 Claude Code 传入的 stdin JSON
**教训**：
- 必须用异步方式读取 stdin：`for await (const chunk of process.stdin)`
- 不能用 `readFileSync('/dev/stdin')`，Windows 没有 `/dev/stdin`
- `execSync` 的 `stdio: ['ignore', 'ignore', 'pipe']` 可捕获 stderr

---

## 2026-06-05 | npm view 在国内网络可能挂起

**场景**：guard-deps hook 中执行 `npm view lodash version` 查询版本
**后果**：某些包在国内网络下挂起 30-120 秒，阻塞 AI 工作流
**教训**：
- 必须设置 3 秒熔断：`execSync(..., { timeout: 3000 })`
- 查询失败时降级为跳过校验，不阻断

---

## 2026-06-05 | Hook 测试命令被自身 Hook 拦截

**场景**：用 Bash 工具测试 guard-bash hook，测试命令字符串中包含 `rmdir /s /q` 和 `npm install`
**后果**：guard-bash 和 guard-deps hook 拦截了测试命令本身
**教训**：
- 测试危险命令拦截 hook 时，必须将测试脚本写入文件再执行
- `node test-file.js` 命令不包含危险模式，不会被拦截
- 测试数据中的危险模式会被 hook 的 stdin 解析正确拦截（这是预期行为）

---

## 2026-06-05 | 多子项目结构下 node_modules 路径

**场景**：format-and-lint hook 硬编码 `./node_modules/.bin/`
**后果**：根目录无 node_modules，prettier/eslint 找不到
**教训**：
- JNPF 是多子项目结构（jnpf-web-vue3、jnpf-app-vue3 各有独立 node_modules）
- 必须从被编辑文件路径向上动态查找最近的 node_modules
- `findProjectRoot()` 函数实现：从文件路径向上遍历，检查 `node_modules/.bin` 是否存在

## 2026-09-04 P13-H Hardening lessons
- Roslyn 4.8 API name is PreprocessorSymbolNames, not PreprocessorSymbols (verified via NuGet assembly reflection). Never assume API names; probe the exact package version.
- MSBuildWorkspace trees bake #if branches at parse time: reusing trees in an Adhoc compilation keeps DEBUG active. Re-parse source texts with clean CSharpParseOptions to test absence.
- Roslyn ToDisplayString defaults to keyword form (int/string), not metadata form. Assert what Roslyn reports, not what reads better.
- dotnet test --no-build runs the STALE dll after test-only edits; always rebuild before trusting --no-build results.

## 2026-09-04 P13 隔离审查课
- Symbol==null 的单候选绝不能报 Resolved：Type.Member 实例成员访问在 Roslyn 里本来就不绑定（非值上下文），必须走接收者验证+显式理由，否则就是 First() 后门。
- 工厂方法吞 reason（ResolvedResult 硬编码 OK）是同类诚实 bug：结果对象的理由字段必须端到端可追踪。
- 昂贵操作（Compilation.Emit）进解析路径必须配缓存（ConditionalWeakTable 按 Compilation 键），否则每个表达式查询都是秒级。
