# SA Foundry - 自博弈引擎

4 Agent 闭环训练 SA 知识图谱,让系统"越用越聪明"。

## 4 Agent 架构

```
        ┌──────────────┐
        │   Attacker   │  ← 生成对抗需求(挖系统弱点)
        │   攻击者     │
        └──────┬───────┘
               │ 需求
               ▼
        ┌──────────────┐
        │   Builder    │  ← 调用 SAOrchestrator 跑 9 步
        │   构建者     │
        └──────┬───────┘
               │ SA 资产
               ▼
        ┌──────────────┐
        │   Judge      │  ← Validator + 业务语义测试 + 因果图
        │   判官       │
        └──────┬───────┘
               │ Pass/Fail
               ▼
        ┌──────────────┐
        │   Distiller  │  ← 提炼 Pattern + 半衰期遗忘
        │   蒸馏师     │
        └──────┬───────┘
               │ 写入 kg_pattern
               └──────→ 下一轮用更准的 Pattern
```

## 快速开始

```bash
# 1. 装依赖
cd sa-foundry
pip install -r requirements.txt

# 2. 配置(填 OpenAI API key)
export OPENAI_API_KEY=sk-...

# 3. 注入种子知识(5-10 条 Pattern)
python scripts/seed_knowledge.py

# 4. 启动自博弈(默认 10000 次)
python scripts/run_selfplay.py
# 或自定义配置:
python scripts/run_selfplay.py my_config.yaml
```

## 配置项 (config.yaml)

```yaml
foundry:
  total_iterations: 10000      # 总迭代次数
  convergence_window: 100       # 收敛检查窗口
  convergence_threshold: 0.95   # 收敛 pass rate
  checkpoint_interval: 100      # 检查点间隔
  report_interval: 10           # 进度报告间隔

backend:
  sa_orchestrator_url: http://localhost:3000   # 后端 SDK
  validator_url: http://localhost:3001         # Validator 服务
```

## 输出

### 进度条

```
[████████████████████░░░░░░░░░░░░░░] 50.0% (5000/10000) | Pass Rate (100): 78.5% | Patterns: 142 | Speed: 0.32 iter/s
```

### 最终报告

```
============================================================
🏭 Foundry 自博弈训练完成 - 最终报告
============================================================
总迭代次数:    10000
总耗时:        31200 秒
迭代速度:      0.32 iter/s
────────────────────────────────────
整体 pass rate:      72.3%
最近 100 次 pass rate:  94.8%
最近 1000 次 pass rate: 88.5%
────────────────────────────────────
知识图谱 Pattern:
  - 总数:     142
  - 已验证:   87
  - 候选:     55
  - 平均评分: 0.68
  - 失败案例: 234
============================================================
```

## 10 个失败模式(Attacker 种子)

| 模式 | 类别 | 描述 |
|---|---|---|
| concurrent_report | 并发 | 5 个工人同时报工同一工单 |
| cross_shift | 时间边界 | 夜班跨 2 天报工 |
| substitute_material | 数据爆炸 | 10 种候选替代料 |
| lot_tracing | 数据血缘 | 1000 张工单共用 1 批料 |
| scrap_recovery | 库存逻辑 | 报废料回收再用 |
| phantom_bom | BOM 逻辑 | 虚项没展开 |
| rework_loop | 状态机 | 返工死循环 |
| multi_tenant_iso | 安全 | 租户 A 数据泄漏到 B |
| decision_inconsistency | 规则一致 | 跨事件判定不一致 |
| backflush_timing | 事务 | 倒冲时机错位 |

## 关键设计

### 1. Builder 调用 SAOrchestrator(后端 SDK)

```python
# POST http://localhost:3000/api/projects
# POST http://localhost:3000/api/projects/:id/run-sa
```

后端 SDK(`sa-sdk/`)提供完整的 9 步 SA 流水线,Foundry 通过 HTTP 触发。

### 2. Judge 双层验证

- **第一层**:Validator(后端 7 个 Validator,HTTP 调用)
- **第二层**:业务语义测试(基于 Attacker 提供的 test_assertions,用 LLM 判定)

只有两层都通过,才算 pass。

### 3. Distiller 双模式

- **通过 → verified Pattern**:`status=verified`,初始 score=0.5
- **失败 → candidate Pattern**:`status=candidate`,部分正确的部分
- **半衰期**:180 天未使用且 score<0.3 → deprecated

### 4. Attacker 自我进化

- RAG over `failure_rag`:历史失败案例给 Attacker 参考
- 避免重复攻击同一种失败模式
- LLM 高温度(0.9)→ 多样性

## 收敛条件

```
最近 100 次迭代 pass rate >= 95% → 收敛,提前结束
```

参考内容目标:"10000 次自博弈,pass rate 从 60% 提升到 95%+"

## 文件结构

```
sa-foundry/
├── requirements.txt
├── pyproject.toml
├── config.yaml
├── README.md
├── src/
│   ├── __init__.py
│   ├── orchestrator.py          # 主循环
│   ├── agents/
│   │   ├── __init__.py
│   │   ├── base.py              # 基类
│   │   ├── attacker.py          # 攻击者
│   │   ├── builder.py           # 构建者
│   │   ├── judge.py             # 判官
│   │   └── distiller.py         # 蒸馏师
│   ├── knowledge/
│   │   ├── __init__.py
│   │   ├── pattern_store.py     # kg_pattern 封装
│   │   └── failure_rag.py       # 历史失败 RAG
│   └── metrics/
│       ├── __init__.py
│       └── tracker.py           # 指标追踪
├── scripts/
│   ├── run_selfplay.py          # 主入口
│   └── seed_knowledge.py        # 种子知识注入
└── tests/
    └── test_selfplay.py         # 单元测试
```

## 测试

```bash
pytest tests/ -v
# 期望: 6 passed
```

## 与后端的对接

| 后端服务 | Foundry 调用方式 | 频率 |
|---|---|---|
| SAOrchestrator (`sa-sdk/`) | `POST /api/projects` + `POST /api/projects/:id/run-sa` | 每轮 1 次 |
| 7 个 Validator | `POST /api/validate-all/:projectId` | 每轮 1 次 |
| DKEE (kg_pattern) | 直接读/写 SQL(生产用 SQLAlchemy) | 每轮多次 |

后端需要起两个服务:
```bash
# 终端 1: SAOrchestrator 服务
cd sa-sdk && npm run start  # 监听 :3000

# 终端 2: Validator 服务
cd validators && npm run start  # 监听 :3001

# 终端 3: Foundry 自博弈
cd sa-foundry && python scripts/run_selfplay.py
```

## 监控建议

- 每 100 次迭代自动检查点(checkpoint_*.json)
- 最终报告保存到 `checkpoints/final_report.json`
- 可集成 Prometheus / Grafana 监控 pass rate 趋势

## 扩展方向

- **多行业并行**:Attacker 接收 industry 参数,每个行业跑独立 Foundry
- **Distributed**:多机并行跑 Foundry,通过 Redis 共享 kg_pattern
- **在线学习**:用户线上修改也走 Distiller,而不是只靠 Foundry
- **Pattern 解释**:Distiller 提炼的 Pattern 加 LLM-generated 解释
