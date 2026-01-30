#!/bin/bash
# =============================================================================
# iOS Xcode 打包脚本
# 用法: ./ios_build.sh <xcode_project_path>
# =============================================================================

set -e

XCODE_PATH="$1"

if [ -z "$XCODE_PATH" ]; then
    echo "用法: $0 <xcode_project_path>"
    exit 1
fi

# 检查 Xcode 工程是否存在
if [ ! -d "$XCODE_PATH/Unity-iPhone.xcodeproj" ]; then
    echo "[ERROR] 未找到 Xcode 工程: $XCODE_PATH/Unity-iPhone.xcodeproj"
    exit 1
fi

# 输出目录
BUILD_DIR="$XCODE_PATH/build"
ARCHIVE_PATH="$BUILD_DIR/Unity-iPhone.xcarchive"
IPA_PATH="$BUILD_DIR/ipa"

mkdir -p "$BUILD_DIR"

echo "[INFO] Xcode 工程路径: $XCODE_PATH"
echo "[INFO] Archive 输出: $ARCHIVE_PATH"
echo "[INFO] IPA 输出: $IPA_PATH"

# 创建 ExportOptions.plist (如果不存在)
EXPORT_OPTIONS_PLIST="$XCODE_PATH/ExportOptions.plist"
if [ ! -f "$EXPORT_OPTIONS_PLIST" ]; then
    echo "[INFO] 创建默认 ExportOptions.plist..."
    cat > "$EXPORT_OPTIONS_PLIST" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>app-store</string>
    <key>uploadSymbols</key>
    <true/>
    <key>compileBitcode</key>
    <false/>
    <key>signingStyle</key>
    <string>automatic</string>
</dict>
</plist>
EOF
fi

# Step 1: Archive
echo "[INFO] Step 1: 执行 Archive..."

xcodebuild archive \
    -project "$XCODE_PATH/Unity-iPhone.xcodeproj" \
    -scheme "Unity-iPhone" \
    -configuration "Release" \
    -archivePath "$ARCHIVE_PATH" \
    -destination "generic/platform=iOS" \
    CODE_SIGN_STYLE=Automatic \
    -allowProvisioningUpdates || {
        echo "[ERROR] Archive 失败"
        exit 1
    }

echo "[SUCCESS] Archive 完成"

# Step 2: Export IPA
echo "[INFO] Step 2: 导出 IPA..."

xcodebuild -exportArchive \
    -archivePath "$ARCHIVE_PATH" \
    -exportPath "$IPA_PATH" \
    -exportOptionsPlist "$EXPORT_OPTIONS_PLIST" \
    -allowProvisioningUpdates || {
        echo "[ERROR] Export 失败"
        exit 1
    }

echo "[SUCCESS] IPA 导出完成"

# 显示输出文件
IPA_FILE=$(find "$IPA_PATH" -name "*.ipa" | head -1)
if [ -n "$IPA_FILE" ]; then
    echo "[SUCCESS] IPA 文件: $IPA_FILE"
    echo "[INFO] 文件大小: $(du -h "$IPA_FILE" | cut -f1)"
    
    # 保存 IPA 路径供后续脚本使用
    echo "$IPA_FILE" > "$BUILD_DIR/last_ipa_path.txt"
else
    echo "[ERROR] 未找到生成的 IPA 文件"
    exit 1
fi
