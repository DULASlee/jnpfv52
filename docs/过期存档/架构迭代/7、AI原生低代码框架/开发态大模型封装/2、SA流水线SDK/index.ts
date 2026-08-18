// SA SDK - 主入口
// 后端只需 3 行就能用:
//   const orchestrator = new SAOrchestrator(llm, db, validators);
//   const result = await orchestrator.runSA(req);
//   console.log(result.scope, result.dfd, ...);

export { SAOrchestrator, ValidatorBundle } from './orchestrator/SAOrchestrator';
export { runWithRetry, RetryResult, RetryLoopConfig } from './orchestrator/RetryLoop';
export { decideSteps, runScopeStep, StepDecision, EventComplexity } from './orchestrator/StepRouter';

export {
  ScopeAgent, DFDAgent, BPMAgent, DictAgent, PSpecAgent,
  DecisionTableAgent, ERAgent, StateMachineAgent, UIAgent
} from './agents';
export { BaseAgent } from './agents/BaseAgent';

export { InMemorySADatabase } from './persistence/SADatabase';

export { DKEEExtractor, PatternScorer } from './dkee';

export {
  SARequest, SAOutput, SAContext, SAConfig, DEFAULT_SA_CONFIG,
  ILLMClient, ISADatabase, KGPattern, DomainModelContext,
  ScopeOutput, DFDOutput, BPMOutput, DictOutput, PSpecOutput,
  DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  ValidationError
} from './types';
