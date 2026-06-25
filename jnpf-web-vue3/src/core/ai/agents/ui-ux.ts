/**
 * UI/UX 设计智能体
 *
 * 根据需求描述和架构设计生成页面 UI 方案。
 * 产出的 IR 可直接送入编译网关（compileGateway）生成代码。
 *
 * 关键约束：
 *   - 生成的组件必须使用 ComponentRegistry 中已注册的组件
 *   - 自动填充 aiHints.designRationale
 *   - 3D 大屏标记 VIP 限制
 *
 * @version 1.0.0
 * @module ai/agents/ui-ux
 */

import type { LLMGateway } from '../llm/types';
import { UI_UX_DESIGNER_PROMPT } from '../llm/prompts';
import type { FormPageIR } from '../../ir/types';
import type { DashboardIR } from '../../ir/dashboard-types';
import { BaseAgent, type AgentContext, type AgentResponse } from './base';

// ============================================================
// 输出类型
// ============================================================

export interface UIDesign {
  /** 设计概述 */
  overview: string;
  /** 页面类型 */
  pageType: 'form' | 'list' | 'dashboard' | 'detail';
  /** 设计理由 */
  designRationale: string;
  /** 布局方案 */
  layout: {
    type: 'grid' | 'flex' | 'absolute';
    columns?: number;
    gap?: number;
    responsive?: boolean;
  };
  /** 配色方案 */
  colorScheme: {
    primary: string;
    secondary: string;
    background: string;
    text: string;
  };
  /** 页面 IR（FormPageIR 或 DashboardIR） */
  ir: Partial<FormPageIR> | Partial<DashboardIR>;
  /** 交互定义 */
  interactions: Array<{
    trigger: string;
    action: string;
    animation?: string;
  }>;
}

// ============================================================
// 已知组件白名单（从 ComponentRegistry 获取）
// ============================================================

const KNOWN_JNPF_COMPONENTS = [
  'JnpfInput',
  'JnpfInputNumber',
  'JnpfTextarea',
  'JnpfSelect',
  'JnpfRadio',
  'JnpfCheckbox',
  'JnpfCascader',
  'JnpfTreeSelect',
  'JnpfDatePicker',
  'JnpfTimePicker',
  'JnpfSwitch',
  'JnpfRate',
  'JnpfSlider',
  'JnpfColorPicker',
  'JnpfUploadImg',
  'JnpfUploadFile',
  'JnpfSign',
  'JnpfSignature',
  'JnpfEditor',
  'JnpfRow',
  'JnpfCol',
  'JnpfDivider',
  'JnpfAlert',
  'JnpfTabs',
  'JnpfTabPane',
  'JnpfTable',
  'JnpfList',
  'JnpfCard',
  'JnpfDescriptions',
  'ECharts:Bar',
  'ECharts:Line',
  'ECharts:Pie',
  'ECharts:Map',
];

const KNOWN_PC_COMPONENTS: Record<string, string> = {
  JnpfInput: 'a-input',
  JnpfInputNumber: 'a-input-number',
  JnpfTextarea: 'a-textarea',
  JnpfSelect: 'a-select',
  JnpfRadio: 'a-radio-group',
  JnpfCheckbox: 'a-checkbox-group',
  JnpfCascader: 'a-cascader',
  JnpfTreeSelect: 'a-tree-select',
  JnpfDatePicker: 'a-date-picker',
  JnpfTimePicker: 'a-time-picker',
  JnpfSwitch: 'a-switch',
  JnpfRate: 'a-rate',
  JnpfSlider: 'a-slider',
  JnpfColorPicker: 'a-color-picker',
  JnpfUploadImg: 'a-upload',
  JnpfUploadFile: 'a-upload',
  JnpfSign: 'signature-pad',
  JnpfSignature: 'signature-pad',
  JnpfEditor: 'rich-text-editor',
  JnpfRow: 'a-row',
  JnpfCol: 'a-col',
  JnpfDivider: 'a-divider',
  JnpfAlert: 'a-alert',
  JnpfTabs: 'a-tabs',
  JnpfTabPane: 'a-tab-pane',
  JnpfTable: 'a-table',
  JnpfList: 'a-list',
  JnpfCard: 'a-card',
  JnpfDescriptions: 'a-descriptions',
};

// ============================================================
// UIUXAgent
// ============================================================

export class UIUXAgent extends BaseAgent {
  constructor(llm: LLMGateway) {
    super(llm, UI_UX_DESIGNER_PROMPT);
  }

  /**
   * 生成 UI 设计方案。
   *
   * 流程：LLM 生成 UIDesign → 校验组件白名单 → 填充 aiHints
   *
   * @param requirement - 需求描述（自然语言或 JSON 化的需求分析）
   * @param context - 执行上下文（可传入 availableComponents 覆盖默认组件列表）
   * @returns UI 设计方案
   */
  async design(requirement: string, context: AgentContext = {}): Promise<AgentResponse<UIDesign>> {
    // 注入可用组件列表到上下文
    const components = (context.availableComponents as string) ?? KNOWN_JNPF_COMPONENTS.join(', ');

    const result = await this.execute<UIDesign>(requirement, {
      ...context,
      availableComponents: components,
    });

    // 后处理：校验组件、填充 aiHints、检测 VIP
    result.data = this.validateComponents(result.data);
    result.data = this.injectHints(result.data, requirement);
    result.data = this.detectVIP(result.data);

    return result;
  }

  // ============================================================
  // 后处理
  // ============================================================

  /**
   * 校验 IR 中的组件是否在注册表中。
   * 未知组件降级为 a-input。
   */
  private validateComponents(design: UIDesign): UIDesign {
    if (!design.ir) return design;
    const ir = design.ir as Partial<FormPageIR>;
    if (ir.fields) {
      for (const field of ir.fields) {
        const jnpfKey = field.component?.jnpfKey;
        if (jnpfKey && !KNOWN_JNPF_COMPONENTS.includes(jnpfKey)) {
          console.warn(`[UIUXAgent] 未知组件 "${jnpfKey}"，降级为 JnpfInput`);
          field.component = {
            jnpfKey: 'JnpfInput',
            pc: 'a-input',
            app: 'uni-easyinput',
            legacyApp: 'uni-easyinput',
          };
        }
        // 自动补全 pc/app 映射
        if (jnpfKey && field.component && !field.component.pc) {
          field.component.pc = KNOWN_PC_COMPONENTS[jnpfKey] ?? 'a-input';
          field.component.app = 'uni-easyinput';
          field.component.legacyApp = 'uni-easyinput';
        }
      }
    }
    return design;
  }

  /**
   * 注入 aiHints（设计理由、置信度等）。
   */
  private injectHints(design: UIDesign, requirement: string): UIDesign {
    if (!design.ir) return design;
    const ir = design.ir as Partial<FormPageIR>;
    if (ir.aiHints === undefined) {
      ir.aiHints = {};
    }
    ir.aiHints = {
      ...ir.aiHints,
      designRationale: design.designRationale,
      requirement: requirement.slice(0, 200),
    };
    return design;
  }

  /**
   * 检测是否需要 3D 大屏（VIP 功能）。
   */
  private detectVIP(design: UIDesign): UIDesign {
    if (!design.ir) return design;
    const ir = design.ir as Partial<DashboardIR>;
    if (design.pageType === 'dashboard' && (design.overview.includes('3D') || design.overview.includes('三维'))) {
      if (ir.aiHints === undefined) {
        ir.aiHints = {};
      }
      ir.aiHints = {
        ...ir.aiHints,
        domain: '3D-digital-twin',
      };
      console.warn('[UIUXAgent] 检测到 3D 大屏需求，此为 VIP 功能，需授权');
    }
    return design;
  }
}
