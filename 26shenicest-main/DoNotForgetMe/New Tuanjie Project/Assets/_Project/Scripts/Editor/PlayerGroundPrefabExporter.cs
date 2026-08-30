#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 玩家与地板 Prefab 导出工具。
    /// 将原 SceneSetupBase 中代码生成的 Player / Ground 保存为可编辑 Prefab。
    ///
    /// 使用方法：菜单 Tools > Scene > Export Player & Ground Prefabs
    /// 前提：PlayerInputSettings.asset 和 PlayerSettings.asset 已存在。
    /// </summary>
    public static class PlayerGroundPrefabExporter
    {
        private const string PrefabDir = "Assets/_Project/Resources/ScenePrefabs";
        private const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
        private const string GroundPrefabPath = PrefabDir + "/Ground.prefab";

        [MenuItem("Tools/Scene/Export Player & Ground Prefabs")]
        public static void ExportAll()
        {
            ExportPlayer();
            ExportGround();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerGroundPrefabExporter] 全部导出完成");
        }

        [MenuItem("Tools/Scene/Export Player Prefab")]
        public static void ExportPlayer()
        {
            var inputSettings = AssetDatabase.LoadAssetAtPath<InputSettings>(
                "Assets/_Project/Settings/PlayerInputSettings.asset");
            var playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>(
                "Assets/_Project/Settings/PlayerSettings.asset");

            if (inputSettings == null || playerSettings == null)
            {
                EditorUtility.DisplayDialog("导出失败",
                    "未找到 PlayerInputSettings.asset 或 PlayerSettings.asset。\n" +
                    "请先运行 Tools > 3C Setup > Create Living Room Scene 生成配置。", "确定");
                return;
            }

            EnsureDirectoryExists();

            // --- 创建 Player GameObject ---
            var playerObj = new GameObject("Player");
            playerObj.layer = 8; // GroundLayer=8 会在下面覆盖为 PlayerLayer=9
            playerObj.layer = 9; // PlayerLayer
            playerObj.transform.position = new Vector3(0, -2f, 0);

            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.8f);

            var pc = playerObj.AddComponent<PlayerController>();
            var mc = playerObj.AddComponent<MovementController>();
            var hc = playerObj.AddComponent<HealthController>();
            var ic = playerObj.AddComponent<InteractionController>();
            var pih = playerObj.AddComponent<PlayerInputHandler>();
            playerObj.AddComponent<SimpleWalkAnimation>();

            // --- 自动绑定引用 ---
            var pcSo = new SerializedObject(pc);
            pcSo.FindProperty("movementController").objectReferenceValue = mc;
            pcSo.FindProperty("interactionController").objectReferenceValue = ic;
            pcSo.FindProperty("healthController").objectReferenceValue = hc;
            pcSo.FindProperty("cachedInputHandler").objectReferenceValue = pih;
            pcSo.FindProperty("inputSettings").objectReferenceValue = inputSettings;
            pcSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
            pcSo.ApplyModifiedProperties();

            var icSo = new SerializedObject(ic);
            icSo.FindProperty("inputSettings").objectReferenceValue = inputSettings;
            icSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
            icSo.FindProperty("interactLayer").intValue = 1 << 10; // InteractableLayer
            icSo.ApplyModifiedProperties();

            var hcSo = new SerializedObject(hc);
            hcSo.FindProperty("playerSettings").objectReferenceValue = playerSettings;
            hcSo.ApplyModifiedProperties();

            // --- 保存为 Prefab ---
            PrefabUtility.SaveAsPrefabAsset(playerObj, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(playerObj);

            Debug.Log($"[PlayerGroundPrefabExporter] Player Prefab 已保存到 {PlayerPrefabPath}");
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            EditorGUIUtility.PingObject(prefabAsset);
            Selection.activeObject = prefabAsset;
        }

        [MenuItem("Tools/Scene/Export Ground Prefab")]
        public static void ExportGround()
        {
            EnsureDirectoryExists();

            var groundObj = new GameObject("Ground");
            groundObj.layer = 8; // GroundLayer
            groundObj.transform.position = new Vector3(0, -3, 0);
            groundObj.transform.localScale = new Vector3(1, 1, 1);

            var col = groundObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            PrefabUtility.SaveAsPrefabAsset(groundObj, GroundPrefabPath);
            UnityEngine.Object.DestroyImmediate(groundObj);

            Debug.Log($"[PlayerGroundPrefabExporter] Ground Prefab 已保存到 {GroundPrefabPath}");
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefabPath);
            EditorGUIUtility.PingObject(prefabAsset);
            Selection.activeObject = prefabAsset;
        }

        private static void EnsureDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "ScenePrefabs");
            }
        }
    }
}
#endif
