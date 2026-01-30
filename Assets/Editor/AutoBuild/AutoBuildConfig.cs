using UnityEngine;

namespace AutoBuild
{
    /// <summary>
    /// 自动构建配置 - 存储项目特定的构建设置
    /// 即插即用：复制 AutoBuild 文件夹到任意 Unity 项目即可使用
    /// </summary>
    [CreateAssetMenu(fileName = "AutoBuildConfig", menuName = "AutoBuild/Config")]
    public class AutoBuildConfig : ScriptableObject
    {
        [Header("=== 输出路径 ===")]
        [Tooltip("iOS Xcode 工程输出路径 (相对于项目根目录)")]
        public string iosBuildPath = "Builds/iOS";
        
        [Tooltip("Android APK 输出路径 (相对于项目根目录)")]
        public string androidBuildPath = "Builds/Android";

        [Header("=== Android 签名配置 ===")]
        [Tooltip("使用 PlayerSettings 中的 keystore 配置")]
        public bool usePlayerSettingsKeystore = true;
        
        [Tooltip("自定义 keystore 路径 (仅当 usePlayerSettingsKeystore = false 时生效)")]
        public string keystorePath = "";
        
        [Tooltip("keystore 别名")]
        public string keyAlias = "";

        [Header("=== 通知配置 ===")]
        [Tooltip("Telegram Bot Token (留空则不发送通知)")]
        public string telegramBotToken = "";
        
        [Tooltip("Telegram Chat ID")]
        public string telegramChatId = "";

        [Header("=== 高级选项 ===")]
        [Tooltip("构建前是否自增版本号")]
        public bool autoIncrementBuildNumber = true;
        
        [Tooltip("构建完成后是否自动打开输出目录")]
        public bool openOutputFolderOnComplete = true;
        
        [Tooltip("iOS 构建选项")]
        public iOSBuildOptions iosBuildOptions = new iOSBuildOptions();
        
        [Tooltip("Android 构建选项")]
        public AndroidBuildOptions androidBuildOptions = new AndroidBuildOptions();

        [System.Serializable]
        public class iOSBuildOptions
        {
            [Tooltip("是否生成 Xcode 调试符号")]
            public bool generateXcodeSymbols = true;
            
            [Tooltip("开发版本构建 (允许调试)")]
            public bool developmentBuild = false;
        }

        [System.Serializable]
        public class AndroidBuildOptions
        {
            [Tooltip("构建 App Bundle (.aab) 而非 APK")]
            public bool buildAppBundle = false;
            
            [Tooltip("开发版本构建 (允许调试)")]
            public bool developmentBuild = false;
            
            [Tooltip("分架构构建 (armv7 + arm64 分开)")]
            public bool splitByArchitecture = false;
        }
        
        /// <summary>
        /// 获取项目根目录的绝对路径
        /// </summary>
        public static string ProjectRoot => System.IO.Path.GetDirectoryName(Application.dataPath);
        
        /// <summary>
        /// 获取 iOS 构建输出的绝对路径
        /// </summary>
        public string GetIOSBuildAbsolutePath() => System.IO.Path.Combine(ProjectRoot, iosBuildPath);
        
        /// <summary>
        /// 获取 Android 构建输出的绝对路径
        /// </summary>
        public string GetAndroidBuildAbsolutePath() => System.IO.Path.Combine(ProjectRoot, androidBuildPath);
    }
}
