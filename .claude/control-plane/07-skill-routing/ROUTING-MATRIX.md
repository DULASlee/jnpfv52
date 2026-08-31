# Skill Routing Matrix

## 任务类型 → 技能自动加载

| 任务类型 | Section | Risk | 自动加载 Skills |
|-----------|---------|------|----------------|
| new-feature | common | low | phase-management, evidence-collection |
| new-feature | common | high | requirement-analysis, architecture-analysis, design-specification, implementation-planning, adversarial-review |
| new-feature | section-8 | any | requirement-analysis, architecture-analysis, design-specification, implementation-planning, adversarial-review, contract-governance |
| runtime-change | any | any | architecture-analysis, contract-governance, architecture-gate |
| api-change | any | any | contract-governance, completion-verification |
| bug-fix | any | critical | self-repair, evidence-collection, completion-verification |
| bug-fix | any | low | evidence-collection |
| refactor | any | high | adversarial-review, contract-governance |
| phase-close | any | any | completion-verification |

## Skill Registry

### Engineering Control Skills
- orchestration
- phase-management
- contract-governance
- architecture-gate
- adversarial-review
- self-repair
- evidence-collection
- completion-verification

### Project Skills (复用)
- requirement-analysis (.agents/skills)
- architecture-analysis (.agents/skills)
- coding (.agents/skills)
- tdd (.agents/skills)
