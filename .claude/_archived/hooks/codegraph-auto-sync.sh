#!/bin/bash
# CodeGraph Auto-Sync Post-Commit Hook
# 每次提交后自动同步 CodeGraph 索引，确保 AI 看到最新代码结构
#
# 安装方式: 复制到 .git/hooks/post-commit 并 chmod +x
# 或通过: git config core.hooksPath .claude/git-hooks

set -e

REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo "$(pwd)")
BACKEND_DIR="$REPO_ROOT/backend"
LOCK_FILE="$BACKEND_DIR/.codegraph/.sync.lock"

# 检查 codegraph 是否可用
if ! command -v codegraph &> /dev/null; then
    exit 0  # 静默跳过，不阻塞提交
fi

# 检查后端目录是否有 .codegraph 索引
if [ ! -d "$BACKEND_DIR/.codegraph" ]; then
    exit 0  # 未初始化索引，跳过
fi

# 防重入：如果最近 30 秒内同步过，跳过
if [ -f "$LOCK_FILE" ]; then
    LAST_SYNC=$(stat -c %Y "$LOCK_FILE" 2>/dev/null || echo 0)
    NOW=$(date +%s 2>/dev/null || echo 0)
    if [ $((NOW - LAST_SYNC)) -lt 30 ]; then
        exit 0
    fi
fi

# 仅在有 C# 代码变更时才同步
CHANGED_CS=$(git diff-tree --no-commit-id --name-only -r HEAD 2>/dev/null | grep '\.cs$' || true)
if [ -z "$CHANGED_CS" ]; then
    exit 0  # 无 C# 变更，跳过
fi

# 执行同步
touch "$LOCK_FILE"
cd "$BACKEND_DIR" && codegraph sync --quiet . 2>/dev/null || true
rm -f "$LOCK_FILE"

exit 0
