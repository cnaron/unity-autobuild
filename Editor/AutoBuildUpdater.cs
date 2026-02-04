using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine.Networking;
using System;
using System.Linq;

namespace AutoBuild
{
    /// <summary>
    /// AutoBuild 自动更新检查器
    /// </summary>
    public static class AutoBuildUpdater
    {
        private const string GITHUB_REPO = "cnaron/unity-autobuild";
        private const string PACKAGE_NAME = "com.cnaron.autobuild";
        private const string GITHUB_API_URL = "https://api.github.com/repos/cnaron/unity-autobuild/commits/main";
        
        public static string CurrentVersion { get; private set; } = "Unknown";
        public static string LatestVersion { get; private set; } = "Unknown";
        public static string CurrentCommitHash { get; private set; }
        public static bool IsChecking { get; private set; }
        public static bool HasUpdate { get; private set; }
        public static string LastCheckTime { get; private set; }

        public static event Action OnCheckComplete;

        /// <summary>
        /// 检查更新
        /// </summary>
        public static void CheckForUpdates()
        {
            if (IsChecking) return;
            IsChecking = true;
            
            // 1. 获取本地版本信息
            GetLocalPackageInfo();
            
            // 2. 获取远程版本信息
            CheckRemoteVersion();
        }

        private static void GetLocalPackageInfo()
        {
            var listRequest = Client.List(true);
            EditorApplication.update += () =>
            {
                if (listRequest.IsCompleted)
                {
                    if (listRequest.Status == StatusCode.Success)
                    {
                        var package = listRequest.Result.FirstOrDefault(p => p.name == PACKAGE_NAME);
                        if (package != null)
                        {
                            CurrentVersion = package.version;
                            // 尝试从 packageId 中获取 commit hash (git 依赖通常包含 hash)
                            if (package.packageId.Contains(".git#"))
                            {
                                CurrentCommitHash = package.packageId.Split('#').Last();
                                if (CurrentCommitHash.Length > 7) 
                                    CurrentCommitHash = CurrentCommitHash.Substring(0, 7);
                            }
                            else
                            {
                                CurrentCommitHash = "Unknown";
                            }
                        }
                    }
                    EditorApplication.update -= null; // 移除回调的trick，实际需要正确移除
                }
            };
            
            // 下面的实现方式更稳妥
            EditorApplication.CallbackFunction progressCallback = null;
            progressCallback = () =>
            {
                if (listRequest.IsCompleted)
                {
                    EditorApplication.update -= progressCallback;
                    if (listRequest.Status == StatusCode.Success)
                    {
                        var package = listRequest.Result.FirstOrDefault(p => p.name == PACKAGE_NAME);
                        if (package != null)
                        {
                            CurrentVersion = package.version;
                            if (package.git != null && !string.IsNullOrEmpty(package.git.hash))
                            {
                                CurrentCommitHash = package.git.hash.Substring(0, 7);
                            }
                        }
                    }
                }
            };
            EditorApplication.update += progressCallback;
        }

        private static void CheckRemoteVersion()
        {
            var request = UnityWebRequest.Get(GITHUB_API_URL);
            request.timeout = 10;
            
            var operation = request.SendWebRequest();
            
            EditorApplication.CallbackFunction webRequestCallback = null;
            webRequestCallback = () =>
            {
                if (operation.isDone)
                {
                    EditorApplication.update -= webRequestCallback;
                    IsChecking = false;
                    LastCheckTime = DateTime.Now.ToString("HH:mm:ss");

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var json = request.downloadHandler.text;
                            var commitInfo = JsonUtility.FromJson<GitHubCommit>(json);
                            
                            if (commitInfo != null)
                            {
                                LatestVersion = commitInfo.sha.Substring(0, 7);
                                
                                // 对比 Hash
                                if (!string.IsNullOrEmpty(CurrentCommitHash) && 
                                    CurrentCommitHash != "Unknown" && 
                                    LatestVersion != CurrentCommitHash)
                                {
                                    HasUpdate = true;
                                }
                                else
                                {
                                    HasUpdate = false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[AutoBuild] 解析更新信息失败: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AutoBuild] 检查更新失败: {request.error}");
                    }
                    
                    OnCheckComplete?.Invoke();
                    request.Dispose();
                }
            };
            EditorApplication.update += webRequestCallback;
        }

        /// <summary>
        /// 执行更新
        /// </summary>
        public static void UpdatePackage()
        {
            // 通过重新 Add git URL 来强制更新到最新 HEAD
            // 注意：必须完全匹配安装时的 URL
            Client.Add($"https://github.com/{GITHUB_REPO}.git");
        }
        
        [Serializable]
        private class GitHubCommit
        {
            public string sha;
        }
    }
}
