"""种子知识初始化 - 启动 Foundry 前先注入 5-10 条种子 Pattern"""
import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from src.knowledge.pattern_store import PatternStore


# 种子 Pattern(参考内容提到:5-10 条种子 DomainPattern)
SEED_PATTERNS = [
    {
        "type": "field_naming",
        "industry": "manufacturing",
        "content": {
            "commonFields": [
                {"name": "WorkOrderId", "type": "BIGINT", "isFK": True, "refEntity": "WorkOrder"},
                {"name": "OperatorId", "type": "NVARCHAR(50)", "isFK": True, "refEntity": "User"},
                {"name": "ReportQty", "type": "DECIMAL(18,2)", "isFK": False},
                {"name": "ScrapQty", "type": "DECIMAL(18,2)", "isFK": False},
            ],
            "applicableScenarios": ["MES-机加工", "MES-装配"],
        },
        "tags": ["MES", "标准字段", "机加工"],
        "source": "human-created",
        "status": "verified",
    },
    {
        "type": "decision_rule",
        "industry": "manufacturing",
        "content": {
            "rules": [
                {"condition": "报废率>5%", "action": "让步接收", "frequency": 8},
                {"condition": "报废率>20%", "action": "驳回", "frequency": 8},
            ],
            "hasDefaultRule": True,
            "defaultAction": "合格",
        },
        "tags": ["MES", "报废判定"],
        "source": "human-created",
        "status": "verified",
    },
    {
        "type": "state_machine",
        "industry": "manufacturing",
        "content": {
            "entity": "ProductionReport",
            "states": ["待校验", "待终核", "已归档", "已驳回", "让步接收"],
            "transitions": [
                {"from": "待校验", "to": "待终核", "trigger": "初核通过"},
                {"from": "待终核", "to": "已归档", "trigger": "终核通过"},
                {"from": "待终核", "to": "已驳回", "trigger": "终核驳回"},
                {"from": "待终核", "to": "让步接收", "trigger": "让步接收"},
            ],
        },
        "tags": ["MES", "报工状态机"],
        "source": "human-created",
        "status": "verified",
    },
    {
        "type": "process_pattern",
        "industry": "manufacturing",
        "content": {
            "standardProcesses": [
                {"id": "P1", "name": "录入与初核"},
                {"id": "P2", "name": "终核校验"},
                {"id": "P3", "name": "进度查询"},
                {"id": "P4", "name": "物料消耗"},
                {"id": "P4.3", "name": "倒冲扣库存"},
            ],
        },
        "tags": ["MES", "标准流程"],
        "source": "human-created",
        "status": "verified",
    },
    {
        "type": "field_naming",
        "industry": "manufacturing",
        "content": {
            "commonFields": [
                {"name": "TenantId", "type": "NVARCHAR(50)", "isFK": False, "isRequired": True},
                {"name": "CreatedAt", "type": "DATETIME", "isFK": False, "isRequired": True},
                {"name": "CreatedBy", "type": "NVARCHAR(50)", "isFK": True, "refEntity": "User"},
            ],
            "note": "多租户 + 审计必备字段",
        },
        "tags": ["通用", "审计", "多租户"],
        "source": "human-created",
        "status": "verified",
    },
    # 候选 Pattern(等 Foundry 自博弈验证)
    {
        "type": "decision_rule",
        "industry": "manufacturing",
        "content": {
            "rules": [
                {"condition": "物料损耗率>10%", "action": "挂起待查", "frequency": 3},
            ],
            "scenario": "装配车间",
        },
        "tags": ["MES", "物料损耗", "候选"],
        "source": "self-play",
        "status": "candidate",
    },
]


async def main():
    store = PatternStore()
    for p in SEED_PATTERNS:
        await store.upsert(p)
    print(f"✓ 已注入 {len(SEED_PATTERNS)} 条种子 Pattern")
    print(f"  - verified: {sum(1 for p in SEED_PATTERNS if p['status'] == 'verified')}")
    print(f"  - candidate: {sum(1 for p in SEED_PATTERNS if p['status'] == 'candidate')}")
    print(f"\n知识图谱初始状态:")
    print(json.dumps(store.get_stats(), indent=2))


if __name__ == "__main__":
    import json
    asyncio.run(main())
