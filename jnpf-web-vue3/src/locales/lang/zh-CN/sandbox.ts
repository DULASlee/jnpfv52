/**
 * 沙箱管理中文语言包（D-9: 2026-06-20）
 */
export default {
  title: '沙箱管理',
  status: {
    creating: '创建中',
    running: '运行中',
    stopped: '已停止',
    destroyed: '已销毁',
    error: '异常',
  },
  stat: {
    active: '活跃沙箱',
    cpu: 'CPU 使用率',
    memory: '内存使用',
    uptime: '运行时间',
  },
  action: {
    create: '新建沙箱',
    destroy: '销毁',
    viewLog: '查看日志',
    ssh: '终端',
    deploy: '部署代码',
    healthCheck: '健康检查',
  },
  message: {
    createSuccess: '沙箱创建成功',
    destroySuccess: '沙箱已销毁',
    destroyConfirm: '确认销毁沙箱？此操作不可撤销',
    noSandbox: '暂无沙箱',
  },
};
