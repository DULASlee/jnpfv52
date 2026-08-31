# Contract Rules — 契约规则索引

> **分类：** L0/L1 项目规则
> 
> **来源：** 多处源文件

---

## Frozen Contract 保护

**来源：** `business-first-iron-law.md`

**规则：** 不得破坏已冻结的 Public Contract、API Surface、Database Contract。

**强制要求：**
- API Freeze 时必须建立 Baseline
- 任何修改必须通过 Contract Change Request
- Breaking Change 必须 Human Gate (H3)

---

## Breaking Change 分类

| 类型 | Gate | 说明 |
|------|------|------|
| Public API Breaking | H3 | 修改接口签名 |
| Database Contract Breaking | H3 | Schema 破坏性变更 |
| Protocol Breaking | H3 | 协议版本不兼容 |
| Frozen Contract Violation | H3 | 修改已冻结的契约 |

---

## API Freeze 规则

**来源：** `architecture-redlines.md`

**强制要求：**

### API Baseline 建立

1. **Baseline 建立：**
```yaml
apiBaseline:
  version: "1.0"
  frozenAt: "2026-08-31"
  surface:
    - path: /api/studio/ir
      methods: [GET, POST]
    - path: /api/oauth
      methods: [POST]
```

2. **Structural Diff 检查：**
```yaml
apiDiff:
  positive: "正向测试通过"
  negative: "负向测试通过"
  recovery: "回滚测试通过"
```

---

## Contract 测试矩阵

| 测试类型 | 说明 | 必须 |
|---------|------|------|
| Positive Test | 正常流程 | ✅ |
| Negative Test | 异常输入 | ✅ |
| Boundary Test | 边界条件 | ✅ |
| Recovery Test | 回滚能力 | ✅ |
| Concurrency Test | 并发场景 | 建议 |

---

## Triple-Key 契约

**来源：** `triple-key-iron-law.md`

**规则：** 所有数据实体必须携带 tenantId/projectId/pipelineId。

**强制要求：**
- DB Schema 必须包含三元组字段
- IR 投影查询 WHERE 必须含三元组
- 文件路径必须四层：{tenantId}/{projectId}/{pipelineId}/

---

## 关联文档

- `.claude/rules/business-first-iron-law.md` — Business First
- `.claude/rules/triple-key-iron-law.md` — Triple-Key
- `.claude/rules/architecture-redlines.md` — Architecture Redlines
