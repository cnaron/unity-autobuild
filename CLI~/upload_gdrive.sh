#!/bin/bash
# =============================================================================
# Google Drive 上传脚本
# 用法: ./upload_gdrive.sh <android_build_path>
# 
# 支持两种工具:
# 1. gdrive CLI (推荐)
# 2. rclone
# =============================================================================

set -e

ANDROID_BUILD_PATH="$1"

if [ -z "$ANDROID_BUILD_PATH" ]; then
    echo "用法: $0 <android_build_path>"
    exit 1
fi

# 查找最新的 APK/AAB 文件
APK_FILE=$(find "$ANDROID_BUILD_PATH" -name "*.apk" -o -name "*.aab" 2>/dev/null | sort -t_ -k3 -n | tail -1)

if [ -z "$APK_FILE" ] || [ ! -f "$APK_FILE" ]; then
    echo "[ERROR] 未找到 APK/AAB 文件在: $ANDROID_BUILD_PATH"
    exit 1
fi

echo "[INFO] 准备上传: $APK_FILE"
echo "[INFO] 文件大小: $(du -h "$APK_FILE" | cut -f1)"

# Google Drive 文件夹 ID (可通过环境变量配置)
GDRIVE_FOLDER_ID="${GDRIVE_FOLDER_ID:-}"

# 方式 1: gdrive CLI
if command -v gdrive &> /dev/null; then
    echo "[INFO] 使用 gdrive 上传..."
    
    UPLOAD_CMD="gdrive files upload \"$APK_FILE\""
    
    if [ -n "$GDRIVE_FOLDER_ID" ]; then
        UPLOAD_CMD="gdrive files upload \"$APK_FILE\" --parent \"$GDRIVE_FOLDER_ID\""
    fi
    
    RESULT=$(eval "$UPLOAD_CMD" 2>&1) || {
        echo "[ERROR] gdrive 上传失败: $RESULT"
        exit 1
    }
    
    echo "[SUCCESS] 上传成功!"
    echo "$RESULT"
    
    # 提取文件 ID 并生成分享链接
    FILE_ID=$(echo "$RESULT" | grep -oE "[a-zA-Z0-9_-]{25,}" | head -1)
    if [ -n "$FILE_ID" ]; then
        gdrive files share "$FILE_ID" 2>/dev/null || true
        SHARE_LINK="https://drive.google.com/file/d/$FILE_ID/view?usp=sharing"
        echo "[INFO] 分享链接: $SHARE_LINK"
        
        # 保存链接供通知使用
        echo "$SHARE_LINK" > "$ANDROID_BUILD_PATH/last_gdrive_link.txt"
    fi
    
    exit 0
fi

# 方式 2: rclone
if command -v rclone &> /dev/null; then
    echo "[INFO] 使用 rclone 上传..."
    
    # 假设已配置 remote 名为 "gdrive"
    REMOTE_PATH="${RCLONE_REMOTE:-gdrive}:Unity_Builds/"
    
    rclone copy "$APK_FILE" "$REMOTE_PATH" --progress || {
        echo "[ERROR] rclone 上传失败"
        exit 1
    }
    
    echo "[SUCCESS] rclone 上传成功!"
    exit 0
fi

# 未安装上传工具
echo "[WARNING] 未安装 gdrive 或 rclone"
echo "[INFO] 请安装上传工具:"
echo ""
echo "1. 安装 gdrive (推荐):"
echo "   brew install gdrive"
echo "   gdrive account add  # 首次需要授权"
echo ""
echo "2. 安装 rclone:"
echo "   brew install rclone"
echo "   rclone config  # 配置 Google Drive remote"
echo ""
echo "3. 手动上传:"
echo "   打开 https://drive.google.com 并上传: $APK_FILE"
echo ""

# 打开 Finder 显示文件
open -R "$APK_FILE"

exit 1
