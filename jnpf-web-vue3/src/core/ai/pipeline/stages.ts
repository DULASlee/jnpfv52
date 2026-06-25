/**
 * Five-stage pipeline definitions
 * @module ai/pipeline/stages
 */

export type PipelineStage = 'requirement' | 'architecture' | 'design' | 'development' | 'delivery';

export type AgentRole = 'requirement-analyst' | 'architect' | 'ui-ux-database' | 'compiler' | 'reviewer';

export interface StageDefinition {
  id: PipelineStage;
  name: string;
  agent: AgentRole;
  inputFrom: PipelineStage | null;
  outputType: string;
  requiresConfirmation: boolean;
}

export const STAGES: StageDefinition[] = [
  { id: 'requirement', name: '需求分析', agent: 'requirement-analyst', inputFrom: null, outputType: 'RequirementAnalysis', requiresConfirmation: true },
  { id: 'architecture', name: '架构设计', agent: 'architect', inputFrom: 'requirement', outputType: 'ArchitectureDesign', requiresConfirmation: true },
  { id: 'design', name: 'UI/UX+数据库', agent: 'ui-ux-database', inputFrom: 'architecture', outputType: 'UIDesign+DatabaseDesign', requiresConfirmation: true },
  { id: 'development', name: '代码生成', agent: 'compiler', inputFrom: 'design', outputType: 'GeneratedProject', requiresConfirmation: false },
  { id: 'delivery', name: '交付', agent: 'reviewer', inputFrom: 'development', outputType: 'DeliveryResult', requiresConfirmation: true },
];

export function getNextStage(current: PipelineStage): PipelineStage | null {
  const idx = STAGES.findIndex(s => s.id === current);
  return idx >= 0 && idx < STAGES.length - 1 ? STAGES[idx + 1].id : null;
}

export function getPrevStage(current: PipelineStage): PipelineStage | null {
  const idx = STAGES.findIndex(s => s.id === current);
  return idx > 0 ? STAGES[idx - 1].id : null;
}
