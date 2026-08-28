# V6 Evaluation Strategy — 能力型 Evaluation Cases

> 不以"审了多少 JNPF 类"为指标，而以"能否正确解决特定类型的问题"为指标。

## 1. 核心原则

### 能力型 Evaluation，非覆盖型 Evaluation

**错误指标**：审了 100 个类，发现 50 个 Finding
**正确指标**：给定 10 个特定场景，能否正确判定 GO/STOP/NEED

### Evaluation Cases 设计原则

1. **覆盖 v6 新增能力**（跨类上下文/Context Expansion/Level 0/1/2）
2. **覆盖 v4 已解决能力**（回归测试，确保 v6 不弱化 v4）
3. **覆盖 NEED EVIDENCE 场景**（确保 v6 正确冻结，不强行 GO）
4. **覆盖 STOP 场景**（确保 v6 正确拒绝，不强行 GO）

---

## 2. Evaluation Cases 清单

### 2.1 v4 已解决能力（回归测试）

| # | 场景 | 技术性质 | 预期判定 | 证据来源 |
|---|------|----------|----------|----------|
| E01 | 单类异常处理（catch 丢栈） | Exception Semantics | GO | Golden #1 EmailService |
| E02 | 单类资源释放（FileStream 未 using） | Resource Lifetime | GO | Golden #2 FileService Upload |
| E03 | 单类资源释放（FileStream.Close） | Resource Lifetime | GO | Golden #3 FileService FileDown |
| E04 | 单类事务边界（多步 DB 无事务） | Business Transaction | GO | Golden #4 OrderService |
| E05 | 单类 N+1 形态（foreach 内查询） | Performance | NEED EVIDENCE | 需运行时证据 |

### 2.2 v6 新增能力（跨类上下文）

| # | 场景 | 技术性质 | 预期判定 | 证据来源 |
|---|------|----------|----------|----------|
| E06 | 跨类 ownership（A 调 B，B 返回 IDisposable） | Resource Lifetime | STOP/GO | 需跨类上下文 |
| E07 | 跨类 DI 生命周期（Singleton 注 Scoped） | Architecture | STOP/NEED | 需跨类上下文 |
| E08 | 跨类数据量传播（B 返回全量，A 有无截断） | Performance | NEED EVIDENCE | 需跨类上下文 |
| E09 | 跨类事务边界（Service 调 Repository） | Business Transaction | STOP/GO | 需跨类上下文 |
| E10 | 跨层 ownership（Service → Controller → 前端） | Resource Lifetime | STOP | 需跨层上下文 |

### 2.3 Context Expansion 场景

| # | 场景 | 技术性质 | 预期判定 | Context Expansion 路径 |
|---|------|----------|----------|------------------------|
| E11 | STOP → Context Expansion → GO | UnitOfWork | GO | Level 1（DI 注册） |
| E12 | STOP → Context Expansion → NEED EVIDENCE | N+1 | NEED EVIDENCE | Level 2（call-graph，未实现） |
| E13 | STOP → Context Expansion → STOP | Ownership | STOP | Level 0（人工描述） |

### 2.4 边界场景

| # | 场景 | 技术性质 | 预期判定 | 说明 |
|---|------|----------|----------|------|
| E14 | 无法证明的 Finding | 任意 | NEED EVIDENCE | 不强行 GO |
| E15 | 不安全局部修复 | 任意 | STOP | 不强行 GO |
| E16 | 跨模块传染 | 任意 | STOP | 不强行 GO |
| E17 | False Positive | 任意 | NO FINDING | 不误判 |

---

## 3. Evaluation Corpus

### 3.1 候选材料来源

- **JNPF Phase 2 现有结果**：FileService / OrderService / ScheduleService / EmailService 等
- **人工构造场景**：针对特定技术性质构造最小场景
- **历史 Golden Examples**：v4 的 4 个 Golden 作为回归测试

### 3.2 Corpus 组织

```
evaluation-corpus/
├── v4-regression/          # v4 已解决能力（回归测试）
│   ├── E01-exception/
│   ├── E02-resource-upload/
│   ├── E03-resource-download/
│   └── E04-transaction/
├── v6-cross-class/         # v6 新增能力（跨类上下文）
│   ├── E06-ownership/
│   ├── E07-di-lifetime/
│   └── E08-data-volume/
├── v6-context-expansion/   # Context Expansion 场景
│   ├── E11-stop-to-go/
│   ├── E12-stop-to-need/
│   └── E13-stop-to-stop/
└── v6-boundary/            # 边界场景
    ├── E14-need-evidence/
    ├── E15-stop/
    └── E16-cross-module/
```

---

## 4. Evaluation Metrics

### 4.1 能力型指标

| 指标 | 说明 | 目标 |
|------|------|------|
| **Correct Decision Rate** | 正确判定率（GO/STOP/NEED 判定正确） | ≥ 90% |
| **False Positive Rate** | 误判率（把 NO FINDING 判为 Finding） | ≤ 5% |
| **False Negative Rate** | 漏判率（把 Finding 判为 NO FINDING） | ≤ 5% |
| **Context Expansion Accuracy** | Context Expansion 后判定正确率 | ≥ 85% |
| **Convergence Rate** | 正确收敛率（不该继续时停止） | ≥ 95% |

### 4.2 非指标

| 非指标 | 说明 |
|--------|------|
| ❌ 审了多少类 | 不衡量覆盖度 |
| ❌ 发现多少 Finding | 不衡量发现能力 |
| ❌ 修复多少 Finding | 不衡量修复能力 |

---

## 5. Evaluation Process

### 5.1 单场景评估流程

```
给定场景（代码 + 上下文）
    ↓
Skill 执行诊断
    ↓
输出 Finding + 判定（GO/STOP/NEED）
    ↓
与预期判定对比
    ↓
记录：正确 / 错误 / 部分正确
```

### 5.2 整体评估流程

```
执行所有 Evaluation Cases（E01-E17）
    ↓
统计 Correct Decision Rate / False Positive Rate / ...
    ↓
与目标对比
    ↓
输出评估报告
```

---

## 6. Evaluation 与 Golden Examples 的关系

- **Golden Examples = Evaluation Cases 的子集**（v4 的 4 个 Golden = E01-E04）
- **v6 需新增跨类 Golden Examples**（E06-E10 的候选）
- **Golden Examples 用于验证 Skill 决策质量**，不是用于凑数

---

## 7. Evaluation 与 JNPF Phase 2 的关系

- **JNPF Phase 2 现有结果可作为 Evaluation Corpus 的候选材料**
- **本轮不重新执行 JNPF Phase 2**
- **未来 v6 完成后，可用 Phase 2 结果作为回归测试**

---

## 8. 总结

V6 Evaluation Strategy = **能力型 Evaluation Cases，非覆盖型**

- 覆盖 v4 已解决能力（回归测试）
- 覆盖 v6 新增能力（跨类上下文）
- 覆盖 Context Expansion 场景
- 覆盖边界场景（NEED EVIDENCE / STOP / False Positive）
- 指标：Correct Decision Rate / False Positive Rate / Context Expansion Accuracy
- 非指标：审了多少类 / 发现多少 Finding
