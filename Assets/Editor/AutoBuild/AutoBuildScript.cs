using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace AutoBuild
{
    /// <summary>
    /// 自动构建脚本 - CLI 入口
    /// 即插即用：复制 AutoBuild 文件夹到任意 Unity 项目即可使用
    /// 
    /// CLI 用法:
    /// Unity -quit -batchmode -projectPath /path/to/project -executeMethod AutoBuild.AutoBuildScript.BuildIOS
    /// Unity -quit -batchmode -projectPath /path/to/project -executeMethod AutoBuild.AutoBuildScript.BuildAndroid
    /// </summary>
    public static class AutoBuildScript
    {
        private const string CONFIG_PATH = "Assets/Editor/AutoBuild/AutoBuildConfig.asset";
        private const string DEFAULT_IOS_PATH = "Builds/iOS";
        private const string DEFAULT_ANDROID_PATH = "Builds/Android";

        #region Public API - CLI Entry Points

        /// <summary>
        /// 构建 iOS (导出 Xcode 工程)
        /// CLI: -executeMethod AutoBuild.AutoBuildScript.BuildIOS
        /// </summary>
        public static void BuildIOS()
        {
            Log("=== 开始 iOS 构建 ===");
            
            var config = LoadOrCreateConfig();
            var outputPath = config?.GetIOSBuildAbsolutePath() ?? Path.Combine(ProjectRoot, DEFAULT_IOS_PATH);
            
            // 确保输出目录存在
            EnsureDirectoryExists(outputPath);
            
            // 可选：自增版本号
            if (config != null && config.autoIncrementBuildNumber)
            {
                IncrementBuildNumber(BuildTargetGroup.iOS);
            }
            
            // 构建选项
            var options = BuildOptions.None;
            if (config?.iosBuildOptions.developmentBuild == true)
            {
                options |= BuildOptions.Development;
            }
            
            // 获取启用的场景
            var scenes = GetEnabledScenes();
            Log($"构建场景: {string.Join(", ", scenes)}");
            Log($"输出路径: {outputPath}");
            Log($"Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)}");
            
            // 执行构建
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = options
            });
            
            HandleBuildReport(report, "iOS");
        }

        /// <summary>
        /// 构建 Android (生成 APK/AAB)
        /// CLI: -executeMethod AutoBuild.AutoBuildScript.BuildAndroid
        /// </summary>
        public static void BuildAndroid()
        {
            Log("=== 开始 Android 构建 ===");
            
            var config = LoadOrCreateConfig();
            var outputPath = config?.GetAndroidBuildAbsolutePath() ?? Path.Combine(ProjectRoot, DEFAULT_ANDROID_PATH);
            
            // 确保输出目录存在
            EnsureDirectoryExists(outputPath);
            
            // 可选：自增版本号
            if (config != null && config.autoIncrementBuildNumber)
            {
                IncrementBuildNumber(BuildTargetGroup.Android);
            }
            
            // 设置输出文件名
            var bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            var version = PlayerSettings.bundleVersion;
            var buildNumber = PlayerSettings.Android.bundleVersionCode;
            var extension = (config?.androidBuildOptions.buildAppBundle == true) ? "aab" : "apk";
            var fileName = $"{bundleId.Split('.').Last()}_{version}_{buildNumber}.{extension}";
            var fullPath = Path.Combine(outputPath, fileName);
            
            // 构建选项
            var options = BuildOptions.None;
            if (config?.androidBuildOptions.developmentBuild == true)
            {
                options |= BuildOptions.Development;
            }
            
            // App Bundle 设置
            EditorUserBuildSettings.buildAppBundle = config?.androidBuildOptions.buildAppBundle ?? false;
            
            // 获取启用的场景
            var scenes = GetEnabledScenes();
            Log($"构建场景: {string.Join(", ", scenes)}");
            Log($"输出路径: {fullPath}");
            Log($"Bundle ID: {bundleId}");
            
            // 执行构建
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullPath,
                target = BuildTarget.Android,
                options = options
            });
            
            HandleBuildReport(report, "Android");
        }

        /// <summary>
        /// 仅导出 Xcode 工程 (不进行后续处理)
        /// </summary>
        public static void ExportXcode()
        {
            BuildIOS();
        }

        #endregion

        #region Helper Methods

        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        private static AutoBuildConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AutoBuildConfig>(CONFIG_PATH);
            if (config == null)
            {
                Log($"未找到配置文件 {CONFIG_PATH}，使用默认设置");
            }
            return config;
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static void IncrementBuildNumber(BuildTargetGroup targetGroup)
        {
            // 设置外部版本号为当前日期 (yyyy.MM.dd)
            var dateVersion = DateTime.Now.ToString("yyyy.MM.dd");
            if (PlayerSettings.bundleVersion != dateVersion)
            {
                Log($"版本号: {PlayerSettings.bundleVersion} -> {dateVersion}");
                PlayerSettings.bundleVersion = dateVersion;
            }
            
            // 内部版本号递增
            if (targetGroup == BuildTargetGroup.iOS)
            {
                var current = int.TryParse(PlayerSettings.iOS.buildNumber, out var num) ? num : 0;
                PlayerSettings.iOS.buildNumber = (current + 1).ToString();
                Log($"iOS Build Number: {current} -> {current + 1}");
            }
            else if (targetGroup == BuildTargetGroup.Android)
            {
                var current = PlayerSettings.Android.bundleVersionCode;
                PlayerSettings.Android.bundleVersionCode = current + 1;
                Log($"Android Version Code: {current} -> {current + 1}");
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Log($"创建目录: {path}");
            }
        }

        private static void HandleBuildReport(BuildReport report, string platform)
        {
            var summary = report.summary;
            
            if (summary.result == BuildResult.Succeeded)
            {
                Log($"=== {platform} 构建成功 ===");
                Log($"总耗时: {summary.totalTime}");
                Log($"输出大小: {summary.totalSize / (1024 * 1024):F2} MB");
                Log($"输出路径: {summary.outputPath}");
                
                // 将输出路径写入文件，供 shell 脚本读取
                var outputInfoPath = Path.Combine(ProjectRoot, $"Logs/last_build_{platform.ToLower()}.txt");
                File.WriteAllText(outputInfoPath, summary.outputPath);
                
                EditorApplication.Exit(0);
            }
            else
            {
                LogError($"=== {platform} 构建失败 ===");
                LogError($"错误: {summary.result}");
                
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            LogError($"  {message.content}");
                        }
                    }
                }
                
                EditorApplication.Exit(1);
            }
        }

        private static void Log(string message)
        {
            Debug.Log($"[AutoBuild] {message}");
            // 同时输出到控制台 (CLI 模式下更容易看到)
            Console.WriteLine($"[AutoBuild] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[AutoBuild] {message}");
            Console.Error.WriteLine($"[AutoBuild] {message}");
        }

        #endregion
    }
}
