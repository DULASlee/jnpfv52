/**
 * 编译目标枚举与元数据
 *
 * 每个 CompileTarget 对应一个编译器实例。
 * 所有编译器共享同一个 IR 输入（FormPageIR 或 DashboardIR）。
 *
 * @jnpf-generated v5.2.0 type=compiler-targets platform=universal
 */

// ─── 编译目标枚举 ───

export type CompileTarget =
  | 'vue3-web'
  | 'dashboard'
  | 'dashboard-3d'
  | 'uniapp-weixin'
  | 'uniapp-alipay'
  | 'uniapp-douyin'
  | 'uniapp-h5'
  | 'uniapp-x-app'
  | 'workflow';

// ─── 目标元数据 ───

export interface CompileTargetMeta {
  id: CompileTarget;
  name: string;
  description: string;
  icon: string;
  /** VIP 功能（需授权） */
  vip: boolean;
  /** 输入 IR 类型 */
  irType: 'form' | 'dashboard';
  /** 输出文件扩展名 */
  outputExtensions: string[];
}

export const COMPILE_TARGETS: Record<CompileTarget, CompileTargetMeta> = {
  'vue3-web': {
    id: 'vue3-web',
    name: 'Vue3 Web 应用',
    description: '标准 Vue3 + Ant Design Vue web 应用，可独立运行',
    icon: 'vue',
    vip: false,
    irType: 'form',
    outputExtensions: ['.vue', '.ts'],
  },
  dashboard: {
    id: 'dashboard',
    name: '数字大屏',
    description: 'Vue3 + ECharts 数据大屏',
    icon: 'dashboard',
    vip: false,
    irType: 'dashboard',
    outputExtensions: ['.vue', '.ts', '.css', '.json'],
  },
  'dashboard-3d': {
    id: 'dashboard-3d',
    name: '3D 数字孪生大屏',
    description: '含 Three.js 3D 场景',
    icon: '3d',
    vip: true,
    irType: 'dashboard',
    outputExtensions: ['.vue', '.ts', '.css', '.json'],
  },
  'uniapp-weixin': {
    id: 'uniapp-weixin',
    name: '微信小程序',
    description: '标准 uni-app 微信小程序',
    icon: 'wechat',
    vip: false,
    irType: 'form',
    outputExtensions: ['.vue', '.ts', '.json'],
  },
  'uniapp-alipay': {
    id: 'uniapp-alipay',
    name: '支付宝小程序',
    description: '标准 uni-app 支付宝小程序',
    icon: 'alipay',
    vip: false,
    irType: 'form',
    outputExtensions: ['.vue', '.ts', '.json'],
  },
  'uniapp-douyin': {
    id: 'uniapp-douyin',
    name: '抖音小程序',
    description: '标准 uni-app 抖音小程序',
    icon: 'douyin',
    vip: false,
    irType: 'form',
    outputExtensions: ['.vue', '.ts', '.json'],
  },
  'uniapp-h5': {
    id: 'uniapp-h5',
    name: 'H5 移动端',
    description: '标准 uni-app H5',
    icon: 'h5',
    vip: false,
    irType: 'form',
    outputExtensions: ['.vue', '.ts', '.json'],
  },
  'uniapp-x-app': {
    id: 'uniapp-x-app',
    name: '原生 App',
    description: 'uni-app X (v5.0 暂缓)',
    icon: 'app',
    vip: true,
    irType: 'form',
    outputExtensions: ['.vue', '.uts', '.ts', '.json'],
  },
  workflow: {
    id: 'workflow',
    name: '工作流',
    description: 'FlowIR → 可部署工作流配置',
    icon: 'workflow',
    vip: false,
    irType: 'form',
    outputExtensions: ['.json'],
  },
};
