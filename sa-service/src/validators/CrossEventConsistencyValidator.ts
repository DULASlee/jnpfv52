// CrossEventConsistencyValidator - 判定表跨事件一致性校验器
import { ValidationError, ValidationResult } from './types';

export interface DecisionTable {
  id: string;
  project_id: number;
  event_id: number;
  conditions: Array<{
    name: string;
    operator: string;
    value: any;
  }>;
  actions: Array<{ name: string }>;
}

export interface ConditionWhitelist {
  condition: string;
  allowedValues: string[];
}

export class CrossEventConsistencyValidator {
  constructor(
    private currentTable: DecisionTable,
    private allTablesInProject: DecisionTable[],
    private stateWhitelist: ConditionWhitelist[] = []
  ) {}

  validate(): ValidationResult {
    const errors: ValidationError[] = [];
    errors.push(...this.checkConditionThresholdConsistency());
    errors.push(...this.checkStateWhitelist());
    errors.push(...this.checkActionConsistency());
    return { passed: errors.length === 0, errors };
  }

  // 校验 1:同项目下相同条件名必须有相同阈值
  private checkConditionThresholdConsistency(): ValidationError[] {
    const errors: ValidationError[] = [];
    const conditionMap = new Map<string, Array<{ value: any; tableId: string }>>();

    this.allTablesInProject.forEach(table => {
      table.conditions.forEach(cond => {
        if (!conditionMap.has(cond.name)) conditionMap.set(cond.name, []);
        conditionMap.get(cond.name)!.push({ value: cond.value, tableId: table.id });
      });
    });

    conditionMap.forEach((occurrences, condName) => {
      const uniqueValues = new Set(occurrences.map(o => JSON.stringify(o.value)));
      if (uniqueValues.size > 1) {
        errors.push({
          code: 'CONSISTENCY_CONDITION_CONFLICT',
          message: `条件 "${condName}" 在不同事件中阈值不一致: ` +
                   occurrences.map(o => `${o.tableId}=${JSON.stringify(o.value)}`).join(', '),
          severity: 'ERROR',
          suggestion: '统一所有事件中该条件的阈值,或重命名其中一个条件',
        });
      }
    });
    return errors;
  }

  // 校验 2:状态值必须在白名单中
  private checkStateWhitelist(): ValidationError[] {
    const errors: ValidationError[] = [];
    this.currentTable.conditions.forEach(cond => {
      const pattern = this.stateWhitelist.find(p => cond.name.startsWith(p.condition));
      if (pattern && !pattern.allowedValues.includes(String(cond.value))) {
        errors.push({
          code: 'CONSISTENCY_INVALID_STATE',
          message: `条件 "${cond.name}" 的值 "${cond.value}" 不在白名单 [${pattern.allowedValues.join(',')}] 中`,
          severity: 'ERROR',
        });
      }
    });
    return errors;
  }

  // 校验 3:动作名一致性
  private checkActionConsistency(): ValidationError[] {
    const errors: ValidationError[] = [];
    const actionWhitelist = new Set<string>();
    // 构建白名单时排除当前表，只看其他表的动作
    this.allTablesInProject
      .filter(table => table !== this.currentTable)
      .forEach(table => {
        table.actions.forEach(a => actionWhitelist.add(a.name));
      });

    this.currentTable.actions.forEach(action => {
      if (!actionWhitelist.has(action.name)) {
        errors.push({
          code: 'CONSISTENCY_NEW_ACTION',
          message: `判定表 "${this.currentTable.id}" 引入新动作 "${action.name}"`,
          severity: 'WARNING',
        });
      }
    });
    return errors;
  }
}
