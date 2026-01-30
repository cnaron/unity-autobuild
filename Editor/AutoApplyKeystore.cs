using UnityEditor;
using UnityEngine;

namespace AutoBuild
{
    /// <summary>
    /// 自动应用 Android Keystore 配置
    /// 编辑器启动时自动从 AutoBuildConfig 读取并应用签名配置
    /// </summary>
    [InitializeOnLoad]
    public static class AutoApplyKeystore
    {
        static AutoApplyKeystore()
        {
            // 延迟执行，确保 AssetDatabase 已初始化
            EditorApplication.delayCall += ApplyKeystoreConfig;
        }
        
        /// <summary>
        /// 从 AutoBuildConfig 应用 Keystore 配置到 PlayerSettings
        /// </summary>
        public static void ApplyKeystoreConfig()
        {
            var config = LoadConfig();
            if (config == null) return;
            
            // 如果配置了 keystore 路径和密码，则自动应用
            if (!string.IsNullOrEmpty(config.keystorePath) && !string.IsNullOrEmpty(config.keystorePassword))
            {
                // 处理相对路径
                var keystorePath = config.keystorePath;
                if (!System.IO.Path.IsPathRooted(keystorePath))
                {
                    keystorePath = System.IO.Path.Combine(AutoBuildConfig.ProjectRoot, keystorePath);
                }
                
                // 检查文件是否存在
                if (!System.IO.File.Exists(keystorePath))
                {
                    Debug.LogWarning($"[AutoBuild] Keystore 文件不存在: {keystorePath}");
                    return;
                }
                
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = config.keystorePassword;
                
                if (!string.IsNullOrEmpty(config.keyAliasName))
                {
                    PlayerSettings.Android.keyaliasName = config.keyAliasName;
                    PlayerSettings.Android.keyaliasPass = 
                        string.IsNullOrEmpty(config.keyAliasPassword) 
                            ? config.keystorePassword 
                            : config.keyAliasPassword;
                }
                
                Debug.Log($"[AutoBuild] 已应用 Keystore 配置: {keystorePath}");
            }
        }
        
        private static AutoBuildConfig LoadConfig()
        {
            // 尝试从 Resources 加载
            var config = Resources.Load<AutoBuildConfig>("AutoBuildConfig");
            if (config != null) return config;
            
            // 尝试从 Editor 目录加载
            var guids = AssetDatabase.FindAssets("t:AutoBuildConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AutoBuildConfig>(path);
            }
            
            return null;
        }
        
        /// <summary>
        /// 编辑器菜单：手动应用 Keystore 配置
        /// </summary>
        [MenuItem("Tools/AutoBuild/Apply Keystore Config")]
        public static void ApplyKeystoreConfigMenu()
        {
            ApplyKeystoreConfig();
            Debug.Log("[AutoBuild] Keystore 配置已应用");
        }
    }
}
