#!/bin/bash
# =============================================================================
# Unity CI/CD 自动构建主入口
# 即插即用：将此文件夹复制到任意 Unity 项目即可使用
# 
# 用法:
#   ./build.sh ios          # 构建 iOS 并上传 TestFlight
#   ./build.sh android      # 构建 Android 并上传 Google Drive
#   ./build.sh ios --unity-only    # 仅执行 Unity 构建 (不打包/上传)
#   ./build.sh ios --xcode-only    # 仅执行 Xcode 打包 (不上传)
# =============================================================================

set -e  # 遇到错误立即退出

# 脚本所在目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# 项目根目录 (脚本上一级)
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# 日志目录
LOG_DIR="$PROJECT_ROOT/Logs"
mkdir -p "$LOG_DIR"

# 时间戳
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 自动检测 Unity 安装路径
detect_unity() {
    if [ -n "$UNITY_PATH" ]; then
        log_info "使用环境变量 UNITY_PATH: $UNITY_PATH"
        return
    fi
    
    # 从 ProjectVersion.txt 获取项目 Unity 版本
    local version_file="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
    if [ -f "$version_file" ]; then
        local project_version=$(grep "m_EditorVersion:" "$version_file" | cut -d' ' -f2)
        log_info "项目 Unity 版本: $project_version"
        
        # 尝试匹配已安装的版本
        local installed_unity="/Applications/Unity/Hub/Editor/$project_version/Unity.app"
        if [ -d "$installed_unity" ]; then
            UNITY_PATH="$installed_unity"
            log_info "找到匹配的 Unity: $UNITY_PATH"
            return
        fi
    fi
    
    # 回退: 使用最新安装的版本
    UNITY_PATH=$(find /Applications/Unity/Hub/Editor -maxdepth 2 -name "Unity.app" 2>/dev/null | sort -V | tail -1)
    
    if [ -z "$UNITY_PATH" ]; then
        log_error "未找到 Unity 安装，请设置 UNITY_PATH 环境变量"
        exit 1
    fi
    
    log_warning "使用默认 Unity: $UNITY_PATH"
}

# 预检查: 磁盘空间
check_disk_space() {
    local required_mb=5000  # 需要至少 5GB 可用空间
    local available_mb=$(df -m "$PROJECT_ROOT" | awk 'NR==2 {print $4}')
    
    if [ "$available_mb" -lt "$required_mb" ]; then
        log_error "磁盘空间不足! 需要至少 ${required_mb}MB，当前可用: ${available_mb}MB"
        exit 1
    fi
    log_info "磁盘空间检查通过 (可用: ${available_mb}MB)"
}

# 预检查: Android 签名配置
check_android_signing() {
    if [ -z "$KEYSTORE_PASSWORD" ]; then
        log_warning "未设置 KEYSTORE_PASSWORD 环境变量，将使用项目默认配置"
    fi
}

# 自动关闭正在运行的 Unity 编辑器
close_unity_if_running() {
    # 检测是否有 Unity 进程正在使用此项目
    local unity_pid=$(pgrep -f "Unity.*-projectPath.*$(basename "$PROJECT_ROOT")" 2>/dev/null || true)
    
    if [ -n "$unity_pid" ]; then
        log_warning "检测到 Unity 正在运行此项目 (PID: $unity_pid)"
        log_info "正在自动关闭 Unity 编辑器..."
        
        # 优雅关闭 (给 Unity 5秒保存)
        kill -TERM $unity_pid 2>/dev/null || true
        sleep 3
        
        # 如果还在运行，强制关闭
        if ps -p $unity_pid > /dev/null 2>&1; then
            log_warning "Unity 未响应，强制关闭..."
            kill -9 $unity_pid 2>/dev/null || true
            sleep 2
        fi
        
        log_success "Unity 已关闭"
    fi
}

# 显示帮助
show_help() {
    echo "用法: $0 <platform> [options]"
    echo ""
    echo "平台:"
    echo "  ios        构建 iOS 并上传 TestFlight"
    echo "  android    构建 Android 并上传 Google Drive"
    echo "  all        构建两个平台"
    echo ""
    echo "选项:"
    echo "  --unity-only    仅执行 Unity 构建 (不打包/上传)"
    echo "  --xcode-only    仅执行 Xcode 打包 (不上传，仅 iOS)"
    echo "  --no-upload     构建完成但不上传"
    echo "  --dry-run       空运行，仅打印命令"
    echo "  --help          显示此帮助"
    echo ""
    echo "环境变量:"
    echo "  UNITY_PATH      Unity.app 路径 (可选，默认自动检测)"
    echo ""
    echo "示例:"
    echo "  $0 ios"
    echo "  $0 android --unity-only"
    echo "  UNITY_PATH=/Applications/Unity/Hub/Editor/2021.1.7f1/Unity.app $0 ios"
}

# 构建 iOS
build_ios() {
    log_info "=== 开始 iOS 构建流程 ==="
    
    local log_file="$LOG_DIR/build_ios_$TIMESTAMP.log"
    log_info "日志文件: $log_file"
    
    # Step 1: Unity 导出 Xcode 工程
    if [ "$XCODE_ONLY" != "true" ]; then
        log_info "Step 1/3: Unity 导出 Xcode 工程..."
        
        "$UNITY_PATH/Contents/MacOS/Unity" \
            -quit \
            -batchmode \
            -nographics \
            -projectPath "$PROJECT_ROOT" \
            -executeMethod "AutoBuild.AutoBuildScript.BuildIOS" \
            -logFile "$log_file" || {
                log_error "Unity 构建失败，查看日志: $log_file"
                tail -50 "$log_file"
                exit 1
            }
        
        log_success "Unity Xcode 导出完成"
    fi
    
    if [ "$UNITY_ONLY" == "true" ]; then
        log_success "Unity 构建完成 (--unity-only 模式)"
        return
    fi
    
    # Step 2: Xcode 打包
    log_info "Step 2/3: Xcode 打包..."
    "$SCRIPT_DIR/ios_build.sh" "$PROJECT_ROOT/Builds/iOS" || {
        log_error "Xcode 打包失败"
        exit 1
    }
    
    if [ "$NO_UPLOAD" == "true" ]; then
        log_success "构建完成 (--no-upload 模式)"
        return
    fi
    
    # Step 3: 上传 TestFlight
    log_info "Step 3/3: 上传 TestFlight..."
    "$SCRIPT_DIR/upload_testflight.sh" "$PROJECT_ROOT/Builds/iOS" || {
        log_error "TestFlight 上传失败"
        exit 1
    }
    
    # 发送通知
    "$SCRIPT_DIR/notify.sh" "🍎 iOS 构建完成并已上传 TestFlight"
    
    log_success "=== iOS 构建流程完成 ==="
}

# 构建 Android
build_android() {
    log_info "=== 开始 Android 构建流程 ==="
    
    # 加载环境变量
    if [ -f "$SCRIPT_DIR/.env" ]; then
        source "$SCRIPT_DIR/.env"
    fi
    
    # 传递 keystore 密码给 Unity
    export KEYSTORE_PASSWORD
    export KEY_PASSWORD="${KEY_PASSWORD:-$KEYSTORE_PASSWORD}"
    
    local log_file="$LOG_DIR/build_android_$TIMESTAMP.log"
    log_info "日志文件: $log_file"
    
    # Step 1: Unity 构建 APK
    log_info "Step 1/2: Unity 构建 APK..."
    
    "$UNITY_PATH/Contents/MacOS/Unity" \
        -quit \
        -batchmode \
        -nographics \
        -projectPath "$PROJECT_ROOT" \
        -executeMethod "AutoBuild.AutoBuildScript.BuildAndroid" \
        -logFile "$log_file" || {
            log_error "Unity 构建失败，查看日志: $log_file"
            tail -50 "$log_file"
            exit 1
        }
    
    log_success "APK 构建完成"
    
    # 查找生成的 APK 文件
    APK_FILE=$(find "$PROJECT_ROOT/Builds/Android" -name "*.apk" -type f -mmin -5 2>/dev/null | head -1)
    
    if [ -z "$APK_FILE" ]; then
        log_error "未找到新生成的 APK 文件"
        exit 1
    fi
    
    APK_NAME=$(basename "$APK_FILE")
    APK_SIZE=$(($(stat -f%z "$APK_FILE" 2>/dev/null || stat -c%s "$APK_FILE") / 1024 / 1024))
    log_info "APK 文件: $APK_NAME ($APK_SIZE MB)"
    
    if [ "$UNITY_ONLY" == "true" ] || [ "$NO_UPLOAD" == "true" ]; then
        log_success "构建完成 (跳过上传)"
        "$SCRIPT_DIR/notify.sh" "🤖 Android APK 构建完成

📦 文件: $APK_NAME
💾 大小: ${APK_SIZE} MB"
        return
    fi
    
    # Step 2: 上传 R2
    log_info "Step 2/2: 上传 R2..."
    "$SCRIPT_DIR/upload_r2.sh" "$APK_FILE" || {
        log_warning "R2 上传失败"
        "$SCRIPT_DIR/notify.sh" "⚠️ Android APK 构建成功但上传失败

📦 文件: $APK_NAME
💾 大小: ${APK_SIZE} MB"
        return
    }
    
    # 读取下载链接
    DOWNLOAD_URL=$(cat /tmp/last_r2_url.txt 2>/dev/null || echo "")
    
    # 发送通知
    "$SCRIPT_DIR/notify.sh" "🤖 Android APK 构建并上传成功

📦 文件: $APK_NAME
💾 大小: ${APK_SIZE} MB
📥 下载: $DOWNLOAD_URL"
    
    log_success "=== Android 构建流程完成 ==="
}

# 主入口
main() {
    # 解析参数
    PLATFORM=""
    UNITY_ONLY="false"
    XCODE_ONLY="false"
    NO_UPLOAD="false"
    DRY_RUN="false"
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            ios|android|all)
                PLATFORM="$1"
                shift
                ;;
            --unity-only)
                UNITY_ONLY="true"
                shift
                ;;
            --xcode-only)
                XCODE_ONLY="true"
                shift
                ;;
            --no-upload)
                NO_UPLOAD="true"
                shift
                ;;
            --dry-run)
                DRY_RUN="true"
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                log_error "未知参数: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    if [ -z "$PLATFORM" ]; then
        log_error "请指定构建平台"
        show_help
        exit 1
    fi
    
    # 检测 Unity
    detect_unity
    
    log_info "项目路径: $PROJECT_ROOT"
    log_info "Unity 路径: $UNITY_PATH"
    
    # 预检查
    check_disk_space
    close_unity_if_running
    
    # 执行构建
    case $PLATFORM in
        ios)
            build_ios
            ;;
        android)
            build_android
            ;;
        all)
            build_ios
            build_android
            ;;
    esac
}

main "$@"
