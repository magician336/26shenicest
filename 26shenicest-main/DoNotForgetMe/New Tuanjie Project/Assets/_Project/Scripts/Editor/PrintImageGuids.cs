#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DoNotForgetMe.EditorTools
{
    [InitializeOnLoad]
    public static class PrintImageGuids
    {
        static PrintImageGuids()
        {
            var artDir = "Assets/_Project/Art/勿忘我图片1";
            var guids = AssetDatabase.FindAssets("", new[] { artDir });
            var logPath = Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"), "image_guids.txt");
            using (var writer = new StreamWriter(logPath, false))
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    writer.WriteLine($"{path} => {guid}");
                }
            }
            Debug.Log($"[PrintImageGuids] Wrote {guids.Length} entries to {logPath}");
        }
    }
}
#endif
