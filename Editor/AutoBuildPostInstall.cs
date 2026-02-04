using UnityEditor;
using UnityEngine;

namespace AutoBuild
{
    /// <summary>
    /// 首次安装后自动弹出配置面板
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBuildPostInstall
    {
        private const string INSTALL_KEY = "AutoBuild_Installed_v1";
        
        static AutoBuildPostInstall()
        {
            // 延迟执行，确保编辑器完全初始化
            EditorApplication.delayCall += CheckFirstInstall;
        }
        
        private static void CheckFirstInstall()
        {
            // 检查是否首次安装
            if (!EditorPrefs.GetBool(INSTALL_KEY, false))
            {
                EditorPrefs.SetBool(INSTALL_KEY, true);
                
                // 显示欢迎弹窗
                bool openSetup = EditorUtility.DisplayDialog(
                    "Unity AutoBuild 安装成功! 🎉",
                    "感谢使用 Unity AutoBuild!\n\n" +
                    "请先配置以下内容:\n" +
                    "• Telegram Bot Token (通知)\n" +
                    "• Android Keystore 密码 (签名)\n" +
                    "• R2/TestFlight 配置 (上传)\n\n" +
                    "点击「开始配置」打开设置面板。",
                    "开始配置",
                    "稍后配置"
                );
                
                if (openSetup)
                {
                    AutoBuildWindow.ShowWindow();
                }
                
                // 安装 CLI 脚本
                InstallCLIScripts();
            }
        }
        
        /// <summary>
        /// 安装 CLI 脚本到项目根目录
        /// </summary>
        private static void InstallCLIScripts()
        {
            // 获取包路径
            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath)) return;
            
            string cliSourcePath = System.IO.Path.Combine(packagePath, "CLI~");
            string projectRoot = AutoBuildConfig.ProjectRoot;
            string ciPath = System.IO.Path.Combine(projectRoot, ".ci");
            
            // 创建 .ci 目录
            if (!System.IO.Directory.Exists(ciPath))
            {
                System.IO.Directory.CreateDirectory(ciPath);
            }
            
            // 复制脚本
            if (System.IO.Directory.Exists(cliSourcePath))
            {
                foreach (var file in System.IO.Directory.GetFiles(cliSourcePath))
                {
                    var destFile = System.IO.Path.Combine(ciPath, System.IO.Path.GetFileName(file));
                    if (!System.IO.File.Exists(destFile))
                    {
                        System.IO.File.Copy(file, destFile);
                        Debug.Log($"[AutoBuild] 已安装: {destFile}");
                    }
                }
                
                // 设置执行权限 (macOS/Linux)
                #if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                foreach (var file in System.IO.Directory.GetFiles(ciPath, "*.sh"))
                {
                    System.Diagnostics.Process.Start("chmod", $"+x \"{file}\"");
                }
                
                // 安装全局 build 命令
                InstallGlobalBuildCommand(ciPath);
                #endif
                
                Debug.Log("[AutoBuild] CLI 脚本已安装到 .ci/ 目录");
            }
        }
        
        /// <summary>
        /// 安装全局 build 命令到 ~/.local/bin
        /// </summary>
        private static void InstallGlobalBuildCommand(string ciPath)
        {
            string homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string localBinPath = System.IO.Path.Combine(homeDir, ".local", "bin");
            string globalBuildPath = System.IO.Path.Combine(localBinPath, "build");
            
            // 创建目录
            if (!System.IO.Directory.Exists(localBinPath))
            {
                System.IO.Directory.CreateDirectory(localBinPath);
            }
            
            // 创建全局 build 脚本
            string buildScript = @"#!/bin/bash
# Unity AutoBuild 全局命令
# 自动检测项目根目录并调用 .ci/build.sh

find_project_root() {
    local dir=""$PWD""
    while [[ ""$dir"" != ""/"" ]]; do
        if [[ -d ""$dir/ProjectSettings"" && -d ""$dir/Assets"" ]]; then
            echo ""$dir""
            return 0
        fi
        dir=""$(dirname ""$dir"")""
    done
    return 1
}

PROJECT_ROOT=$(find_project_root)

if [[ -z ""$PROJECT_ROOT"" ]]; then
    echo ""❌ 错误: 当前目录不在 Unity 项目中""
    exit 1
fi

BUILD_SCRIPT=""$PROJECT_ROOT/.ci/build.sh""

if [[ ! -f ""$BUILD_SCRIPT"" ]]; then
    echo ""❌ 错误: 未找到 .ci/build.sh""
    exit 1
fi

cd ""$PROJECT_ROOT""
exec ""$BUILD_SCRIPT"" ""$@""
";
            
            System.IO.File.WriteAllText(globalBuildPath, buildScript);
            System.Diagnostics.Process.Start("chmod", $"+x \"{globalBuildPath}\"");
            
            Debug.Log($"[AutoBuild] 全局 build 命令已安装到 {globalBuildPath}");
            Debug.Log("[AutoBuild] 如果 ~/.local/bin 不在 PATH 中，请添加: export PATH=\"$HOME/.local/bin:$PATH\"");
        }
        
        private static string GetPackagePath()
        {
            // 尝试查找包路径
            var guids = AssetDatabase.FindAssets("t:Script AutoBuildPostInstall");
            if (guids.Length > 0)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                // 返回包根目录 (Editor 的上一级)
                return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(scriptPath));
            }
            return null;
        }
        

    }
}
