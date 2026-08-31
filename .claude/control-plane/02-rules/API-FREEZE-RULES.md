# API Freeze Rules — API 冻结规则索引

> **分类：** L0/L1 项目规则
> 
> **来源：** `architecture-redlines.md`

---

## API Freeze 原则

**来源：** `business-first-iron-law.md` + `implementation-integrity-iron-law.md`

**规则：** 不得破坏已冻结的 Public Contract。

---

## API Surface 完整性

### Baseline 建立

```yaml
apiBaseline:
  version: "1.0"
  frozenAt: "2026-08-31"
  createdBy: "chief-architect"
  
  surface:
    controllers: []
    endpoints: []
    contracts:
      - name: "StudioIrContract"
        frozen: true
      - name: "OAuthContract"
        frozen: true
```

---

## API Surface 检查

### Positive Test

```yaml
positive:
  - "GET /api/studio/ir → 200"
  - "POST /api/oauth/login → 200 + token"
```

### Negative Test

```yaml
negative:
  - "GET /api/studio/ir/999 → 404"
  - "POST /api/oauth/login (invalid) → 600 JWT expired"
```

### Recovery Test

```yaml
recovery:
  - "API change → can rollback"
  - "Breaking change → triggers H3"
```

---

## Breaking Change 处理

| 类型 | Gate | 处理 |
|------|------|------|
| Public API Breaking | H3 | PAUSE + Change Request |
| Database Contract Breaking | H3 | PAUSE + Change Request |
| Protocol Breaking | H3 | PAUSE + Change Request |

---

## Hook 覆盖

| 检查 | Hook | 状态 |
|------|------|------|
| API Permission | `guard-auth.mjs` | ✅ |
| API Surface Diff | - | 手动 |

---

## 关联文档

- `.claude/rules/architecture-redlines.md` — Architecture Redlines
- `.claude/rules/business-first-iron-law.md` — Business First
- `.claude/rules/implementation-integrity-iron-law.md` — Implementation Integrity
