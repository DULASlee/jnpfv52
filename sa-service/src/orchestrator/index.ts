// SA SDK - 主入口
// 后端只需 3 行就能用:
//   const orchestrator = new SAOrchestrator(llm, db, validators);
//   const result = await orchestrator.runSA(req);
//   console.log(result.scope, result.dfd, ...);

export { SAOrchestrator, ValidatorBundle } from './SAOrchestrator';
export { runWithRetry, RetryResult, RetryLoopConfig } from './RetryLoop';
export { decideSteps, runScopeStep, StepDecision, EventComplexity } from './StepRouter';

export { BaseAgent } from './BaseAgent';

export { InMemorySADatabase } from './SADatabase';
export { SqlServerSADatabase } from './SqlServerSADatabase';

export {
  SARequest, SAOutput, SAContext, SAConfig, DEFAULT_SA_CONFIG,
  ILLMClient, ISADatabase, KGPattern, DomainModelContext,
  ScopeOutput, DFDOutput, BPMOutput, DictOutput, PSpecOutput,
  DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  ValidationError
} from './orchestrator-types';
