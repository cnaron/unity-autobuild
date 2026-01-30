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
                EditorGUILayout.HelpBox("未找到配置文件，将使用默认设置。\n点击下方按钮创建配置文件。", 
                    MessageType.Info);
                
                if (GUILayout.Button("创建配置文件"))
                {
                    CreateConfig();
                    LoadConfig();
                }
            }
            else
            {
                EditorGUILayout.ObjectField("配置文件", config, typeof(AutoBuildConfig), false);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("编辑配置"))
                {
                    Selection.activeObject = config;
                }
                
                // 导出配置
                if (GUILayout.Button("📤 导出配置"))
                {
                    ExportConfig();
                }
                
                // 导入配置
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
                "可在终端使用以下命令进行无界面构建:\n\n" +
                "iOS:\n" +
                "./.ci/build.sh ios\n\n" +
                "Android:\n" +
                "./.ci/build.sh android", 
                MessageType.Info);
        }
    }
}
