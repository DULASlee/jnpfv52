// BPMValidator - 业务流程图校验器
import { ValidationError, ValidationResult } from './types';

export interface BPMNode {
  id: string;
  name: string;
  laneId: string;
  dfdProcessId: string;
  type: 'user_action' | 'system_action' | 'decision';
}

export interface BPM {
  swim_lanes: { laneId: string; role: string; name: string }[];
  activity_nodes: BPMNode[];
}

export interface DFDProcessLite {
  id: string;
  name: string;
}

export class BPMValidator {
  constructor(
    private bpm: BPM,
    private dfdProcesses: DFDProcessLite[]
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkBPMNodesInDFD());
    errors.push(...this.checkDFDProcessesInBPM());
    return { passed: errors.length === 0, errors };
  }

  // 校验 1:每个 BPM 节点必须绑定 DFD 过程
  private checkBPMNodesInDFD(): ValidationError[] {
    const errors: ValidationError[] = [];
    const dfdProcessIds = new Set(this.dfdProcesses.map(p => p.id));

    this.bpm.activity_nodes.forEach(node => {
      if (!dfdProcessIds.has(node.dfdProcessId)) {
        errors.push({
          code: 'BPM_NODE_NO_DFD',
          message: `BPM 节点 "${node.id}" 绑定的 DFD 过程 "${node.dfdProcessId}" 不存在`,
          severity: 'ERROR',
        });
      }
    });
    return errors;
  }

  // 校验 2:每个 DFD 过程至少有一个 BPM 节点
  private checkDFDProcessesInBPM(): ValidationError[] {
    const errors: ValidationError[] = [];
    const bpmProcessIds = new Set(this.bpm.activity_nodes.map(n => n.dfdProcessId));

    this.dfdProcesses.forEach(p => {
      if (!bpmProcessIds.has(p.id)) {
        errors.push({
          code: 'BPM_DFD_NO_NODE',
          message: `DFD 过程 "${p.id}" 在 BPM 中无对应节点`,
          severity: 'WARNING',
        });
      }
    });
    return errors;
  }
}
