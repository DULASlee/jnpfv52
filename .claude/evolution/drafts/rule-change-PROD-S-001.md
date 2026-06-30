# 规则变更草案 — 任务 PROD-S-001
生成时间: 2026-06-25T19:11:33.913684
异常数量: 3

## 建议修改清单

### TRAP-002 | review | reviewer
- **症状**: Mapster Adapt 未排除审计字段
- **根因**: guard-reviewer 仅扫描字符串级 Adapt
- **建议修复**: .Ignore(x => x.CreateTime)
- **目标规则文件**: .claude/souls/coder/rules/jnpf-expert-traps.md
- **复发次数**: 3

### TRAP-002 | review | reviewer
- **症状**: Mapster Adapt 未排除审计字段
- **根因**: guard-reviewer 仅扫描字符串级 Adapt
- **建议修复**: .Ignore(x => x.CreateTime)
- **目标规则文件**: .claude/souls/coder/rules/jnpf-expert-traps.md
- **复发次数**: 3

### TRAP-002 | review | reviewer
- **症状**: Mapster Adapt 未排除审计字段
- **根因**: guard-reviewer 仅扫描字符串级 Adapt
- **建议修复**: .Ignore(x => x.CreateTime)
- **目标规则文件**: .claude/souls/coder/rules/jnpf-expert-traps.md
- **复发次数**: 3

---
## 人工审核区

- [ ] 已审核所有建议修改清单
- [ ] 已修改对应规则文件
- [ ] 已提交 Git

> ⚠️ **AI 绝不能自己修改规则文件。必须由人类工程师审核后手动修改。**