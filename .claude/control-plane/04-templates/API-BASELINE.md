# API Baseline Template

> **目的：** 定义 API Baseline 的标准模板

---

```yaml
apiBaseline:
  version: "1.0"
  frozenAt: "YYYY-MM-DD"
  createdBy: "chief-architect"
  status: "FROZEN"  # DRAFT / REVIEW / FROZEN / DEPRECATED
  
  # API Surface
  surface:
    - path: "/api/studio/ir"
      methods:
        - GET
        - POST
      contracts:
        - name: "StudioIrContract"
          frozen: true
          
    - path: "/api/oauth"
      methods:
        - POST
      contracts:
        - name: "OAuthContract"
          frozen: true
          
  # Data Contracts
  dataContracts:
    - name: "PipelineEntity"
      frozen: true
      fields:
        - name: "F_ID"
          type: "long"
          required: true
        - name: "F_TENANT_ID"
          type: "string"
          required: true
          
  # Breaking Change History
  breakingChanges: []
```

---

## 使用说明

1. API Baseline 是 API Surface 的快照
2. 任何 API 修改必须对比 Baseline
3. Breaking Change 必须 Human Gate (H3)
4. Frozen 状态需要 Chief Architect 批准

---

## API Surface 完整性检查

### Positive Tests

```yaml
positiveTests:
  - endpoint: "/api/studio/ir"
    method: GET
    expectedStatus: 200
  - endpoint: "/api/oauth"
    method: POST
    expectedStatus: 200
```

### Negative Tests

```yaml
negativeTests:
  - endpoint: "/api/studio/ir/{invalid}"
    method: GET
    expectedStatus: 404
  - endpoint: "/api/oauth"
    method: POST
    payload: {}
    expectedStatus: 600  # JWT expired
```

### Recovery Tests

```yaml
recoveryTests:
  - description: "API change can rollback"
    test: "Verify old behavior"
  - description: "Breaking change triggers H3"
    test: "Verify gate"
```

---

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| API Owner | | | |
| Chief Architect | | | |
