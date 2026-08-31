# Design Specification Template

> **目的：** 定义设计规格文档的标准模板

---

```markdown
# Design Specification: [Feature Name]

## Metadata

```yaml
spec:
  id: "SPEC-[Phase]-[N]"
  phase: "Phase X-Y"
  status: "DRAFT"  # DRAFT / REVIEW / APPROVED
  version: "1.0"
  createdAt: "YYYY-MM-DD"
  updatedAt: "YYYY-MM-DD"
```

---

## 1. Objective

本设计要解决什么问题。

## 2. Context

### 背景

### 相关系统

### 约束条件

## 3. Problem Statement

### 问题描述

### 影响

### 机会

## 4. Scope

### 包含

- [ ] Item 1
- [ ] Item 2

### 不包含

- Item 1
- Item 2

## 5. Non-Scope

明确不属于本设计的范围。

## 6. Architecture Position

### 在整体架构中的位置

```
[Layer Diagram]
```

### 与其他组件的关系

## 7. Component Model

### 组件列表

| Component | Responsibility | Public API |
|-----------|---------------|------------|
| Component1 | Responsibility1 | API1 |
| Component2 | Responsibility2 | API2 |

### 组件交互图

```
[Interaction Diagram]
```

## 8. Public Contract

### 接口定义

#### API 1

```yaml
endpoint: /api/xxx
method: GET
request:
  - name: param1
    type: string
    required: true
response:
  - name: result
    type: object
```

### 数据契约

#### Request DTO

```csharp
public class XxxRequest
{
    public string Param1 { get; set; }
}
```

#### Response DTO

```csharp
public class XxxResponse
{
    public string Result { get; set; }
}
```

## 9. Internal Contract

内部组件之间的契约。

## 10. Data Model

### 数据库 Schema

```sql
CREATE TABLE [TableName] (
    [Column1] [Type] NOT NULL,
    [Column2] [Type] NULL
);
```

### 实体模型

```csharp
public class XxxEntity
{
    public string Column1 { get; set; }
    public string Column2 { get; set; }
}
```

## 11. State Model

### 状态机

```
[State Diagram]
```

### 状态转换表

| Current State | Event | Next State | Action |
|---------------|-------|------------|--------|
| State1 | Event1 | State2 | Action1 |

## 12. Lifecycle

### 对象生命周期

```
[Lifecycle Diagram]
```

## 13. Failure Model

### 故障场景

| Scenario | Cause | Effect | Handling |
|-----------|-------|--------|----------|
| Scenario1 | Cause1 | Effect1 | Handling1 |

### 错误码

| Code | Message | HTTP Status |
|------|---------|-------------|
| ERR001 | Error message 1 | 400 |
| ERR002 | Error message 2 | 500 |

## 14. Concurrency Model

### 并发策略

### 线程安全考虑

## 15. Error Handling

### 异常处理策略

### 日志要求

## 16. Observability

### 监控指标

### 日志事件

## 17. Compatibility

### 向后兼容

### 版本策略

## 18. Security Boundary

### 权限要求

### 数据安全

## 19. Extensibility

### 扩展点

### 插件机制（如有）

## 20. Deferred Decisions

| Decision | Reason | Target |
|----------|--------|--------|
| Decision1 | Reason1 | Phase X |

## 21. Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Risk1 | High | Medium | Mitigation1 |

## 22. Acceptance Criteria

| # | Criteria | Test Case | Status |
|---|----------|-----------|--------|
| 1 | Criteria1 | TC-001 | DONE |
| 2 | Criteria2 | TC-002 | TODO |

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Designer | | | |
| Reviewer | | | |
| Approver | | | |
