# SA 需求门控 + 附件处理 — 设计方案

> **日期**：2026-06-21
> **来源**：清言代码审查报告（9 项问题）
> **决策**：门控服务只返回结果（选 B），配置独立文件（选 C）

## 架构

```
用户输入 + 附件
       │
       ▼
AIDevelopmentPipelineService.StreamLlmResponseAsync()
       │
       ├─ (1) AttachmentProcessor.ProcessAttachmentsAsync() → 纯文本
       ├─ (2) RequirementGateService.ExtractFromImages() → 图片分析文本
       ├─ (3) RequirementGateService.ValidateHardRules() → HardRuleResult
       │       → Pipeline 根据结果决定发 SSE 拒绝 or 继续
       ├─ (4) RequirementGateService.EvaluateMaturity() → MaturityResult
       │       → Pipeline 根据 Score/Mode 发成熟度提示 SSE
       └─ (5) RequirementGateService.GetSystemPrompt(mode, maturity) → string
               → Pipeline 传入 ChatCompletionRequest
```

**门控服务只做纯逻辑，不碰 SSE。**

## 文件变更

| 文件 | 操作 | 职责 |
|---|---|---|
| `JNPF.InteAssistant.csproj` | 修改 | +EPPlus +NPOI +PdfPig |
| `Gates/AttachmentProcessor.cs` | 新建 | Excel/Word/PDF/文本提取 |
| `Gates/RequirementGateService.cs` | 新建 | 硬规则+成熟度+Prompt+图片提取 |
| `Configurations/MultimodalVision.json` | 新建 | 多模态Vision API配置 |
| `AIDevelopmentPipelineService.cs` | 修改 | StreamLlmResponseAsync 插入门控 |

## 修复清单

| # | 严重度 | 问题 | 修复 |
|---|--------|------|------|
| 1 | 🔴 编译 | ExtractFromImages 缺 model 参数 | 补上 |
| 2 | 🔴 泄漏 | XWPFDocument 未 using | 加 using |
| 3 | 🔴 泄漏 | JsonDocument.Parse 未 using | 加 using |
| 4 | 🔴 泄漏 | StringContent 未 using | 加 using |
| 5 | 🟡 性能 | async 无意义状态机 | 去掉 async |
| 6 | 🟡 配置 | API Key 硬编码 | Configurations/MultimodalVision.json |
| 7 | 🟡 实践 | EPPlus LicenseContext 重复 | static ctor |
| 8 | 🟡 实践 | HttpClient DefaultRequestHeaders | HttpRequestMessage |
| 9 | 🟡 健壮 | GetPipelineAttachments 占位 | 三方案骨架 |

## 关键决策

- **SseEvent**：不改，门控不碰 SSE
- **配置**：`Configurations/MultimodalVision.json`（独立文件）
- **DI**：ITransient 自动扫描，无需手动注册
- **ChatMessage**：Content 是纯 string，附件文本直接拼接
- **图片**：走独立 HttpClient + Vision API，不经过 ChatMessage
