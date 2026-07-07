/** SA 门控语义评估 — 对齐后端 SemanticFitnessResult / gate SSE payload */

export interface IdentifiedElement {
  category: string;
  description: string;
  evidence?: string;
}

export interface MissingElement {
  category: string;
  description: string;
  severity: string;
  howToFix: string;
}

export interface SemanticFitnessResult {
  passed: boolean;
  score: number;
  level: string;
  identified: IdentifiedElement[];
  missing: MissingElement[];
  nextStepGuidance?: string;
}

export interface GateFailedPayload {
  reason?: string;
  hint?: string;
  warnings?: string[];
  semanticFitness?: SemanticFitnessResult;
}

export interface GatePassedPayload {
  mergedText?: string;
  warnings?: string[];
  semanticFitness?: SemanticFitnessResult;
}

export interface GateErrorPayload {
  message?: string;
  errorCode?: string;
}

export interface ChatStreamAction {
  label: string;
  type?: 'primary' | 'default' | 'link';
  action: 'fill_prompt' | 'focus_input';
  payload?: string;
}

export interface AttachmentsReadyPayload {
  processed?: number;
  failed?: number;
  warnings?: string[];
  items?: Array<{
    id?: string;
    fileName?: string;
    processStatus?: number;
    extractedLength?: number;
    error?: string;
  }>;
}
