# Evidence Record Template

> **目的：** 定义 Evidence Record 的标准模板

---

```yaml
evidenceRecord:
  id: "EVID-[Phase]-[N]"
  phase: "Phase X-Y"
  createdAt: "YYYY-MM-DD"
  
  # Evidence Chain
  chain:
    requirement:
      id: "REQ-XXX"
      source: "phase-contract"
      status: "APPROVED"
      evidence:
        - type: "document"
          path: "docs/requirements/req-xxx.md"
        - type: "screenshot"
          path: "evidence/req-xxx.png"
          
    design:
      id: "SPEC-XXX"
      source: "design-spec.md"
      status: "APPROVED"
      evidence:
        - type: "document"
          path: "docs/design/spec-xxx.md"
        - type: "diagram"
          path: "docs/design/spec-xxx-diagram.png"
          
    implementation:
      files:
        - path: "src/Module/Component.cs"
          lines: "100-200"
          changes: "Added new feature"
      evidence:
        - type: "screenshot"
          path: "evidence/impl-1.png"
          description: "Code implementation"
          
    tests:
      unit:
        count: 45
        passRate: "100%"
        evidence:
          - type: "report"
            path: "tests/unit/report.html"
      contract:
        count: 12
        passRate: "100%"
      negative:
        count: 8
        passRate: "100%"
      concurrency:
        count: 5
        passRate: "100%"
        
    verification:
      build:
        status: "PASS"
        evidence:
          - type: "log"
            path: "logs/build.log"
      unit_tests:
        status: "PASS"
        evidence:
          - type: "report"
            path: "tests/unit/report.html"
      integration_tests:
        status: "PASS"
        evidence:
          - type: "report"
            path: "tests/integration/report.html"
      api_diff:
        status: "PASS"
        evidence:
          - type: "diff"
            path: "evidence/api-diff.json"
      architecture_check:
        status: "PASS"
        evidence:
          - type: "report"
            path: "evidence/arch-check.html"
```

---

## Evidence 类型

| 类型 | 说明 | 示例 |
|------|------|------|
| document | 文档 | 设计规格、需求文档 |
| screenshot | 截图 | UI、测试结果 |
| log | 日志 | Build log、测试 log |
| report | 报告 | 测试报告、覆盖率报告 |
| diff | 差异 | API diff、代码 diff |
| diagram | 图表 | 架构图、流程图 |

---

## 完整性检查

- [ ] Requirement 有证据
- [ ] Design 有证据
- [ ] Implementation 有证据
- [ ] Tests 有证据
- [ ] Verification 有证据
- [ ] 无断链

---

## Signatures

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Engineer | | | |
| Reviewer | | | |
