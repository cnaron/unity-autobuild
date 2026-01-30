# Unity AutoBuild

一键式 Unity CI/CD 自动构建系统，支持 iOS 和 Android 平台的自动打包、上传和通知。

## ✨ 特性

- 🍎 **iOS**: Unity → Xcode → IPA → TestFlight (全自动)
- 🤖 **Android**: Unity → APK/AAB → Google Drive (全自动)
- 📱 **Telegram 通知**: 构建完成自动推送
- 🔄 **版本自动管理**: 外部版本 = 当前日期 (yyyy.MM.dd)，内部版本自动递增
- 🚀 **代理绕过**: TestFlight 上传自动绕过代理，无需手动关闭 VPN
- 📦 **即插即用**: 复制到任意 Unity 项目即可使用

## 📁 目录结构

```
项目根目录/
├── Assets/
│   └── Editor/
│       └── AutoBuild/
│           ├── AutoBuildConfig.cs     # 配置 ScriptableObject
│           ├── AutoBuildScript.cs     # CLI 构建入口
│           └── AutoBuildWindow.cs     # 编辑器窗口
└── .ci/
    ├── build.sh                       # 主构建脚本
    ├── ios_build.sh                   # Xcode 打包
    ├── upload_testflight.sh           # TestFlight 上传
    ├── upload_gdrive.sh               # Google Drive 上传
    ├── notify.sh                      # Telegram 通知
    └── .env.example                   # 环境变量模板
```

## 🚀 快速开始

### 1. 安装

**方式 A: 直接克隆 (推荐)**

```bash
cd 你的Unity项目根目录

# 克隆到临时目录并复制
git clone https://github.com/cnaron/unity-autobuild.git temp-autobuild
cp -r temp-autobuild/Assets/Editor/AutoBuild ./Assets/Editor/
cp -r temp-autobuild/.ci ./
rm -rf temp-autobuild
```

**方式 B: 手动下载**

1. 下载 [Release ZIP](https://github.com/cnaron/unity-autobuild/releases)
2. 解压并复制 `Assets/Editor/AutoBuild/` 到项目
3. 复制 `.ci/` 文件夹到项目根目录

> ⚠️ **安全说明**: 仓库不包含任何敏感信息，`.env.example` 是空模板。你需要创建自己的 `.ci/.env` 并填入你的 Token。

### 2. 安装依赖 (macOS)

```bash
brew install fastlane
# gdrive 可选，用于 Android 上传 Google Drive
```

### 3. 配置环境变量

```bash
cd 你的Unity项目
cp .ci/.env.example .ci/.env
# 编辑 .ci/.env 填入配置
```

### 4. 配置 App Store Connect API Key (iOS)

1. 访问 [App Store Connect API](https://appstoreconnect.apple.com/access/api)
2. 创建 API Key，下载 `.p8` 文件
3. 将 `.p8` 文件复制到 `~/.private_keys/`
4. 在 `.env` 中配置:

```bash
ASC_KEY_ID=你的Key_ID
ASC_ISSUER_ID=你的Issuer_ID
```

### 5. 运行构建

```bash
# iOS 完整流程 (Unity → Xcode → IPA → TestFlight)
./.ci/build.sh ios

# Android 完整流程 (Unity → APK → Google Drive)
./.ci/build.sh android

# 仅导出 Xcode 工程
./.ci/build.sh ios --unity-only

# 从现有 Xcode 打包 (跳过 Unity)
./.ci/build.sh ios --xcode-only
```

## ⚙️ 配置参数

### 环境变量 (.ci/.env)

| 变量名 | 必需 | 说明 |
|--------|------|------|
| `TELEGRAM_BOT_TOKEN` | ✅ | Telegram Bot Token |
| `TELEGRAM_CHAT_ID` | ✅ | 接收通知的 Chat ID |
| `ASC_KEY_ID` | iOS | App Store Connect API Key ID |
| `ASC_ISSUER_ID` | iOS | App Store Connect Issuer ID |
| `GDRIVE_FOLDER_ID` | Android | Google Drive 目标文件夹 ID |

### Unity 编辑器配置

打开 **Tools → AutoBuild → Build Window** 可视化配置:

- **构建输出路径**: iOS/Android 构建输出目录
- **自动递增版本号**: 每次构建自动 +1
- **Development Build**: 是否包含调试符号
- **App Bundle (AAB)**: Android 是否生成 AAB 格式

## 🔧 命令参数

```bash
./.ci/build.sh <platform> [options]

平台:
  ios          构建 iOS
  android      构建 Android
  all          同时构建两个平台

选项:
  --unity-only   仅执行 Unity 导出，不打包不上传
  --xcode-only   仅执行 Xcode 打包 (跳过 Unity)
  --no-upload    构建后不上传
  --dry-run      测试模式，不实际执行
  --help         显示帮助
```

## 📝 版本号规则

- **外部版本 (Version)**: 自动设置为当前日期 `yyyy.MM.dd`
- **内部版本 (Build Number)**: 每次构建自动递增

例如: 版本 `2026.01.30`，构建号 `42`

## 🔔 Telegram 通知示例

```
[项目名] 🚀 iOS 构建并上传 TestFlight 成功！

📦 版本: 2026.01.30
🔢 构建号: 42
⏱️ 上传速度: 8.0 MB/s

⏰ 2026-01-30 14:16:47
```

## ⚠️ 注意事项

1. **CLI 构建需要关闭 Unity 编辑器** - 同一项目不能被两个 Unity 实例打开
2. **TestFlight 上传已配置代理绕过** - 无需手动关闭 Surge/Clash 等
3. **iOS 签名**: 默认使用自动签名，确保 Xcode 已配置好证书
4. **Android 签名**: 使用 PlayerSettings 中的 keystore 配置

## 📄 License

MIT License
