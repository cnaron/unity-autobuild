using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace AutoBuild
{
    /// <summary>
    /// iOS 构建后处理 - 自动配置 Info.plist 和修复兼容性问题
    /// </summary>
    public static class iOSPostProcessBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            // 1. 修改 Info.plist - 出口合规声明
            PatchInfoPlist(pathToBuiltProject);
            
            // 2. 修复 Xcode 16 兼容性问题
            PatchMainMM(pathToBuiltProject);
            
            UnityEngine.Debug.Log("[AutoBuild] iOS 构建后处理完成");
        }
        
        /// <summary>
        /// 出口合规声明
        /// </summary>
        private static void PatchInfoPlist(string pathToBuiltProject)
        {
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            // 声明不使用加密，跳过 TestFlight 合规检查
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            
            plist.WriteToFile(plistPath);
            UnityEngine.Debug.Log("[AutoBuild] Info.plist 已配置出口合规声明");
        }
        
        /// <summary>
        /// 修复 Xcode 16 兼容性问题
        /// Unity 2021.x 生成的 main.mm 缺少 mach-o/ldsyms.h 导入
        /// </summary>
        private static void PatchMainMM(string pathToBuiltProject)
        {
            string mainPath = Path.Combine(pathToBuiltProject, "MainApp/main.mm");
            if (!File.Exists(mainPath)) return;
            
            string content = File.ReadAllText(mainPath);
            
            // 检查是否已经有这个导入
            if (content.Contains("mach-o/ldsyms.h")) return;
            
            // 在第一个 #include 之前添加
            string newInclude = "#include <mach-o/ldsyms.h>\n";
            int insertPos = content.IndexOf("#include");
            if (insertPos >= 0)
            {
                content = content.Insert(insertPos, newInclude);
                File.WriteAllText(mainPath, content);
                UnityEngine.Debug.Log("[AutoBuild] main.mm 已修复 Xcode 16 兼容性");
            }
        }
    }
}
