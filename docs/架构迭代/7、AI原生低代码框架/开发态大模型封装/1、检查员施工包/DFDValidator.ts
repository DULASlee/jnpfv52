// DFDValidator - DFD 校验器
import { ValidationError, ValidationResult } from './types';

export interface DFDProcess {
  id: string;
  name: string;
  inputFlows: string[];
  outputFlows: string[];
  parentId?: string;
}

export interface DFD {
  id: number;
  dfd_levels: {
    [key: string]: {
      processes: DFDProcess[];
      flows: { name: string; from: string; to: string }[];
    };
  };
  processes: DFDProcess[];
}

export class DFDValidator {
  constructor(private dfd: DFD) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkParentChildBalance());
    errors.push(...this.checkConservation());
    errors.push(...this.checkProcessIO());
    return { passed: errors.length === 0, errors };
  }

  private checkParentChildBalance(): ValidationError[] {
    const errors: ValidationError[] = [];
    const level0 = this.dfd.dfd_levels['0']?.processes || [];

    level0.forEach(parent => {
      const childProcesses = this.dfd.processes.filter(p => p.parentId === parent.id);
      if (childProcesses.length === 0) {
        errors.push({
          code: 'DFD_NOT_DECOMPOSED',
          message: `Level 0 过程 "${parent.id}" 在 Level 1 未分解`,
          severity: 'ERROR',
        });
        return;
      }
      const childIO = new Set<string>();
      childProcesses.forEach(p => {
        p.inputFlows.forEach(f => childIO.add(f));
        p.outputFlows.forEach(f => childIO.add(f));
      });
      const parentIO = new Set([...parent.inputFlows, ...parent.outputFlows]);
      parentIO.forEach(io => {
        if (!childIO.has(io)) {
          errors.push({
            code: 'DFD_BALANCE_MISMATCH',
            message: `过程 "${parent.id}" 的 IO "${io}" 在子图中找不到对应`,
            severity: 'ERROR',
          });
        }
      });
    });
    return errors;
  }

  private checkConservation(): ValidationError[] {
    const errors: ValidationError[] = [];
    this.dfd.processes.forEach(process => {
      process.inputFlows.forEach(flow => {
        if (!this.findFlowProducer(flow, process.id)) {
          errors.push({
            code: 'DFD_BLACK_HOLE',
            message: `过程 "${process.id}" 的输入流 "${flow}" 找不到来源(数据黑洞)`,
            severity: 'ERROR',
          });
        }
      });
      process.outputFlows.forEach(flow => {
        if (!this.findFlowConsumer(flow, process.id)) {
          errors.push({
            code: 'DFD_MIRACLE',
            message: `过程 "${process.id}" 的输出流 "${flow}" 没有消费者(数据奇迹)`,
            severity: 'ERROR',
          });
        }
      });
    });
    return errors;
  }

  private checkProcessIO(): ValidationError[] {
    const errors: ValidationError[] = [];
    this.dfd.processes.forEach(process => {
      if (process.inputFlows.length === 0) {
        errors.push({
          code: 'DFD_NO_INPUT',
          message: `过程 "${process.id}" 没有输入流`,
          severity: 'ERROR',
        });
      }
      if (process.outputFlows.length === 0) {
        errors.push({
          code: 'DFD_NO_OUTPUT',
          message: `过程 "${process.id}" 没有输出流`,
          severity: 'ERROR',
        });
      }
    });
    return errors;
  }

  private findFlowProducer(flow: string, excludeProcessId: string): boolean {
    return this.dfd.processes.some(
      p => p.id !== excludeProcessId && p.outputFlows.includes(flow)
    );
  }

  private findFlowConsumer(flow: string, excludeProcessId: string): boolean {
    return this.dfd.processes.some(
      p => p.id !== excludeProcessId && p.inputFlows.includes(flow)
    );
  }
}
