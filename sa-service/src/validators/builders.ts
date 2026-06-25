// Test Data Builders - 用于快速构造测试数据
import {
  DataDictionary, DataFlowDef, DataStoreDef, FieldElement
} from './DictValidator';
import { DFD, DFDProcess } from './DFDValidator';
import { BPM, BPMNode } from './BPMValidator';
import { PSpec } from './LogicValidator';
import { DecisionTable } from './CrossEventConsistencyValidator';
import { ER, EREntity } from './ERValidator';
import { UI, UIScreen } from './UIValidator';

// =====================================================
// 通用: 标准审计字段 + TenantId(合法数据)
// =====================================================
export const AUDIT_FIELDS: FieldElement[] = [
  { name: 'created_at', type: 'DATETIME' },
  { name: 'created_by', type: 'NVARCHAR(50)' },
  { name: 'updated_at', type: 'DATETIME' },
  { name: 'updated_by', type: 'NVARCHAR(50)' },
  { name: 'tenant_id', type: 'NVARCHAR(50)' },
];

// =====================================================
// DictBuilder - 数据字典构造器
// =====================================================
export class DictBuilder {
  private dict: DataDictionary = {
    id: 1,
    project_id: 1,
    asset_level: 'EVENT',
    event_id: 1,
    elements: [...AUDIT_FIELDS],
    dataFlows: [],
    dataStores: [],
  };

  withElement(name: string, type: string, isFK = false, refEntity?: string): this {
    this.dict.elements.push({ name, type, isFK, refEntity });
    return this;
  }

  withDataFlow(name: string, fields: FieldElement[]): this {
    this.dict.dataFlows.push({ name, fields });
    return this;
  }

  withDataStore(name: string, fields: FieldElement[], withTenantId = true): this {
    const allFields = withTenantId
      ? [...fields, { name: 'tenant_id', type: 'NVARCHAR(50)' }]
      : fields;
    this.dict.dataStores.push({ name, fields: allFields });
    return this;
  }

  withoutAuditFields(): this {
    this.dict.elements = this.dict.elements.filter(
      e => !['created_at', 'created_by', 'updated_at', 'updated_by', 'tenant_id'].includes(e.name)
    );
    return this;
  }

  withoutTenantId(): this {
    this.dict.elements = this.dict.elements.filter(e => e.name !== 'tenant_id');
    return this;
  }

  build(): DataDictionary {
    return JSON.parse(JSON.stringify(this.dict));
  }
}

// =====================================================
// DFDBuilder - DFD 构造器
// =====================================================
export class DFDBuilder {
  private dfd: DFD = {
    id: 1,
    dfd_levels: {
      '0': { processes: [], flows: [] },
      '1': { processes: [], flows: [] },
    },
    processes: [],
  };

  addLevel0Process(id: string, name: string, inputs: string[], outputs: string[]): this {
    this.dfd.dfd_levels['0'].processes.push({ id, name, inputFlows: inputs, outputFlows: outputs });
    return this;
  }

  addLevel1Process(id: string, name: string, parentId: string, inputs: string[], outputs: string[]): this {
    this.dfd.processes.push({ id, name, parentId, inputFlows: inputs, outputFlows: outputs });
    this.dfd.dfd_levels['1'].processes.push({ id, name, inputFlows: inputs, outputFlows: outputs });
    return this;
  }

  addProcess(id: string, name: string, inputs: string[], outputs: string[], parentId?: string): this {
    this.dfd.processes.push({ id, name, parentId, inputFlows: inputs, outputFlows: outputs });
    return this;
  }

  withEmptyIO(): this {
    // 把所有 process 的 IO 清空(用于测 DFD_NO_INPUT / DFD_NO_OUTPUT)
    this.dfd.processes.forEach(p => {
      p.inputFlows = [];
      p.outputFlows = [];
    });
    return this;
  }

  build(): DFD {
    return JSON.parse(JSON.stringify(this.dfd));
  }
}

// =====================================================
// BPMBuilder - 业务流程图构造器
// =====================================================
export class BPMBuilder {
  private bpm: BPM = {
    swim_lanes: [
      { laneId: 'L1', role: 'worker', name: '工人' },
      { laneId: 'L2', role: 'supervisor', name: '班组长' },
    ],
    activity_nodes: [],
  };

  addNode(id: string, name: string, dfdProcessId: string, type: BPMNode['type'] = 'user_action'): this {
    this.bpm.activity_nodes.push({ id, name, laneId: 'L1', dfdProcessId, type });
    return this;
  }

  build(): BPM {
    return JSON.parse(JSON.stringify(this.bpm));
  }
}

// =====================================================
// DecisionTableBuilder - 判定表构造器
// =====================================================
export class DecisionTableBuilder {
  private table: DecisionTable = {
    id: 'DT-1',
    project_id: 1,
    event_id: 1,
    conditions: [],
    actions: [],
  };

  withCondition(name: string, value: any, operator = '>'): this {
    this.table.conditions.push({ name, operator, value });
    return this;
  }

  withAction(name: string): this {
    this.table.actions.push({ name });
    return this;
  }

  build(): DecisionTable {
    return JSON.parse(JSON.stringify(this.table));
  }
}

// =====================================================
// PSpecBuilder - PSPEC 构造器
// =====================================================
export class PSpecBuilder {
  private spec: PSpec = { process_specs: [] };

  addProcess(id: string, name: string, inputs: string[]): this {
    this.spec.process_specs.push({ id, name, input: inputs, output: [] });
    return this;
  }

  build(): PSpec {
    return JSON.parse(JSON.stringify(this.spec));
  }
}

// =====================================================
// ERBuilder - ER 构造器
// =====================================================
export class ERBuilder {
  private er: ER = { entities: [], relationships: [] };

  addEntity(name: string, columns: EREntity['columns']): this {
    this.er.entities.push({ name, columns });
    return this;
  }

  build(): ER {
    return JSON.parse(JSON.stringify(this.er));
  }
}

// =====================================================
// UIBuilder - UI 构造器
// =====================================================
export class UIBuilder {
  private ui: UI = { id: 1, screens: [] };

  addScreen(id: string, name: string, dataFlow: string, bpmNodeId: string, fields: UIScreen['fields']): this {
    this.ui.screens.push({ id, name, dataFlow, bpmNodeId, fields });
    return this;
  }

  build(): UI {
    return JSON.parse(JSON.stringify(this.ui));
  }
}
