"""
JNPF V3.0 TaskRouter — 任务复杂度分级 + 流水线路径选择
========================================================
自动分级规则：按文件数、业务特征（迁移/Entity/API/跨模块）决定S/A/B/C。
C级快速通道：跳过架构师、跳过审查——秒级闭环，避免过度工程。

注意：避免循环导入 — TaskLevel 和 FuguPipeline 在 state_machine.py 中定义，
本模块在 _routing 方法中延迟导入以避免循环依赖。
"""
from typing import List, Optional
from enum import Enum


# 本地定义用于避免循环导入（与 state_machine.py 中的值完全一致）
class TaskLevel(str, Enum):
    S = "S"
    A = "A"
    B = "B"
    C = "C"


class TaskRouter:
    """任务路由器：按复杂度自动选择流水线路径"""

    # 微小任务：单行fix、样式、文案
    C_KEYWORDS = [
        "fix typo", "typo", "format", "格式化", "lint",
        "css", "style", "样式", "文案", "copy",
        "注释", "comment", "readme", "文档",
    ]

    # 复杂任务标志
    COMPLEX_KEYWORDS = [
        "迁移", "migration", "数据库", "表结构",
        "entity", "实体", "api", "接口", "controller",
        "跨模块", "集成", "调用", "新模块", "scaffold",
        "架构", "重构", "refactor", "module",
    ]

    def classify(self, requirement: str, changed_files: Optional[List[str]] = None) -> TaskLevel:
        """
        自动分级规则：
          - C级: ≤1文件 OR 明确微小任务
          - B级: 2-5文件 AND 无实体/API/迁移/跨模块
          - A级: 3-10文件 OR 含实体/API/接口 OR 含迁移（少量文件）
          - S级: >10文件 OR 含迁移+多文件 OR 新模块/scaffold
        """
        file_count = len(changed_files) if changed_files else 0
        req_lower = requirement.lower()

        # 明确微小任务
        if file_count <= 1:
            return TaskLevel.C
        if any(kw in req_lower for kw in ["typo", "format", "lint", "样式", "文案", "注释"]):
            if file_count <= 2:
                return TaskLevel.C

        # 检测复杂关键词
        has_migration = any(kw in req_lower for kw in ["迁移", "migration", "数据库", "表结构"])
        has_entity = any(kw in req_lower for kw in ["entity", "实体"])
        has_api = any(kw in req_lower for kw in ["api", "接口", "controller"])
        is_cross_module = any(kw in req_lower for kw in ["跨模块", "集成", "跨服务"])
        is_new_module = any(kw in req_lower for kw in ["新模块", "scaffold"])

        # S级：新模块 OR 大量文件
        if is_new_module:
            return TaskLevel.S
        if file_count > 10:
            return TaskLevel.S

        # A级起：含迁移/跨模块/实体/API
        if has_migration or is_cross_module:
            return TaskLevel.A
        if has_entity or has_api:
            return TaskLevel.A

        # A级：中量文件
        if file_count > 5:
            return TaskLevel.A

        # B级：少量文件无复杂特征
        return TaskLevel.B

    def get_pipeline(self, level: 'TaskLevel') -> list:
        """获取流水线阶段列表（延迟导入避免循环依赖）"""
        from state_machine import Phase, FuguPipeline
        return FuguPipeline.PIPELINES.get(level, FuguPipeline.PIPELINES[TaskLevel.A])

    def requires_review(self, level: 'TaskLevel') -> bool:
        """C级任务不需要审查"""
        return level != TaskLevel.C

    def requires_exploration(self, level: 'TaskLevel') -> bool:
        """S/A级需要Phase 2.5探索"""
        return level in [TaskLevel.S, TaskLevel.A]

    def requires_decompose(self, level: 'TaskLevel') -> bool:
        """S/A级需要子任务分解"""
        return level in [TaskLevel.S, TaskLevel.A]
