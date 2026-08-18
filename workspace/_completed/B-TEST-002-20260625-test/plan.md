# 实施计划 — B-TEST-002

## 子任务列表
| ID | 名称 | 依赖 | 预估token | 验收标准 |
|:---|:---|:---|:---|:---|
| ST-001 | OrderEntity 增加 Email 属性 | 无 | 600 | 编译通过，含 TenantId |
| ST-002 | OrderDto 增加 Email 属性 | ST-001 | 400 | Mapster 映射不覆盖审计字段 |
| ST-003 | OrderService 增加 Email 校验 | ST-002 | 800 | 正则校验 + 单元测试 |
| ST-004 | 更新单元测试 | ST-003 | 600 | dotnet test 全部通过 |

## DAG 图
```
ST-001 → ST-002 → ST-003 → ST-004
```

## 回滚策略
任一子任务失败，回滚到上一个子任务完成状态

## 验收标准
- 编译通过 (dotnet build)
- 测试通过 (dotnet test)
- Entity 含 TenantId
- DTO 不覆盖审计字段
- Email 格式校验生效
