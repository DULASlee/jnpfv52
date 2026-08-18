# sa-service（已退役）

> **状态：** 已退役（2026-07-15）
> **当前 Studio S2 模式：** compile（默认）— 不依赖 sa-service

sa-service 的 SA 九步分析能力已由 C# 实现：
- 前 7 步：`SaNineViewCompiler`（确定性编译，零 LLM）
- 后 2 步：`PmSkillService.EnhancePspecDecisionTableAsync`（调后端 LlmGateway）

compile 模式下 `start-dev.ps1` 不启动 sa-service。
DKEE 的领域知识按需检索能力已由 C# 的 `DomainKnowledgeRenderer` + `IDomainSeedService` 实现。
