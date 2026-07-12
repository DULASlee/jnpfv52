"""Foundry 自博弈启动脚本"""
import asyncio
import sys
from pathlib import Path

# 添加项目根到 path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.orchestrator import FoundryOrchestrator


def main():
    """主入口"""
    config_path = sys.argv[1] if len(sys.argv) > 1 else "config.yaml"
    print(f"加载配置: {config_path}")

    orchestrator = FoundryOrchestrator(config_path)
    asyncio.run(orchestrator.run())


if __name__ == "__main__":
    main()
