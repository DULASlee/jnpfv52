/**
 * 创始人控制台中文语言包（D-9: 2026-06-20）
 */
export default {
  title: '创始人控制台',
  totp: {
    title: '创始人二次认证',
    subtitle: '请输入 6 位 TOTP 验证码以访问创始人功能',
    emailLabel: '管理员邮箱',
    codeLabel: 'TOTP 验证码',
    verify: '验证并进入',
    back: '返回首页',
    error: '验证失败，请检查邮箱和验证码是否正确',
    success: '验证成功',
    locked: '验证失败次数过多，已锁定 5 分钟',
  },
  model: {
    title: '模型与 Prompt 配置',
    primary: '主模型',
    fallback: '备用模型',
    temperature: '温度参数',
    maxTokens: '最大 Token',
    save: '保存配置',
  },
  selfPlay: {
    title: '自博弈引擎',
    status: '运行状态',
    enabled: '已启用',
    disabled: '已停用',
    toggle: '切换',
    rounds: '博弈轮次',
    novelPatterns: '新发现模式',
  },
  knowledge: {
    title: '知识图谱审核',
    nodeCount: '节点数',
    edgeCount: '边数',
    candidateCount: '候选模式',
    verifiedCount: '已验证模式',
  },
  audit: {
    title: '系统级审计日志',
    emptyHint: '暂无审计记录',
  },
};
