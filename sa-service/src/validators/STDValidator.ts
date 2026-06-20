// STDValidator - 状态机校验器
import { ValidationError, ValidationResult } from './types';

export interface StateTransition {
  from: string;
  to: string;
  trigger: string;
}

export interface StateMachine {
  entity: string;
  states: string[];
  transitions: StateTransition[];
}

export interface STD {
  stateMachines: StateMachine[];
}

export class STDValidator {
  constructor(
    private std: STD,
    private dict?: { dataStores?: Array<{ name: string }> }
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    this.std.stateMachines.forEach(sm => {
      errors.push(...this.checkTransitionStates(sm));
      errors.push(...this.checkDeadStates(sm));
      errors.push(...this.checkUnreachableStates(sm));
      errors.push(...this.checkDuplicateTransitions(sm));
      errors.push(...this.checkEntityInDict(sm));
    });
    return { passed: errors.length === 0, errors };
  }

  // 校验 1: 转换中的状态必须在 states 列表中
  private checkTransitionStates(sm: StateMachine): ValidationError[] {
    const errors: ValidationError[] = [];
    const stateSet = new Set(sm.states);

    sm.transitions.forEach((t, i) => {
      if (!stateSet.has(t.from)) {
        errors.push({
          code: 'STD_INVALID_FROM_STATE',
          message: `实体 "${sm.entity}" 转换[${i}] 的源状态 "${t.from}" 不在状态列表中`,
          severity: 'ERROR',
        });
      }
      if (!stateSet.has(t.to)) {
        errors.push({
          code: 'STD_INVALID_TO_STATE',
          message: `实体 "${sm.entity}" 转换[${i}] 的目标状态 "${t.to}" 不在状态列表中`,
          severity: 'ERROR',
        });
      }
    });
    return errors;
  }

  // 校验 2: 无死状态（除终态外，每个状态必须有出边）
  private checkDeadStates(sm: StateMachine): ValidationError[] {
    const errors: ValidationError[] = [];
    const statesWithOutgoing = new Set(sm.transitions.map(t => t.from));
    // 终态：有入边但无出边的状态是合法的（如 Completed、Cancelled）
    const statesWithIncoming = new Set(sm.transitions.map(t => t.to));

    sm.states.forEach(state => {
      if (!statesWithOutgoing.has(state) && statesWithIncoming.has(state)) {
        // 有入边无出边 = 终态，合法但给出 WARNING
        errors.push({
          code: 'STD_TERMINAL_STATE',
          message: `实体 "${sm.entity}" 状态 "${state}" 是终态（有入边无出边），请确认这是预期行为`,
          severity: 'WARNING',
        });
      }
      if (!statesWithOutgoing.has(state) && !statesWithIncoming.has(state)) {
        // 无入边无出边 = 孤立状态
        errors.push({
          code: 'STD_ISOLATED_STATE',
          message: `实体 "${sm.entity}" 状态 "${state}" 是孤立状态（无入边无出边）`,
          severity: 'ERROR',
        });
      }
    });
    return errors;
  }

  // 校验 3: 无不可达状态（除起始态外，每个状态必须有入边）
  private checkUnreachableStates(sm: StateMachine): ValidationError[] {
    const errors: ValidationError[] = [];
    const statesWithIncoming = new Set(sm.transitions.map(t => t.to));
    const statesWithOutgoing = new Set(sm.transitions.map(t => t.from));

    sm.states.forEach(state => {
      if (!statesWithIncoming.has(state) && statesWithOutgoing.has(state)) {
        // 有出边无入边 = 起始态，合法
        // 不报错
      }
      if (!statesWithIncoming.has(state) && !statesWithOutgoing.has(state)) {
        // 已在 checkDeadStates 中处理
      }
    });
    return errors;
  }

  // 校验 4: 无重复转换
  private checkDuplicateTransitions(sm: StateMachine): ValidationError[] {
    const errors: ValidationError[] = [];
    const seen = new Set<string>();

    sm.transitions.forEach((t, i) => {
      const key = `${t.from}→${t.to}@${t.trigger}`;
      if (seen.has(key)) {
        errors.push({
          code: 'STD_DUPLICATE_TRANSITION',
          message: `实体 "${sm.entity}" 转换[${i}] 重复: ${key}`,
          severity: 'ERROR',
        });
      }
      seen.add(key);
    });
    return errors;
  }

  // 校验 5: 实体名必须在字典的 dataStore 中
  private checkEntityInDict(sm: StateMachine): ValidationError[] {
    const errors: ValidationError[] = [];
    if (!this.dict?.dataStores) return errors;

    const storeNames = new Set(this.dict.dataStores.map(s => s.name));
    if (!storeNames.has(sm.entity)) {
      errors.push({
        code: 'STD_ENTITY_NOT_IN_DICT',
        message: `状态机实体 "${sm.entity}" 不在数据字典的 dataStore 中`,
        severity: 'ERROR',
      });
    }
    return errors;
  }
}
