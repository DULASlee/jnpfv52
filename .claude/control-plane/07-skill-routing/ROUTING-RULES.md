# Skill Routing Rules

## 路由规则

### 1. 多维度匹配
- taskType: 任务类型
- section: Section (section-8, section-9, common, etc.)
- phase: Phase (P0-P12)
- riskLevel: 风险等级 (low, medium, high, critical)
- contractImpact: 契约影响 (none, additive, breaking)

### 2. Skill 来源
- engineering-control/*: Control Plane Skills
- project/*: 现有 Project Skills
- superpowers/*: Superpowers Skills

### 3. 加载规则
- required: 必须加载
- recommended: 推荐加载
- gates: 必须的 Human Gates
- testingProfile: TDD Profile

### 4. Human Gate 映射
- H1: 架构冲突 → architecture-gate
- H2: 需求冲突 → 需求分析 Skill
- H3: Breaking Change → contract-governance
- H4: 跨 Section → architecture-gate
- H5: 安全/数据风险 → self-repair + evidence-collection

### 5. TDD Profile 映射
- STRICT-TDD: 核心算法、状态机、高风险
- CONTRACT-FIRST-TDD: 复杂集成、大型 Phase

## 使用流程

1. 识别任务类型
2. 提取维度
3. 匹配规则
4. 加载 Skills
5. 确定 Testing Profile
6. 确定 Human Gates
