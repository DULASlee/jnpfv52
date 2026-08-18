#!/usr/bin/env python3
"""E2E 测试运行入口"""
import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

from e2e_test import main

if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
