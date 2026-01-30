#!/bin/bash
# =============================================================================
# Unity AutoBuild CLI 安装脚本
# 安装 build 命令到系统 PATH
# =============================================================================

set -e

# 获取脚本所在目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_SCRIPT="$SCRIPT_DIR/build"

# 检查 build wrapper 是否存在
if [ ! -f "$BUILD_SCRIPT" ]; then
    echo "❌ 错误: 未找到 build 脚本"
    exit 1
fi

# 确定安装目标
if [ -d "/usr/local/bin" ] && [ -w "/usr/local/bin" ]; then
    INSTALL_DIR="/usr/local/bin"
elif [ -d "$HOME/.local/bin" ]; then
    INSTALL_DIR="$HOME/.local/bin"
else
    INSTALL_DIR="$HOME/.local/bin"
    mkdir -p "$INSTALL_DIR"
fi

# 安装
cp "$BUILD_SCRIPT" "$INSTALL_DIR/build"
chmod +x "$INSTALL_DIR/build"

echo "✅ build 命令已安装到 $INSTALL_DIR/build"
echo ""
echo "用法:"
echo "  cd /path/to/unity-project"
echo "  build ios      # 构建 iOS 并上传 TestFlight"
echo "  build android  # 构建 Android 并上传 R2"
echo ""

# 检查是否在 PATH 中
if ! command -v build &> /dev/null; then
    echo "⚠️  提示: $INSTALL_DIR 可能不在 PATH 中"
    echo "   请将以下行添加到 ~/.zshrc 或 ~/.bashrc:"
    echo ""
    echo "   export PATH=\"\$HOME/.local/bin:\$PATH\""
    echo ""
fi
