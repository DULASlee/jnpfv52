# 还原我搞砸的业务功能 + 只保留真正的 LLM 伪成功降级修复

## 我的错误定性

我把阶段 A/B/C 设计方案里明确要求的**业务功能**当"降级/兜底"砍了。三个核心错误：
1. **BuildFallbackSet / BuildFallbackArchitectureClarification / BuildFallbackSystemDesignClarification** 是 LLM 出题质量不达标时的**确定性结构化出题**——是用户能看到多选/矩阵题卡片的来源。方案 25 行 120"禁止确定性出题引擎"针对的是**旧的固定 Q1-Q9 引擎**，不是这些。
2. **AnalystSkillService ReviewClarificationAnswers catch+continue** 是"Gap 2 附录增强"——KG 明确记载"一致性是建议性非阻断"。我改成 throw 阻断了 `SaveRequirementSpecAsync`，需求说明书彻底无法落盘。
3. **StreamTextAsync 推需求说明书 markdown** 是阶段 C 改动 4 明确设计的**真流式 UX**（行 306-309 原型代码 + 行 462-466 验收标准）。我当 bug 报告，是没看方案。

## 还原范围（精确到方法）

### 还原 1：RequirementGateService.BuildFallbackSet（业务功能）
- **还原**行 321-331：`throw Oops.Bah` → 恢复 `return BuildFallbackSet(maturity, round)`
- **还原**行 395-404：同上
- **恢复** `BuildFallbackSet` 方法定义（确定性 MULTI+MATRIX_SINGLE/MATRIX_MULTI 三题生成，含"其他"选项）
- **依据**：RequirementGateService.cs:315 注释"LLM 未产出 → fallback 为 multi + MATRIX_SINGLE" + KG「Clarification交互机制」+ 方案 27 题型约束

### 还原 2：ArchitectSkillService.BuildFallbackArchitectureClarification（业务功能）
- **还原** 3 处 `throw Oops.Bah` → 恢复 `return BuildFallbackArchitectureClarification()`
  - `!response.IsSuccess` 分支
  - JSON 解析失败分支
  - `questions.Count == 0` 分支
- **恢复** `BuildFallbackArchitectureClarification` 方法
- **依据**：架构设计阶段用户看到的澄清题来源

### 还原 3：SystemDesignClarificationSkill.BuildFallbackSystemDesignClarification（业务功能）
- **还原** 3 处 `throw Oops.Bah` → 恢复 `return BuildFallbackSystemDesignClarification()`
- **恢复** `BuildFallbackSystemDesignClarification` 方法
- **依据**：KG「Clarification交互机制」明确"参考实现: SystemDesignClarificationSkill.cs 两阶段暂停-恢复模式"

### 还原 4：AnalystSkillService ReviewClarification catch+continue（业务功能）
- **还原** FinalizeAsync:628-631：`throw Oops.Bah` → 恢复 `catch (Exception ex) when (...) { _logger.LogWarning(...); }`
- **还原** EnrichSkeletonViaSemanticAnalysisAsync：同上模式
- **依据**：KG「Task26-27-28-复核结论」明确"⑦一致性是建议性非阻断，需求分析书会包含 CRITICAL 发现供业务用户审阅" + 该 catch 在 `SaveRequirementSpecAsync` 之前，throw 会阻断说明书落盘

### 还原 5：PmSkillService.BuildEmptyClarificationSet（业务功能）
- **恢复** 方法定义 + 行 1198-1203 的调用（`throw Oops.Bah` → 恢复 `return BuildEmptyClarificationSet(...)`）
- **依据**：PmSkillService.cs:1212 注释"refine 模式或 LLM 失败降级（0 题）：允许跳过"——高成熟度时返回可跳过的空题集是合法业务分支

### 还原 6：RequirementAnalysisOrchestrator 二次出题（业务功能）
- **还原** 行 671-687 和 773-786：`throw Oops.Bah` → 恢复 `GenerateStepClarificationAsync` 调用
- **依据**：阶段 C 改动 2（行 204-233）明确这是"PM 一次出题"——但本次会话我把"一次出题的 META 协议失败"也当降级砍了。META 协议失败时应回退到 GenerateStepClarificationAsync（同是 PM Skill，符合铁律 2）

### 还原 7：PmSkillService.RefineSkeletonAsync 双重 catch（业务功能）
- **还原** 行 1500-1517：我改成的 throw → 恢复原来的 `else { Log+跳过 } + catch { Log+跳过 }`
- **依据**：LLM 细化骨架失败时用确定性补丁继续，是"主 LLM + 确定性基线"双轨设计

### 还原 8：SkillLlmBudgetGuard red tier（基础设施）
- **还原** red tier 的 `throw Oops.Bah` → 恢复 `ShouldDegradeToFast ? "fast" : policy.ModelTier`
- **依据**：方案 25 行 252"基础设施（26）：3 Provider **降级链** · 真熔断"——预算降级是设计方案明文要求的基础设施，不是业务降级

### 还原 9：LlmGatewayService.ParseResponse（基础设施）
- **还原** 两处 catch 的 `IsSuccess=false` → 恢复 `IsSuccess=true, Content=body`
- **依据**：方案 25 行 252"JSON 修复"是基础设施；网关层兜底让上层各取所需

### 还原 10：L13 Hook 移除 B2（BuildFallback* 拦截）
- **移除** guard-write.mjs L13-B2 规则（拦截 BuildFallback* 方法名）
- **保留** L13-B1/B3/B4/B5（真正的伪成功降级拦截）
- **依据**：BuildFallback* 是合法的业务方法命名模式，不能按方法名拦截

## 保留不动的（真降级修复，确实合理）

以下改动**保留**，因为它们确实是"LLM 失败伪装成功"的真降级，且不阻断核心业务：
- PmSkillService EnhanceRequirementAsync 的 `!IsSuccess → throw`（LLM 完全失败应报错）
- PmSkillService ParseEnhanceResponse/ParseRefineResponse 的 JSON 解析 throw（解析失败应报错）
- RequirementGateService EvaluateMaturity 的 3 处 throw（门控评估失败应报错）
- Orchestrator 骨架编译失败 throw（IR 数据损坏应报错）
- SkillHarness UserRequirement 硬门控（防绕过）
- 种子关联筛选 + 可观测性日志
- ExtractSearchKeyword 移除 "enterprise" 兜底

## 不碰的（设计方案核心）

- **StreamTextAsync 推需求说明书 markdown**：阶段 C 改动 4 明确设计的真流式 UX，不动
- **onToken 回调**：阶段 C 设计，不动
- **真流式 SSE token 事件**：阶段 C 验收标准，不动

## 验证

1. `dotnet build modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj` 0 错误
2. `dotnet test tests/JNPF.Tests.PhaseB` — 必须仍然 ≥ 25/25（还原后不能回归）
3. `dotnet test tests/JNPF.Tests.Systems` — 7/7
4. `node scripts/test-hooks.mjs` — Hook 44/44
5. 关键业务路径手验：BuildFallbackSet / BuildFallbackArchitectureClarification / BuildFallbackSystemDesignClarification 方法恢复存在；ReviewClarification catch 恢复为非阻断；SaveRequirementSpecAsync 调用点前面无阻塞性 throw

## 我不做的

- 不再碰 StreamTextAsync（设计方案核心）
- 不再擅自判断"这是不是降级"——以设计方案 25/27/30/31 + 阶段 A/B/C + KG 领域知识为准
- 不再用 PhaseB 25/25 当唯一验收（这次教训：那些测试不覆盖澄清出题链路）