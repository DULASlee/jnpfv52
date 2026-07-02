# C2 施工包行前检查清单

> **日期**：2026-07-03
> **用途**：C2.1 开工前的最后一轮前置条件确认

---

## 前置条件检查

| # | 检查项 | 来源 | 状态 | 备注 |
|---|--------|------|------|------|
| 1 | IR 扩展方案已定稿 | `docs/superpowers/specs/2026-07-02-ir-extension-design.md` | ✅ | 10 缺口全部 JSON Schema 化，6 MUST-FIX / 4 CAN-DEFER |
| 2 | 临时记录生命周期已定稿 | `docs/superpowers/specs/2026-07-02-visualdev-temp-record-lifecycle.md` | ✅ | 创建→使用→清理→过期清扫 完整链路 |
| 3 | CodeGenService 接口已梳理 | 前置阅读报告 + 代码探索 | ✅ | `DownloadCode(id, DownloadCodeFormInput)` + `CodePreview(id, input)` |
| 4 | VisualDevEntity 字段映射已知 | 同上 | ✅ | FormData/Tables/ColumnData/Category/WebType/Type 全部已梳理 |
| 5 | IR 生成侧入口已定位 | 本次探索 | ✅ | 见下文 §IR 入口定位 |
| 6 | C2.1 伪代码已输出 | `docs/superpowers/notes/c2.1-pseudocode.md` | ✅ | 数据流完整，模糊点已暴露 |
| 7 | 沙箱初始化脚本方案 | 待 C2.7 阶段设计 | ⏸ | C2.1 不涉及 |

---

## IR 入口精确定位

### IR 数据流

```
SA Service (外部 Node.js 服务)
  │  执行 3-Tier 流水线: Scope → DFD → BPM → Dict → ER → STD
  │  产出 F_FinalIR JSON
  │
  ▼
BASE_AI_GENERATED_PROJECT 表 (.F_FinalIR 列)
  │  直接写入数据库
  │
  ▼
C2 适配器从此处读取
  GeneratedProjectEntity.F_FinalIR (string, JSON)
```

### 关键发现

**`F_FinalIR` 不是由 C# 后端写入的**。它由外部 SA Service 直接写入 `BASE_AI_GENERATED_PROJECT` 表。C# 代码中仅声明了实体字段（`GeneratedProjectEntity.cs:57-58`），`GeneratedProjectService.cs` 仅做查询展示。

### C2.1 的 IR 读取路径

```csharp
// 从数据库读取 IR
var project = await _db.Queryable<GeneratedProjectEntity>()
    .Where(p => p.F_PipelineStatus == "completed" && !p.F_DeleteMark)
    .FirstAsync();

var irJson = project.F_FinalIR; // JSON 字符串
var ir = JsonSerializer.Deserialize<IntermediateRepresentation>(irJson);
```

### IR 扩展需要修改的位置

当前 SA Service 产出的 IR JSON 仅有 `modules/tables/pages/dashboard` 四字段。新增字段（`fields[].fieldType`、`relations[]`、`operations[]` 等）需要在 **SA Service（Node.js/TypeScript）** 侧修改 JSON 输出结构，C# 侧只需新增对应的反序列化模型。

**影响评估**：IR 扩展涉及跨服务改动（SA Service + C# 适配器），C2.1 开工时需要 SA Service 开发者同步配合或使用降级策略（默认值填充）。

---

## 开工阻塞项

| 阻塞项 | 影响 | 处理 |
|--------|------|------|
| SA Service IR 扩展未同步 | IR JSON 缺少字段类型/外键/权限信息 | 降级策略：C# 适配器用默认值填充缺失字段 |
| Docker 环境未就绪 | 无法端到端验证生成→预览链路 | B 阶段封板阻塞，不影响 C2.1 代码实现 |

**结论**：C2.1 可独立开工（IR 降级策略已就绪），不需要等 SA Service 改造。SA Service 改造可与 C2.1 并行推进，完成后切换为完整 IR 输入。
