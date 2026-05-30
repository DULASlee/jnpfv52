## 变更类型

- [ ] 新功能 (feat)
- [ ] Bug 修复 (fix)
- [ ] 重构 (refactor)
- [ ] 文档 (docs)
- [ ] 配置/工具 (chore)

## 变更描述

<!-- 简述做了什么，为什么做 -->

## 影响范围

- [ ] 涉及数据库变更（需 Migration 脚本）
- [ ] 涉及 API 变更（需前端同步）
- [ ] 涉及配置变更（需环境同步）
- [ ] 涉及新 modularity 模块

## 自查清单

- [ ] 代码符合项目命名约定（`docs/conventions/naming.md`）
- [ ] 无手动创建 Controller（走 DynamicApiController 自动生成）
- [ ] 新表已标注 `[SugarTable]` 且前缀正确
- [ ] 新表实现了 ITenantFilter（如需租户隔离）
- [ ] 敏感配置不在提交文件中（连接字符串/密钥走 gitignore）
- [ ] `dotnet build` 零错误通过
