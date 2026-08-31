# Full Dry Run

## 目的
验证 Control Plane 能够自主驱动开发。

## 5 类任务 Dry Run

### Scenario 1: new-feature / common / low

期望：
- autonomous: true
- human_gates: 0

### Scenario 2: runtime-change / section-8 / high

期望：
- architecture analysis
- adversarial review
- contract verification
- appropriate Gate

### Scenario 3: bug-fix / critical

期望：
- root-cause
- regression test
- evidence
- verification

### Scenario 4: api-change / breaking

期望：
- API analysis
- contract verification
- H3: PAUSE

### Scenario 5: refactor / section-8 / high

期望：
- characterization / contract protection
- adversarial review
- regression
- API verification

## 验证标准
- 所有 5 类任务走通
- 无人工干预次数 ≤ 预期
- Evidence Chain 完整
- Phase State 正确更新
- Skill Routing 准确
