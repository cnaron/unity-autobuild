using System.IO;
using UnityEngine;
using UnityEditor;

namespace AutoBuild
{
    /// <summary>
    /// 自动构建编辑器窗口 - 提供可视化操作界面
    /// 菜单: Tools/AutoBuild/Build Window
    /// </summary>
    public class AutoBuildWindow : EditorWindow
    {
        private AutoBuildConfig config;
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;
        
        [MenuItem("Tools/AutoBuild/Build Window", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoBuildWindow>("AutoBuild");
            window.minSize = new Vector2(400, 500);
        }
        
        [MenuItem("Tools/AutoBuild/Create Config", priority = 200)]
        public static void CreateConfig()
        {
            var path = "Assets/Editor/AutoBuild/AutoBuildConfig.asset";
            
            if (AssetDatabase.LoadAssetAtPath<AutoBuildConfig>(path) != null)
            {
                EditorUtility.DisplayDialog("提示", "配置文件已存在!", "OK");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<AutoBuildConfig>(path);
                return;
            }
            
            var config = ScriptableObject.CreateInstance<AutoBuildConfig>();
            
            // 确保目录存在
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = config;
            EditorUtility.DisplayDialog("成功", "配置文件已创建:\n" + path, "OK");
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            config = AssetDatabase.LoadAssetAtPath<AutoBuildConfig>(
                "Assets/Editor/AutoBuild/AutoBuildConfig.asset");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawProjectInfo();
            EditorGUILayout.Space(10);
            
            DrawBuildButtons();
            EditorGUILayout.Space(20);
            
            DrawConfigSection();
            EditorGUILayout.Space(10);
            
            DrawAdvancedOptions();
            EditorGUILayout.Space(10);
            
            DrawCLICommands();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🚀 AutoBuild", style, GUILayout.Height(30));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("即插即用的 Unity 自动构建工具", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawProjectInfo()
        {
            EditorGUILayout.LabelField("项目信息", EditorStyles.boldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("产品名称", PlayerSettings.productName);
                EditorGUILayout.LabelField("版本号", PlayerSettings.bundleVersion);
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("iOS", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Bundle ID", 
                        PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
                    EditorGUILayout.LabelField("Build Number", PlayerSettings.iOS.buildNumber);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Bundle ID", 
                        PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
                    EditorGUILayout.LabelField("Version Code", 
                        PlayerSettings.Android.bundleVersionCode.ToString());
                }
            }
        }

        private void DrawBuildButtons()
        {
            EditorGUILayout.LabelField("一键构建", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // iOS 构建按钮
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("🍎 构建 iOS", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("确认构建", 
                    $"即将构建 iOS 版本\n\n" +
                    $"Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)}\n" +
                    $"版本: {PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber})\n\n" +
                    $"是否继续?", "构建", "取消"))
                {
                    AutoBuildScript.BuildIOS();
                }
            }
            
            // Android 构建按钮
            GUI.backgroundColor = new Color(0.6f, 0.9f, 0.5f);
            if (GUILayout.Button("🤖 构建 Android", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("确认构建", 
                    $"即将构建 Android 版本\n\n" +
                    $"Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}\n" +
                    $"版本: {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})\n\n" +
                    $"是否继续?", "构建", "取消"))
                {
                    AutoBuildScript.BuildAndroid();
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // 打开输出目录按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📂 打开 iOS 输出", GUILayout.Height(25)))
            {
                var path = config?.GetIOSBuildAbsolutePath() ?? 
                    Path.Combine(AutoBuildConfig.ProjectRoot, "Builds/iOS");
                if (Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "目录不存在: " + path, "OK");
                }
            }
            
            if (GUILayout.Button("📂 打开 Android 输出", GUILayout.Height(25)))
            {
                var path = config?.GetAndroidBuildAbsolutePath() ?? 
                    Path.Combine(AutoBuildConfig.ProjectRoot, "Builds/Android");
                if (Directory.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "目录不存在: " + path, "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);
            
            if (config == null)
            {
                EditorGUILayout.HelpBox("未找到配置文件，点击下方按钮创建。", MessageType.Info);
                
                if (GUILayout.Button("创建配置文件", GUILayout.Height(30)))
                {
                    CreateConfig();
                    LoadConfig();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                
                // Android 签名配置
                EditorGUILayout.LabelField("Android 签名", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    config.keystorePath = EditorGUILayout.TextField("Keystore 路径", config.keystorePath);
                    config.keystorePassword = EditorGUILayout.PasswordField("Keystore 密码", config.keystorePassword);
                    config.keyAliasName = EditorGUILayout.TextField("Key Alias", config.keyAliasName);
                    config.keyAliasPassword = EditorGUILayout.PasswordField("Key 密码", config.keyAliasPassword);
                }
                
                EditorGUILayout.Space(5);
                
                // Telegram 通知
                EditorGUILayout.LabelField("Telegram 通知", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    config.telegramBotToken = EditorGUILayout.TextField("Bot Token", config.telegramBotToken);
                    config.telegramChatId = EditorGUILayout.TextField("Chat ID", config.telegramChatId);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    SyncToEnvFile();
                }
                
                EditorGUILayout.Space(5);
                
                // App Store Connect API Key
                EditorGUILayout.LabelField("App Store Connect API Key", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    config.ascKeyId = EditorGUILayout.TextField("Key ID", config.ascKeyId);
                    config.ascIssuerId = EditorGUILayout.TextField("Issuer ID", config.ascIssuerId);
                    
                    EditorGUILayout.BeginHorizontal();
                    config.ascKeyFilePath = EditorGUILayout.TextField("AuthKey 路径", config.ascKeyFilePath);
                    if (GUILayout.Button("选择", GUILayout.Width(60)))
                    {
                        string path = EditorUtility.OpenFilePanel("选择 AuthKey 文件", "", "p8");
                        if (!string.IsNullOrEmpty(path))
                        {
                            config.ascKeyFilePath = path;
                            EditorUtility.SetDirty(config);
                            SyncToEnvFile();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    SyncToEnvFile();
                }
                
                EditorGUILayout.Space(5);
                
                // 导入/导出按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📤 导出配置"))
                {
                    ExportConfig();
                }
                if (GUILayout.Button("📥 导入配置"))
                {
                    ImportConfig();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void ExportConfig()
        {
            if (config == null) return;
            
            var path = EditorUtility.SaveFilePanel(
                "导出 AutoBuild 配置",
                "",
                "autobuild-config.json",
                "json"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            var json = JsonUtility.ToJson(config, true);
            File.WriteAllText(path, json);
            
            EditorUtility.DisplayDialog("导出成功", 
                $"配置已导出到:\n{path}\n\n可用于其他项目导入。", "确定");
        }
        
        private void ImportConfig()
        {
            var path = EditorUtility.OpenFilePanel(
                "导入 AutoBuild 配置",
                "",
                "json"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("错误", "文件不存在!", "确定");
                return;
            }
            
            var json = File.ReadAllText(path);
            
            // 如果配置文件不存在，先创建
            if (config == null)
            {
                CreateConfig();
                LoadConfig();
            }
            
            if (config != null)
            {
                JsonUtility.FromJsonOverwrite(json, config);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("导入成功", 
                    "配置已从文件导入!\n\n请检查配置是否正确。", "确定");
            }
        }

        private void DrawAdvancedOptions()
        {
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "高级选项", true);
            
            if (showAdvancedOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    if (config != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        
                        config.autoIncrementBuildNumber = EditorGUILayout.Toggle(
                            "自动递增版本号", config.autoIncrementBuildNumber);
                        
                        config.openOutputFolderOnComplete = EditorGUILayout.Toggle(
                            "构建后打开目录", config.openOutputFolderOnComplete);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(config);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
        }

        private void DrawCLICommands()
        {
            EditorGUILayout.LabelField("CLI 命令", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "在终端 cd 到项目目录后执行:\n\n" +
                "build ios        完整 iOS 流程\n" +
                "build android    完整 Android 流程\n\n" +
                "首次使用需确保 ~/.local/bin 在 PATH 中", 
                MessageType.Info);
        }
        private void SyncToEnvFile()
        {
            if (config == null) return;
            
            var projectRoot = AutoBuildConfig.ProjectRoot;
            var ciDir = Path.Combine(projectRoot, ".ci");
            var envPath = Path.Combine(ciDir, ".env");
            
            if (!Directory.Exists(ciDir))
            {
                Directory.CreateDirectory(ciDir);
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# 非敏感配置由 AutoBuild 自动生成");
            sb.AppendLine($"# 更新时间: {System.DateTime.Now}");
            sb.AppendLine();
            
            sb.AppendLine("# Telegram 通知");
            sb.AppendLine($"TELEGRAM_BOT_TOKEN={config.telegramBotToken}");
            sb.AppendLine($"TELEGRAM_CHAT_ID={config.telegramChatId}");
            sb.AppendLine();
            
            sb.AppendLine("# R2 上传 (Cloudflare R2)");
            sb.AppendLine("R2_UPLOADER_URL=https://pan-temp.your-domain.com");
            sb.AppendLine();
            
            sb.AppendLine("# Android Keystore 签名密码");
            sb.AppendLine($"KEYSTORE_PASSWORD={config.keystorePassword}");
            sb.AppendLine($"KEY_PASSWORD={config.keyAliasPassword}");
            sb.AppendLine();
            
            sb.AppendLine("# App Store Connect API Key (TestFlight 上传)");
            sb.AppendLine($"ASC_KEY_ID={config.ascKeyId}");
            sb.AppendLine($"ASC_ISSUER_ID={config.ascIssuerId}");
            sb.AppendLine($"ASC_KEY_FILE=\"{config.ascKeyFilePath}\"");
            
            File.WriteAllText(envPath, sb.ToString());
            // 确保文件只对当前用户可读写 (600)
            // System.Diagnostics.Process.Start("chmod", $"600 \"{envPath}\"");
        }
    }
}
