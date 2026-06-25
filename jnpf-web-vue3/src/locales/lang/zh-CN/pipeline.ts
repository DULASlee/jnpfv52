/**
 * AI 流水线中文语言包（D-9: 2026-06-20）
 */
export default {
  stage: {
    requirement: '需求分析',
    architecture: '架构设计',
    design: '总体设计',
    development: '自动开发',
    delivery: '交付部署',
  },
  status: {
    pending: '待开始',
    running: '运行中',
    review: '待审核',
    stale: '已超时',
    blocked: '已阻断',
    abandoned: '已放弃',
    completed: '已完成',
  },
  action: {
    create: '创建流水线',
    start: '启动',
    confirm: '确认通过',
    reject: '退回修改',
    resume: '继续',
    abandon: '放弃',
    download: '下载源码',
    deploy: '部署到沙箱',
    destroy: '销毁沙箱',
  },
  message: {
    createSuccess: '流水线创建成功',
    startSuccess: '流水线已启动',
    confirmSuccess: '已确认通过',
    rejectSuccess: '已退回修改',
    stageTimeout: '阶段超时，请检查',
    pipelineBlocked: '流水线已阻断，需管理员审核',
  },
  contextBar: {
    stage: '阶段',
    tokens: 'Token',
    elapsed: '耗时',
    failures: '失败',
  },
  timeoutLevel: {
    L1: 'AI 正在思考…',
    L2: '复杂任务处理中，已耗时 {elapsed}秒',
    L3: '处理时间较长，预计还需 {remaining}秒',
    L4: '处理超时，建议重新发起或降级到手工模式',
  },
};
