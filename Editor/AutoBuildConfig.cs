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
        [Tooltip("Keystore 文件路径 (绝对路径或相对于项目根目录)")]
        public string keystorePath = "";
        
        [Tooltip("Keystore 密码")]
        public string keystorePassword = "";
        
        [Tooltip("Key 别名")]
        public string keyAliasName = "";
        
        [Tooltip("Key 密码 (留空则使用 Keystore 密码)")]
        public string keyAliasPassword = "";

        [Header("=== 通知配置 ===")]
        [Tooltip("Telegram Bot Token (留空则不发送通知)")]
        public string telegramBotToken = "";
        
        [Tooltip("Telegram Chat ID")]
        public string telegramChatId = "";

        [Header("=== R2 上传配置 ===")]
        [Tooltip("R2 Uploader 服务地址 (例如: https://pan-temp.your-domain.com)")]
        public string r2UploaderUrl = "";
        
        [Header("=== App Store Connect API Key (TestFlight) ===")]
        [Tooltip("Key ID (例如: D383SF739)")]
        public string ascKeyId = "";
        
        [Tooltip("Issuer ID (例如: 69a6de78-xxx...)")]
        public string ascIssuerId = "";
        
        [Tooltip("AuthKey 文件路径 (.p8)")]
        public string ascKeyFilePath = "";

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
