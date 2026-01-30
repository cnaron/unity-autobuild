#!/bin/bash
# =============================================================================
# TestFlight 上传脚本
# 用法: ./upload_testflight.sh <ios_build_path>
# 
# 支持两种认证方式:
# 1. App Store Connect API Key (推荐)
#    设置环境变量: ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_FILE
# 2. Fastlane (需要预先配置)
# =============================================================================

set -e

IOS_BUILD_PATH="$1"

if [ -z "$IOS_BUILD_PATH" ]; then
    echo "用法: $0 <ios_build_path>"
    exit 1
fi

# 加载环境变量
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/.env" ]; then
    source "$SCRIPT_DIR/.env"
fi

# 查找 IPA 文件
IPA_FILE=""
if [ -f "$IOS_BUILD_PATH/build/last_ipa_path.txt" ]; then
    IPA_FILE=$(cat "$IOS_BUILD_PATH/build/last_ipa_path.txt")
fi

if [ -z "$IPA_FILE" ] || [ ! -f "$IPA_FILE" ]; then
    IPA_FILE=$(find "$IOS_BUILD_PATH/build/ipa" -name "*.ipa" 2>/dev/null | head -1)
fi

if [ -z "$IPA_FILE" ] || [ ! -f "$IPA_FILE" ]; then
    echo "[ERROR] 未找到 IPA 文件"
    exit 1
fi

echo "[INFO] 准备上传: $IPA_FILE"

# 检查使用哪种上传方式

# 方式 1: App Store Connect API Key
if [ -n "$ASC_KEY_ID" ] && [ -n "$ASC_ISSUER_ID" ] && [ -n "$ASC_KEY_FILE" ]; then
    echo "[INFO] 使用 App Store Connect API Key 上传..."
    
    # 绕过代理 (Surge 等) 避免 SSL 错误
    export no_proxy="apple.com,*.apple.com,object-storage.apple.com"
    export NO_PROXY="apple.com,*.apple.com,object-storage.apple.com"
    unset http_proxy
    unset https_proxy
    unset HTTP_PROXY
    unset HTTPS_PROXY
    unset ALL_PROXY
    
    xcrun altool --upload-app \
        --type ios \
        --file "$IPA_FILE" \
        --apiKey "$ASC_KEY_ID" \
        --apiIssuer "$ASC_ISSUER_ID" || {
            echo "[ERROR] altool 上传失败"
            exit 1
        }
    
    echo "[SUCCESS] 上传成功!"
    exit 0
fi

# 方式 2: Fastlane
if command -v fastlane &> /dev/null; then
    echo "[INFO] 使用 Fastlane 上传..."
    
    # 检查是否配置了 fastlane
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
    
    if [ -f "$PROJECT_ROOT/fastlane/Fastfile" ]; then
        cd "$PROJECT_ROOT"
        
        # 使用 Fastlane pilot 上传
        fastlane pilot upload \
            --ipa "$IPA_FILE" \
            --skip_waiting_for_build_processing true || {
                echo "[ERROR] Fastlane 上传失败"
                exit 1
            }
        
        echo "[SUCCESS] Fastlane 上传成功!"
        exit 0
    else
        echo "[WARNING] 未找到 Fastfile，尝试直接使用 pilot..."
        fastlane pilot upload \
            --ipa "$IPA_FILE" \
            --skip_waiting_for_build_processing true || {
                echo "[ERROR] 上传失败"
                exit 1
            }
        exit 0
    fi
fi

# 方式 3: 使用 Apple ID (旧方式，需要输入密码)
echo "[WARNING] 未配置 API Key 或 Fastlane"
echo "[INFO] 请选择以下方式之一配置上传:"
echo ""
echo "1. App Store Connect API Key (推荐):"
echo "   export ASC_KEY_ID='YOUR_KEY_ID'"
echo "   export ASC_ISSUER_ID='YOUR_ISSUER_ID'"
echo "   export ASC_KEY_FILE='/path/to/AuthKey_XXXXX.p8'"
echo ""
echo "2. 安装 Fastlane:"
echo "   brew install fastlane"
echo "   cd $PROJECT_ROOT && fastlane init"
echo ""
echo "3. 手动上传:"
echo "   打开 Transporter.app 并拖入: $IPA_FILE"
echo ""

# 打开 Finder 显示 IPA 文件
open -R "$IPA_FILE"

exit 1
