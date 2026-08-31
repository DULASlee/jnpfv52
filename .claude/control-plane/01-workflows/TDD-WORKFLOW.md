# TDD Workflow — 测试驱动开发工作流

> **版本：** v1.1
> 
> **生效日期：** 2026-08-31

---

## TDD 双 Profile

测试 Profile 由 **Phase Contract** 显式指定：

```yaml
phaseContract:
  testingProfile: STRICT-TDD  # 或 CONTRACT-FIRST-TDD
```

**禁止自行规定项目全部使用某一个 Profile。**

---

## Profile A: STRICT-TDD

### 适用场景

- 核心算法实现
- 关键业务逻辑
- 状态机
- 生命周期
- 高风险行为

### 流程

```
RED
 ↓
Write failing test
 ↓
GREEN
 ↓
Minimal implementation
 ↓
REFACTOR
 ↓
REGRESSION
```

### 详细步骤

#### RED 阶段

1. **Write failing test**
   - 编写一个会失败的测试
   - 测试应该描述期望的行为
   - 不要写实现代码

2. **Run test, verify it fails**
   - 运行测试
   - 确认测试失败
   - 失败原因应该是"功能未实现"，不是"测试写错了"

#### GREEN 阶段

3. **Write minimal implementation**
   - 编写最小实现让测试通过
   - 不要过度设计
   - 不要写"将来可能用到"的代码

4. **Run test, verify it passes**
   - 运行测试
   - 确认测试通过

#### REFACTOR 阶段

5. **REFACTOR**
   - 重构代码，提升质量
   - 保持测试通过
   - 消除重复代码
   - 改善命名

6. **REGRESSION**
   - 运行完整测试套件
   - 确保没有破坏其他功能

### 禁止行为

```
❌ 跳过 RED 直接写实现
❌ 弱化 assertion
❌ skip failing test
❌ 写"将来可能用到"的代码
❌ 过度设计
```

---

## Profile B: CONTRACT-FIRST-TDD

### 适用场景

- 复杂系统集成
- 已有 Contract 扩展
- 大型 Phase
- 跨模块变化

### 流程

```
Contract
 ↓
Test Matrix
 ↓
Implementation
 ↓
Verification
 ↓
Regression
```

### 详细步骤

#### Contract 阶段

1. **Define Contract**
   - 明确接口契约
   - 定义输入/输出
   - 定义边界条件
   - 定义错误处理

#### Test Matrix 阶段

2. **Build Test Matrix**
   - 设计测试用例矩阵
   - 覆盖正常流程
   - 覆盖异常流程
   - 覆盖边界条件

#### Implementation 阶段

3. **Implementation**
   - 根据 Contract 和 Test Matrix 实现
   - 遵守 CONTRACT-FIRST-TDD 原则

#### Verification 阶段

4. **Verify against Contract**
   - 运行所有测试
   - 验证契约满足

#### Regression 阶段

5. **REGRESSION**
   - 运行完整测试套件
   - 确保没有破坏其他功能

### 保留的设计

CONTRACT-FIRST-TDD 保留以下设计：

```
- Negative Test Design（负向测试设计）
- Concurrency Test Design（并发测试设计）
- Failure Test Design（故障测试设计）
```

---

## 测试矩阵

### 必须包含的测试类型

| 类型 | 说明 | STRICT | CONTRACT |
|------|------|--------|----------|
| Unit Tests | 单元测试 | ✅ | ✅ |
| Contract Tests | 契约测试 | ✅ | ✅ |
| State / Lifecycle Tests | 状态/生命周期测试 | ✅ | ✅ |
| Integration Tests | 集成测试 | ✅ | ✅ |
| Concurrency Tests | 并发测试 | 建议 | ✅ |
| Failure Tests | 故障测试 | ✅ | ✅ |
| Regression Tests | 回归测试 | ✅ | ✅ |
| Boundary / Isolation Tests | 边界测试 | ✅ | ✅ |
| Negative Tests | 负向测试 | ✅ | ✅ |
| API Surface Tests | API 表面测试 | ✅ | ✅ |

---

## Negative Testing

主动制造错误：

```
非法状态
非法依赖
错误参数
错误生命周期
并发冲突
异常执行
越权 API
错误 Capability Injection
```

**确保系统不仅：**

```
正确时 PASS
```

**还要：**

```
错误时 FAIL correctly
```

---

## 测试诚信

### 五禁令（来自 implementation-integrity-iron-law.md）

1. **禁止给门控开逃逸通道**
2. **禁止为唯一解析器引入第二源**
3. **禁止改测试断言凑新行为**
4. **禁止用快照重生成替代内容审查**
5. **禁止跳过验收标准的核心项**

### 测试失败时

**问：**
- "我的实现哪里不对？"
- "测试验证的意图是什么？"

**不是问：**
- "我怎么改实现让它过？"
- "我怎么改测试让它过？"

---

## Evidence 要求

每个测试必须提供：

```
1. 测试目的
2. 输入
3. 期望输出
4. 实际输出
5. 通过/失败
6. 证据（截图/log）
```

---

## 关联文档

- `AUTONOMOUS-MULTI-PHASE-ENGINEERING-WORKFLOW.md` — 主工作流
- `PHASE-EXECUTION-PROTOCOL.md` — Phase 执行协议
- `VERIFICATION-WORKFLOW.md` — 验证工作流
- `02-rules/TESTING-RULES.md` — 测试规则
- `04-templates/TEST-MATRIX.md` — 测试矩阵模板
