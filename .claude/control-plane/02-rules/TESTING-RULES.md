# Testing Rules — 测试规则索引

> **分类：** L1 项目规则
> 
> **来源：** 多处源文件

---

## 测试分层

**来源：** `testing-toolchain.md`

| 层级 | 工具 | 用途 |
|------|------|------|
| L1 组件 | xUnit/Vitest | 快速单元测试 |
| L2 轨迹 | 集成测试 | 模块间协作 |
| L3 任务 | E2E | 确定性验证 |
| L4 业务 | LLM Judge | 业务价值评估 |

---

## TDD 双 Profile

### STRICT-TDD

**适用场景：**
- 核心算法实现
- 关键业务逻辑
- 状态机/生命周期
- 高风险行为

**流程：**
```
RED → GREEN → REFACTOR → REGRESSION
```

**禁止：**
- 跳过 RED 直接写实现
- 弱化 assertion
- skip failing test

---

### CONTRACT-FIRST-TDD

**适用场景：**
- 复杂系统集成
- 已有契约扩展
- 大型 Phase
- 跨模块变化

**流程：**
```
Contract/Test Matrix → Implementation → Verification → Regression
```

**保留：**
- Negative Test Design
- Concurrency Test Design
- Failure Test Design

---

## 由 Phase Contract 决定

测试 Profile 由 Phase Contract 显式指定：

```yaml
phaseContract:
  testingProfile: STRICT-TDD  # 或 CONTRACT-FIRST-TDD
```

---

## 测试纪律

**来源：** `implementation-integrity-iron-law.md`

### 五禁令

| 禁令 | 说明 |
|------|------|
| 门控逃逸 | 门控设计意图不可被绕过 |
| 唯一源破坏 | 不得引入第二源 |
| 修改断言 | 测试失败先查实现，非先改测试 |
| 快照重生成 | 内容审查优先于哈希稳定 |
| 跳过验收 | 逐条验收标准必须有证据 |

---

## 测试覆盖要求

| 类型 | 说明 | 必须 |
|------|------|------|
| Unit Tests | 单元测试 | ✅ |
| Contract Tests | 契约测试 | ✅ |
| State/Lifecycle Tests | 状态/生命周期测试 | ✅ |
| Integration Tests | 集成测试 | ✅ |
| Concurrency Tests | 并发测试 | 建议 |
| Failure Tests | 故障测试 | ✅ |
| Regression Tests | 回归测试 | ✅ |
| Boundary/Isolation Tests | 边界测试 | ✅ |
| Negative Tests | 负向测试 | ✅ |
| API Surface Tests | API 表面测试 | ✅ |

---

## Evidence 要求

**规则：** 验证证据优先于"看起来正确"。

**强制要求：**
- Build result
- Test result
- API diff
- Architecture check
- Files changed

---

## 关联文档

- `.claude/rules/testing-toolchain.md` — 测试工具链
- `.claude/rules/testing.md` — 测试纪律
- `.claude/rules/implementation-integrity-iron-law.md` — 实现完整性
- `01-workflows/TDD-WORKFLOW.md` — TDD 工作流
