using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace AutoBuild
{
    /// <summary>
    /// iOS 构建后处理 - 自动配置 Info.plist
    /// 解决 TestFlight "缺少出口合规证明" 问题
    /// </summary>
    public static class iOSPostProcessBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            // 修改 Info.plist
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            // 出口合规 - 声明不使用加密
            // 这样上传到 TestFlight 后不需要手动确认加密问题
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            
            // 保存修改
            plist.WriteToFile(plistPath);
            
            UnityEngine.Debug.Log("[AutoBuild] iOS Info.plist 已配置出口合规声明");
        }
    }
}
