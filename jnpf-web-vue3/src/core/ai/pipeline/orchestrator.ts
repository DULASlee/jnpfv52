/**
 * Pipeline Orchestrator (A1)
 * @module ai/pipeline/orchestrator
 */

import type { LLMGateway } from '../llm/types';
import type { AgentContext } from '../agents/base';
import { RequirementAnalystAgent } from '../agents/requirement-analyst';
import { ArchitectAgent } from '../agents/architect';
import { UIUXAgent } from '../agents/ui-ux';
import { DatabaseAgent } from '../agents/database';
import type { PipelineStage } from './stages';
import { getNextStage } from './stages';
import { type PipelineState, createInitialState, transition, advanceStage, updateConfidence } from './state-machine';

export class OrchestratorAgent {
  private requirementAgent: RequirementAnalystAgent;
  private architectAgent: ArchitectAgent;
  private uiuxAgent: UIUXAgent;
  private databaseAgent: DatabaseAgent;

  constructor(llm: LLMGateway) {
    this.requirementAgent = new RequirementAnalystAgent(llm);
    this.architectAgent = new ArchitectAgent(llm);
    this.uiuxAgent = new UIUXAgent(llm);
    this.databaseAgent = new DatabaseAgent(llm);
  }

  async advance(state: PipelineState, userInput = ''): Promise<PipelineState> {
    state = transition(state, 'running');
    try {
      switch (state.currentStage) {
        case 'requirement':
          return this.doRequirement(state, userInput);
        case 'architecture':
          return this.doArchitecture(state);
        case 'design':
          return this.doDesign(state);
        case 'development':
          return this.doDevelopment(state);
        case 'delivery':
          return this.doDelivery(state);
      }
    } catch (e) {
      state = transition(state, 'failed', (e as Error).message);
      state.error = (e as Error).message;
    }
    return state;
  }

  async confirm(state: PipelineState): Promise<PipelineState> {
    const next = getNextStage(state.currentStage);
    if (!next) {
      state.status = 'completed';
      return state;
    }
    state = advanceStage(state, next);
    if (state.currentStage === 'development') return this.advance(state);
    return state;
  }

  async revise(state: PipelineState, feedback: string): Promise<PipelineState> {
    return this.advance(transition(state, 'running', `revise: ${feedback}`), feedback);
  }

  private async doRequirement(state: PipelineState, userInput: string): Promise<PipelineState> {
    const r = await this.requirementAgent.analyze(userInput);
    state.requirement = r.data;
    return transition(updateConfidence(state, r.confidence), 'waiting_confirmation');
  }

  private async doArchitecture(state: PipelineState): Promise<PipelineState> {
    const r = await this.architectAgent.design(JSON.stringify(state.requirement ?? {}));
    state.architecture = r.data;
    return transition(updateConfidence(state, r.confidence), 'waiting_confirmation');
  }

  private async doDesign(state: PipelineState): Promise<PipelineState> {
    const ctx: AgentContext = { currentIR: state.architecture as Record<string, unknown> };
    const [ui, db] = await Promise.all([
      this.uiuxAgent.design(JSON.stringify(state.architecture ?? {}), ctx),
      this.databaseAgent.design(JSON.stringify(state.architecture ?? {}), ctx),
    ]);
    state.design = { ui: ui.data, database: db.data };
    return transition(updateConfidence(state, (ui.confidence + db.confidence) / 2), 'waiting_confirmation');
  }

  private async doDevelopment(state: PipelineState): Promise<PipelineState> {
    state.development = { code: JSON.stringify(state.design?.ui ?? {}, null, 2), target: 'vue3-web' };
    return transition(state, 'waiting_confirmation');
  }

  private async doDelivery(state: PipelineState): Promise<PipelineState> {
    state.delivery = { url: '', zip: 'ready' };
    state.status = 'completed';
    return state;
  }

  getStagePrompt(stage: PipelineStage): string {
    return (
      (
        { requirement: '描述需求', architecture: '确认领域模型', design: '生成UI+DB方案', development: '生成代码中', delivery: '确认下载' } as Record<
          string,
          string
        >
      )[stage] ?? ''
    );
  }
}
