#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateFusionBridgePrefab
{
    [MenuItem("Tools/Fusion/Create Bridge Prefab")]
    public static void Create()
    {
        var go = new GameObject("FusionGameplayBridge");

#if FUSION_PRESENT
        go.AddComponent<Fusion.NetworkObject>();
        go.AddComponent<DoNotForgetMe.Network.Fusion.FusionGameplayBridge>();
#else
        Debug.LogError("FUSION_PRESENT 未定义，无法创建 FusionGameplayBridge prefab");
        Object.DestroyImmediate(go);
        return;
#endif

        var dir = "Assets/_Project/Resources/NetworkPrefabs";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/_Project/Resources", "NetworkPrefabs");

        var path = dir + "/FusionGameplayBridge.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log("[CreateFusionBridgePrefab] Prefab 已创建: " + path);
    }
}
#endif
