#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DoNotForgetMe.EditorTools
{
    public static class PrintTextureSizes
    {
        [MenuItem("Tools/Debug/Print Texture Sizes")]
        public static void Print()
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets/_Project/Art" });
            var logPath = Path.Combine(System.Environment.GetEnvironmentVariable("TEMP"), "texture_sizes.txt");
            using (var writer = new StreamWriter(logPath, false))
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex == null) continue;
                    writer.WriteLine($"{path} => {tex.width}x{tex.height} (guid: {guid})");
                }
            }
            Debug.Log($"[PrintTextureSizes] Wrote to {logPath}");
        }
    }
}
#endif
