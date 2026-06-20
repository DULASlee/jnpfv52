"""Mock 服务 - 模拟后端 SAOrchestrator / Validator / LLM"""
import asyncio
import random
import time
import json
from typing import Any, Optional


class MockLLM:
    """模拟 LLM - 模拟"第一次出错、第二次修正"的真实行为"""

    def __init__(self, base_error_rate: float = 0.4, seed: int = 42):
        self.base_error_rate = base_error_rate
        self.call_count = 0
        # 固定随机种子,让测试可重复
        self.rng = random.Random(seed)

    async def generate(self, system_prompt: str, context: dict) -> str:
        """模拟 LLM 生成"""
        self.call_count += 1

        # 模拟延迟
        await asyncio.sleep(0.05)

        # 根据 KG 模式 + 错误反馈,动态调整错误率
        kg_count = len(context.get("kg_patterns", []))
        retry_count = len(context.get("last_errors", []))
        # 关键调优:KG 模式大幅降低错误率,错误反馈也大幅降低
        effective_error_rate = max(
            0.01,
            self.base_error_rate - kg_count * 0.20 - retry_count * 0.30,
        )

        # 模拟:有一定概率生成"错误"输出
        if self.rng.random() < effective_error_rate:
            return self._generate_flawed_output(system_prompt, context)
        return self._generate_correct_output(system_prompt, context)

    def _generate_correct_output(self, system_prompt: str, context: dict) -> str:
        """生成正确输出 - 根据 KG Pattern 调整"""
        # 提取 KG 里的字段命名 Pattern
        kg_field_names = []
        kg_decision_conditions = []
        for p in context.get("kg_patterns", []):
            if p.get("type") == "field_naming":
                kg_field_names = p.get("content", {}).get("commonFields", [])
            elif p.get("type") == "decision_rule":
                for rule in p.get("content", {}).get("rules", []):
                    cond = rule.get("condition", "")
                    if cond.startswith("报废率"):
                        kg_decision_conditions.append(cond)

        if "数据字典" in system_prompt or "数据元素" in system_prompt:
            # 基础字段
            elements = [
                {"name": "WorkOrderId", "type": "BIGINT", "isFK": True, "refEntity": "WorkOrder"},
                {"name": "OperatorId", "type": "NVARCHAR(50)", "isFK": True, "refEntity": "User"},
                {"name": "ReportQty", "type": "DECIMAL(18,2)"},
                {"name": "ScrapQty", "type": "DECIMAL(18,2)"},
                {"name": "ReportTime", "type": "DATETIME"},
                {"name": "Status", "type": "NVARCHAR(20)"},
                {"name": "TenantId", "type": "NVARCHAR(50)"},
                {"name": "CreatedAt", "type": "DATETIME"},
                {"name": "CreatedBy", "type": "NVARCHAR(50)"},
            ]
            # 如果 KG 里有别的字段名,加上(温启动会用上)
            for fn in kg_field_names:
                if not any(e["name"] == fn for e in elements):
                    elements.append({"name": fn, "type": "NVARCHAR(50)"})

            return json.dumps({
                "elements": elements,
                "dataFlows": [{"name": "报工单", "fields": [
                    {"name": "WorkOrderId", "type": "BIGINT"},
                    {"name": "ReportQty", "type": "DECIMAL(18,2)"},
                ]}],
                "dataStores": [{"name": "ProductionReport", "fields": [
                    {"name": "Id", "type": "BIGINT"},
                    {"name": "WorkOrderId", "type": "BIGINT"},
                    {"name": "TenantId", "type": "NVARCHAR(50)"},
                ]}],
            }, ensure_ascii=False)

        if "判定表" in system_prompt or "业务规则" in system_prompt:
            # 优先用 KG 里的判定条件(保证跨事件一致)
            if kg_decision_conditions:
                # 解析 condition 里的阈值(如 "报废率>3%" → 0.03)
                first_cond = kg_decision_conditions[0]
                threshold_str = first_cond.split(">")[1].replace("%", "")
                threshold_value = float(threshold_str) / 100
                conditions = [
                    {"name": first_cond, "operator": ">", "value": threshold_value},
                    {"name": "报废率>20%", "operator": ">", "value": 0.20},
                ]
            else:
                conditions = [
                    {"name": "报废率>5%", "operator": ">", "value": 0.05},
                    {"name": "报废率>20%", "operator": ">", "value": 0.20},
                ]
            return json.dumps({
                "tables": [{
                    "id": "DT-1",
                    "conditions": conditions,
                    "actions": [
                        {"name": "合格接收"},
                        {"name": "让步接收"},
                        {"name": "驳回"},
                    ],
                    "rules": [
                        {"conditionMask": [True, False], "actionIndex": 0},
                        {"conditionMask": [True, False], "actionIndex": 1},
                        {"conditionMask": [False, True], "actionIndex": 2},
                    ],
                }],
            }, ensure_ascii=False)

        if "状态机" in system_prompt:
            return json.dumps({
                "state_machines": [{
                    "entity": "ProductionReport",
                    "states": ["待校验", "待终核", "已归档", "已驳回", "让步接收"],
                    "transitions": [
                        {"from": "待校验", "to": "待终核", "trigger": "初核通过"},
                        {"from": "待终核", "to": "已归档", "trigger": "终核通过"},
                        {"from": "待终核", "to": "已驳回", "trigger": "终核驳回"},
                    ],
                }],
            }, ensure_ascii=False)

        return json.dumps({"status": "ok"}, ensure_ascii=False)

    def _generate_flawed_output(self, system_prompt: str, context: dict) -> str:
        """生成有瑕疵的输出(模拟 LLM 幻觉)"""
        if "数据字典" in system_prompt:
            # 错误 1:字段名不在白名单 + 字段类型错误
            return json.dumps({
                "elements": [
                    {"name": "WorkOrderId", "type": "BIGINT"},
                    {"name": "ProductName", "type": "XMLTYPE"},  # ❌ 非法类型
                    {"name": "Qty", "type": "INT"},  # ❌ 应该是 DECIMAL
                ],
                "dataFlows": [],
                "dataStores": [{"name": "WorkOrder", "fields": [{"name": "Id", "type": "BIGINT"}]}],
            }, ensure_ascii=False)

        if "判定表" in system_prompt:
            # 错误 2:跨事件条件不一致
            return json.dumps({
                "tables": [{
                    "id": "DT-1",
                    "conditions": [{"name": "报废率>3%", "operator": ">", "value": 0.03}],  # ❌ 阈值不一致
                    "actions": [{"name": "合格"}],
                    "rules": [],
                }],
            }, ensure_ascii=False)

        return json.dumps({"status": "flawed"}, ensure_ascii=False)


class MockValidator:
    """模拟 7 个 Validator"""

    VALIDATORS = ["DFDValidator", "BPMValidator", "DictValidator", "LogicValidator",
                  "CrossEventConsistencyValidator", "ERValidator", "UIValidator"]

    def __init__(self, llm: MockLLM):
        self.llm = llm
        self.call_count = 0

    async def validate(self, agent_name: str, output: dict, context: dict) -> dict:
        """跑对应 Validator"""
        self.call_count += 1
        await asyncio.sleep(0.02)

        errors = []

        if agent_name == "DictAgent":
            errors.extend(self._validate_dict(output))
        elif agent_name == "DecisionTableAgent":
            errors.extend(self._validate_decision_table(output, context))
        elif agent_name == "StateMachineAgent":
            errors.extend(self._validate_state_machine(output))

        return {
            "passed": len(errors) == 0,
            "errors": errors,
            "validator": self.VALIDATORS[0],
        }

    def _validate_dict(self, output: dict) -> list:
        errors = []
        elements = output.get("elements", [])
        for e in elements:
            if e.get("type") == "XMLTYPE":
                errors.append({
                    "code": "DICT_INVALID_TYPE",
                    "message": f"字段 {e.get('name')} 类型 XMLTYPE 不在白名单中",
                    "severity": "ERROR",
                })
            if e.get("name") == "Qty" and e.get("type") == "INT":
                errors.append({
                    "code": "DICT_INVALID_TYPE",
                    "message": f"字段 Qty 类型 INT 应为 DECIMAL(18,2)",
                    "severity": "ERROR",
                })
        return errors

    def _validate_decision_table(self, output: dict, context: dict) -> list:
        errors = []
        tables = output.get("tables", [])
        for t in tables:
            for cond in t.get("conditions", []):
                # 检查跨事件一致性
                existing_patterns = context.get("kg_patterns", [])
                for p in existing_patterns:
                    if p.get("type") == "decision_rule":
                        for rule in p.get("content", {}).get("rules", []):
                            # 关键:只有 EXACT 名字相同的条件才检查一致性
                            if rule.get("condition") == cond["name"]:
                                # value 是否和 condition name 一致
                                if "%" in cond["name"]:
                                    expected_value = float(cond["name"].split(">")[1].replace("%", "")) / 100
                                    if abs(cond.get("value", 0) - expected_value) > 0.001:
                                        errors.append({
                                            "code": "CONSISTENCY_CONDITION_VALUE_MISMATCH",
                                            "message": f"条件 {cond['name']} 的 value {cond.get('value')} 与预期 {expected_value} 不一致",
                                            "severity": "ERROR",
                                        })
        return errors

    def _validate_state_machine(self, output: dict) -> list:
        # 简化:不返回错误
        return []


class MockSAOrchestrator:
    """模拟 SAOrchestrator - 跑 9 步 SA 流水线"""

    def __init__(self, llm: MockLLM, validator: MockValidator):
        self.llm = llm
        self.validator = validator
        self.max_retries = 5
        self.run_count = 0

    async def run_sa_pipeline(self, requirement: str, kg_patterns: list = None) -> dict:
        """跑 9 步 SA 流水线"""
        self.run_count += 1
        start = time.time()
        kg_patterns = kg_patterns or []

        # 关键:context 跨 retry 保留,这样错误反馈能积累
        context = {
            "requirement": requirement,
            "kg_patterns": kg_patterns,
            "last_errors": [],
        }

        retries = 0
        last_errors = []

        for attempt in range(1, self.max_retries + 1):
            dict_output = json.loads(await self.llm.generate("数据字典 agent", context))
            dict_validation = await self.validator.validate("DictAgent", dict_output, context)
            if not dict_validation["passed"]:
                last_errors = [e["message"] for e in dict_validation["errors"]]
                context["last_errors"] = last_errors  # 跨 retry 累加
                retries += 1
                continue

            dt_output = json.loads(await self.llm.generate("判定表 agent", context))
            dt_validation = await self.validator.validate("DecisionTableAgent", dt_output, context)
            if not dt_validation["passed"]:
                last_errors = [e["message"] for e in dt_validation["errors"]]
                context["last_errors"] = last_errors
                retries += 1
                continue

            # 都通过
            return {
                "passed": True,
                "dict": dict_output,
                "decision_table": dt_output,
                "retries": retries,
                "duration_ms": int((time.time() - start) * 1000),
            }

        return {
            "passed": False,
            "errors": last_errors,
            "retries": retries,
            "duration_ms": int((time.time() - start) * 1000),
        }


class MockDKEE:
    """模拟 DKEE 提炼服务"""

    def __init__(self):
        self.patterns: list[dict] = []
        self.next_id = 1

    def extract_patterns(self, sa_output: dict) -> list:
        """从 SA 输出提炼 Pattern - 增量更新已有 Pattern"""
        new_patterns = []
        if sa_output.get("passed"):
            dict_output = sa_output.get("dict", {})
            # 提炼字段命名 Pattern
            field_names = [e["name"] for e in dict_output.get("elements", []) if not e.get("isFK")]
            if len(field_names) >= 3:
                # 查找是否已存在类似 Pattern
                existing = next((p for p in self.patterns if p["type"] == "field_naming"), None)
                if existing:
                    # 增量更新:合并字段,增加 frequency
                    existing_fields = set(existing["content"].get("commonFields", []))
                    new_fields = set(field_names)
                    merged = list(existing_fields | new_fields)
                    existing["content"]["commonFields"] = merged
                    existing["usage_count"] += 1
                    new_patterns.append(existing)  # 返回更新过的
                else:
                    pattern = {
                        "id": self.next_id,
                        "type": "field_naming",
                        "industry": "manufacturing",
                        "content": {"commonFields": field_names},
                        "source": "self-play",
                        "status": "candidate",
                        "score": 0.5,
                        "usage_count": 1,
                    }
                    self.next_id += 1
                    self.patterns.append(pattern)
                    new_patterns.append(pattern)

            # 提炼判定表 Pattern
            dt_output = sa_output.get("decision_table", {})
            for table in dt_output.get("tables", []):
                conditions = [c["name"] for c in table.get("conditions", [])]
                if conditions:
                    existing = next((p for p in self.patterns if p["type"] == "decision_rule"), None)
                    if existing:
                        # 合并规则,加 frequency
                        existing_rules = {r["condition"]: r for r in existing["content"].get("rules", [])}
                        for c in conditions:
                            if c in existing_rules:
                                existing_rules[c]["frequency"] += 1
                            else:
                                existing_rules[c] = {"condition": c, "frequency": 1}
                        existing["content"]["rules"] = list(existing_rules.values())
                        existing["usage_count"] += 1
                        new_patterns.append(existing)
                    else:
                        pattern = {
                            "id": self.next_id,
                            "type": "decision_rule",
                            "industry": "manufacturing",
                            "content": {"rules": [{"condition": c, "frequency": 1} for c in conditions]},
                            "source": "self-play",
                            "status": "candidate",
                            "score": 0.5,
                            "usage_count": 1,
                        }
                        self.next_id += 1
                        self.patterns.append(pattern)
                        new_patterns.append(pattern)

        return new_patterns

    def update_scores(self):
        """根据使用情况更新评分"""
        for p in self.patterns:
            if p["status"] == "candidate" and p["usage_count"] >= 3:
                p["status"] = "verified"
                p["score"] = 0.8

    def get_top_patterns(self, n: int = 5) -> list:
        """取 Top N Pattern"""
        return sorted(self.patterns, key=lambda p: p["score"], reverse=True)[:n]

    def increment_usage(self, pattern_ids: list):
        """记录使用"""
        for p in self.patterns:
            if p["id"] in pattern_ids:
                p["usage_count"] += 1

    def get_stats(self) -> dict:
        verified = [p for p in self.patterns if p["status"] == "verified"]
        return {
            "total": len(self.patterns),
            "verified": len(verified),
            "candidate": len(self.patterns) - len(verified),
            "avg_score": round(sum(p["score"] for p in self.patterns) / max(len(self.patterns), 1), 2),
        }


class MockFrontend:
    """模拟前端 - 人类专家修改 AI 生成的字段"""

    def __init__(self):
        self.changes: list[dict] = []

    def human_review(self, sa_output: dict) -> dict:
        """模拟人类 review - 只在第一次做修改,温启动时大部分场景不需要改"""
        modified_output = json.loads(json.dumps(sa_output))  # 深拷贝
        changes = []

        # 第一次 review:人类通常会改 1-2 处
        # 第二次 review(温启动):基于 KG Pattern,通常 0 处修改

        # 模拟:人类补充一个 AI 漏掉的字段
        if "dict" in modified_output:
            elements = modified_output["dict"].get("elements", [])
            if not any(e["name"] == "ScrapReason" for e in elements):
                elements.append({"name": "ScrapReason", "type": "NVARCHAR(50)"})
                changes.append({
                    "field": "elements[].ScrapReason",
                    "before": None,
                    "after": "ScrapReason NVARCHAR(50)",
                    "reason": "报废原因字段是行业必备,AI 漏了",
                })

        self.changes.extend(changes)
        return {"modified_output": modified_output, "changes": changes}
