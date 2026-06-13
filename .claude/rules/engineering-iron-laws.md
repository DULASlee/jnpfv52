# Engineering Iron Laws — 完整验证协议

> **触发条件**：当任务涉及后端改造、依赖注入清理、数据库访问层变更、声称完成/修复/通过时，Read 本文件。
> **主文件引用**：CLAUDE.md § Engineering Iron Laws（4 句摘要）

---

## Law 1: No Escalation（零推脱）

When encountering bugs, failures, or anomalies — regardless of whether they fall within the original task scope — NEVER evade. Fix immediately.

NEVER use "out of scope", "existing issue", "open a new issue", "should work in theory", "fix later", "edge case can wait" or any similar excuse.

---

## Law 2: Verification is Completion（验证才算完成）

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

### Gate Function — 声称任何状态之前，MUST 完成这 5 步

1. **IDENTIFY** — 什么命令能证明这个声称？
2. **RUN** — 执行完整命令（本次、实时，不是上次的结果）
3. **READ** — 读完整输出，检查 exit code，数失败数
4. **VERIFY** — 输出是否确认了你的声称？
   - 否 → 用证据说明实际状态
   - 是 → 带着证据做出声称
5. **CLAIM** — 带证据声称结果

**跳过任何一步 = 说谎，不是验证。**

### 按声称类型验证要求

| 声称 | 需要的证据 | 不够的证据 |
|---|---|---|
| 测试通过 | 测试命令输出：0 failures | 上次跑过、"应该通过" |
| 构建成功 | build 命令：exit 0 | linter 通过、日志看起来正常 |
| Bug 已修 | 复现原始症状：通过 | 改了代码就假设修好了 |
| 代码审查通过 | code-reviewer 子代理报告：0 严重 | 自己觉得没问题 |
| 子代理完成 | 检查 VCS diff + 验证变更 | 信任子代理的"成功"报告 |
| 需求已满足 | 逐项对照计划清单 | "测试通过，阶段完成" |

### 红旗词 — 说出这些词就说明你没有证据

- "should" / "probably" / "seems to" / "looks like"
- "应该可以通过" / "看起来没问题"
- 在验证之前表达满意（"Great!" / "Done!" / "完美!"）

### 合理化借口 → 真相

| 借口 | 真相 |
|---|---|
| "应该没问题了" | 跑命令，别猜 |
| "我有信心" | 信心 ≠ 证据 |
| "就这一次" | 没有例外 |
| "linter 通过了" | linter ≠ 编译器 |
| "子代理说成功了" | 独立验证 |
| "部分检查够了" | 部分证明不了任何事 |
| "我累了" | 疲惫 ≠ 免检 |

---

## Law 3: Honest Reporting（诚实报告）

If uncertain, say so — don't fabricate. NEVER make up content to appear thorough. Proactively report issues found in adjacent code.

---

## Law 4: No Shortcuts（零捷径）

NEVER write TODOs, pseudo-implementations, try-catch blocks that swallow exceptions, or random changes without analyzing root cause. NEVER skip boundary cases (null, concurrency, error paths). Three similar lines > premature abstraction.

---

## JNPF 架构铁律（补充）

### 铁律 A: 零反向污染
新编译器的产物禁止回流污染 legacy App.vue 代码。新旧编译器产物严格隔离。

### 铁律 B: 共享层不可逆
共用层（API/Store/Types）修改必须兼容 PC 和 App 两端。单向兼容，不可破坏任一端的调用。

### 铁律 C: 三层组件映射
PC(wd-)/App(wd-)/legacyApp(uni-) 三层独立，组件前缀不可混用。每层有自己的组件注册表。

### 铁律 D: Schema 门禁
所有 Schema 变更（API 入参/出参、数据库表结构、Store 状态形状）必须通过回归测试验证，不可跳过。
