#!/bin/bash
# =============================================================================
# Telegram 通知脚本
# 用法: ./notify.sh <message>
# 
# 环境变量:
#   TELEGRAM_BOT_TOKEN  - Telegram Bot Token
#   TELEGRAM_CHAT_ID    - Telegram Chat ID
# =============================================================================

MESSAGE="$1"

if [ -z "$MESSAGE" ]; then
    echo "用法: $0 <message>"
    exit 1
fi

# 从环境变量或配置文件读取 token
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 尝试从 .env 文件读取
if [ -f "$PROJECT_ROOT/.ci/.env" ]; then
    source "$PROJECT_ROOT/.ci/.env"
fi

if [ -z "$TELEGRAM_BOT_TOKEN" ] || [ -z "$TELEGRAM_CHAT_ID" ]; then
    echo "[WARNING] 未配置 Telegram 通知"
    echo "[INFO] 请设置环境变量或创建 .ci/.env 文件:"
    echo ""
    echo "  export TELEGRAM_BOT_TOKEN='your_bot_token'"
    echo "  export TELEGRAM_CHAT_ID='your_chat_id'"
    echo ""
    exit 0  # 不作为错误退出
fi

# 添加项目信息
PROJECT_NAME=$(basename "$PROJECT_ROOT")
TIMESTAMP=$(date "+%Y-%m-%d %H:%M:%S")

# 使用 printf 处理换行
FULL_MESSAGE=$(printf "[%s] %s\n\n⏰ %s" "$PROJECT_NAME" "$MESSAGE" "$TIMESTAMP")

# 发送消息
echo "[INFO] 发送 Telegram 通知..."

RESPONSE=$(curl -s -X POST \
    "https://api.telegram.org/bot$TELEGRAM_BOT_TOKEN/sendMessage" \
    -d "chat_id=$TELEGRAM_CHAT_ID" \
    -d "text=$FULL_MESSAGE" \
    -d "parse_mode=HTML")

# 检查响应
if echo "$RESPONSE" | grep -q '"ok":true'; then
    echo "[SUCCESS] 通知发送成功"
else
    echo "[WARNING] 通知发送失败: $RESPONSE"
fi
