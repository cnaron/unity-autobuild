# Unity AutoBuild

一键式 Unity CI/CD 自动构建系统。

[![Unity 2019.4+](https://img.shields.io/badge/Unity-2019.4+-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## ✨ 功能特性

- 🍎 **iOS**: Unity → Xcode → IPA → TestFlight (一键完成)
- 🤖 **Android**: Unity → APK → R2/Google Drive (一键完成)  
- 📱 **Telegram 通知**: 构建完成后自动推送通知和下载链接
- 🔐 **自动签名**: Keystore 密码配置，无需每次输入
- 🚀 **自动化**: 自动关闭 Unity 编辑器，磁盘空间检查

---

## 📦 安装

### 方式 A: Unity Package Manager (推荐)

1. 打开 Unity
2. 菜单: `Window` → `Package Manager`
3. 点击 `+` → `Add package from git URL...`
4. 输入:

```
https://github.com/cnaron/unity-autobuild.git
```

5. 安装后会自动弹出配置面板

### 方式 B: 手动安装

```bash
git clone https://github.com/cnaron/unity-autobuild.git
# 复制 Editor/ 到你的项目 Assets/Editor/AutoBuild/
# 复制 CLI~/ 内容到你的项目 .ci/
```

---

## ⚙️ 配置

安装后首次打开会自动弹出配置面板，或通过菜单打开:

`Tools` → `AutoBuild` → `配置面板`

### 必要配置

| 配置项 | 说明 |
|-------|------|
| **Telegram Bot Token** | 从 @BotFather 获取 |
| **Telegram Chat ID** | 你的用户 ID |
| **Keystore 密码** | Android 签名密码 |

### 可选配置

- R2 Uploader URL (Android 上传)
- App Store Connect API Key (TestFlight)

---

## 🚀 使用

### 命令行 (推荐)

首次使用需安装全局命令:

```bash
# 安装 build 命令
~/.local/share/unity/com.cnaron.autobuild/CLI~/install-cli.sh
```

之后在任意 Unity 项目目录下:

```bash
build ios          # 完整 iOS 流程
build android      # 完整 Android 流程
build ios --unity-only    # 仅导出 Xcode 工程
build android --no-upload # 仅构建不上传
```

### Unity 编辑器

`Tools` → `AutoBuild` → `构建 iOS/Android`

---

## 📁 目录结构

安装后会在项目创建 `.ci/` 目录:

```
YourUnityProject/
├── .ci/
│   ├── .env              # 环境变量配置 (自动创建)
│   ├── build.sh          # 主构建脚本
│   ├── ios_build.sh      # Xcode 打包
│   ├── upload_testflight.sh
│   ├── upload_r2.sh
│   └── notify.sh
└── ...
```

---

## 📋 环境变量 (.ci/.env)

```bash
# Telegram 通知
TELEGRAM_BOT_TOKEN=your_bot_token
TELEGRAM_CHAT_ID=your_chat_id

# R2 上传
R2_UPLOADER_URL=https://your-r2-worker.workers.dev

# Android 签名
KEYSTORE_PASSWORD=your_password

# TestFlight (可选)
ASC_KEY_ID=your_key_id
ASC_ISSUER_ID=your_issuer_id
ASC_KEY_FILE=/path/to/AuthKey.p8
```

---

## 🔧 自动化特性

- **自动关闭 Unity**: CLI 构建前自动关闭正在运行的编辑器
- **磁盘空间检查**: 构建前检查可用空间 (需 5GB+)
- **代理绕过**: 上传时自动绕过系统代理
- **版本号管理**: iOS 自动递增，Android 使用固定格式

---

## 📄 License

MIT License
