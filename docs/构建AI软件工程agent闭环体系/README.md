# docs/构建AI软件工程agent闭环体系/ 目录说明

本目录存放**跨 harness 的工程沉淀文档**——区别于：

- `docs/architecture/`：架构设计
- `docs/superpowers/`：brainstorming/specs/plans
- `docs/knowledge-graph/`：知识图谱
- `docs/toolchain/`：工具链文档

`docs/构建AI软件工程agent闭环体系/` 专门承载：

1. **HOW 实施手册**（HOW to do）：把抽象设计落到操作步骤
2. **增量更新文档**：可随实施进展追加章节
3. **跨项目复用**：一次撰写，多处参考

## 当前文件

| 文件 | 用途 | 状态 |
|------|------|------|
| `2026-08-30-类级重构专家Agent封装实施计划.md` | 类级重构专家 Agent 从 v6.0 Skill 到 Universal Agent 的封装过程 | ✅ v1.0（2026-08-30）|
| `2026-08-30-类级重构专家Agent封装实施计划-CHANGELOG.md` | 上述手册的增量变更记录 | ✅ v1.0 |
| `类级重构专家Agent封装实现要求.md` | **UEEA Agent Runtime 实现铁律**（IRON-01 ~ IRON-12）— 防止 MVP / 重构过程将 Agent Runtime 退化为 Workflow / Prompt Chain | ✅ v1.0（2026-08-30）|
| `README.md` | 本目录索引 | ✅ v1.0 |

## 命名约定

- **中文文件名**：让中文使用者快速理解主题
- **kebab-case 不使用**：保留中文可读性
- **CHANGELOG 同名加后缀**：便于查找

## 增量更新约定

详见各文档的"增量更新约定"章节。统一原则：

1. **追加式**：只追加新章节，不修改既有编号
2. **CHANGELOG 强制**：每次更新必须记 CHANGELOG
3. **baseline 与本文档分离**：决策记录在 `docs/superpowers/specs/`，本文档承载 HOW

## 后续规划

- 添加更多"实施手册类"文档（按需）
- 与 `docs/architecture/` 保持互引
- 不与 `docs/superpowers/specs/` 决策记录重叠

## 文档层级

| 文档 | 角色 | 优先级 |
|------|------|--------|
| `类级重构专家Agent封装实现要求.md` | **最高约束**（Iron Laws）| Phase 1-5 全部 Runtime 实现必读 |
| `2026-08-30-类级重构专家Agent封装实施计划.md` | Phase 0 实施指南 | Phase 0 适用 |
| （未来）`UEEA-Runtime-MVP-实施手册.md` | Phase 1 实施指南 | Phase 1 适用 |
| （未来）`UEEA-Engineering-Intelligence-手册.md` | Phase 2 实施指南 | Phase 2 适用 |

**纪律**：所有 Runtime 实现 / 评审 / 优化文档**必须显式声明遵循 Iron Laws**。


