#if UNITY_EDITOR
using DoNotForgetMe.MiniGame.Album;
using UnityEditor;
using UnityEngine;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 将每个 StickerZone 的 sizeDelta 对齐到其对应 StickerDraggable 的 sizeDelta，
    /// 使判定区域与贴纸尺寸一致。菜单 Tools > MiniGame > Align Album Sticker Zones
    /// </summary>
    public static class AlbumZoneAligner
    {
        private const string PrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/AlbumView.prefab";
        private const string ConfigPath = "Assets/_Project/Settings/AlbumConfig.asset";
        private const string MenuItemPath = "Tools/MiniGame/Align Album Sticker Zones";

        [MenuItem(MenuItemPath)]
        public static void Align()
        {
            var config = AssetDatabase.LoadAssetAtPath<AlbumConfig>(ConfigPath);
            if (config == null)
            {
                // 兜底：搜索任意 AlbumConfig
                var guids = AssetDatabase.FindAssets("t:AlbumConfig");
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("对齐失败", "未找到 AlbumConfig.asset", "确定");
                    return;
                }
                config = AssetDatabase.LoadAssetAtPath<AlbumConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("对齐失败", $"未找到 prefab: {PrefabPath}", "确定");
                return;
            }

            var root = prefab.transform;
            var entries = config.Entries;
            var stickerEntries = config.GetStickerEntries();
            int aligned = 0;

            // stickerEntries 的索引 == StickerDraggable_N
            // entries 的索引 == StickerZone_N
            // 需要遍历 entries，跳过 hasSticker=false，对齐到对应的 Draggable
            int draggableIndex = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.hasSticker) continue;

                var zoneTr = root.Find($"StickerZone_{i}");
                var dragTr = root.Find($"StickerDraggable_{draggableIndex}");
                if (zoneTr == null || dragTr == null)
                {
                    Debug.LogWarning($"[AlbumZoneAligner] 跳过 Zone_{i} ↔ Draggable_{draggableIndex}：未找到节点");
                    draggableIndex++;
                    continue;
                }

                var zoneRect = zoneTr as RectTransform;
                var dragRect = dragTr as RectTransform;
                if (zoneRect == null || dragRect == null)
                {
                    draggableIndex++;
                    continue;
                }

                var oldSize = zoneRect.sizeDelta;
                zoneRect.sizeDelta = dragRect.sizeDelta;
                aligned++;
                Debug.Log($"[AlbumZoneAligner] StickerZone_{i}.sizeDelta: {oldSize} → {zoneRect.sizeDelta} " +
                          $"( matched StickerDraggable_{draggableIndex} / {entry.characterId} )");

                draggableIndex++;
            }

            if (aligned > 0)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("对齐完成",
                    $"已将 {aligned} 个 StickerZone 的 sizeDelta 对齐到对应 StickerDraggable。\n" +
                    "如需微调位置，请在 Inspector 中直接拖动 StickerZone 节点。",
                    "确定");
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
            }
            else
            {
                EditorUtility.DisplayDialog("未对齐", "没有找到需要对齐的 StickerZone。", "确定");
            }
        }

        private const string SetPositionsMenuItem = "Tools/MiniGame/Set Album Sticker Positions";

        /// <summary>
        /// StickerDraggable_N → StickerZone 映射关系：
        /// entries 中 hasSticker=true 的条目按顺序对应 StickerDraggable_0~4。
        /// entries[0]=liu_hongxiu → Draggable_0 → Zone_0
        /// entries[2]=liu_hongju  → Draggable_1 → Zone_2
        /// entries[3]=liu_hongfang→ Draggable_2 → Zone_3
        /// entries[4]=liu_hongqiang→Draggable_3 → Zone_4
        /// entries[5]=liu_hongbin → Draggable_4 → Zone_5
        /// </summary>
        private static readonly (int zoneIndex, Vector2 pos)[] DesiredPositions =
        {
            (0, new Vector2(-318f, 43f)),
            (2, new Vector2(91f, 63f)),
            (3, new Vector2(-235.09f, -128.14f)),
            (4, new Vector2(-113f, -97f)),
            (5, new Vector2(29f, -83f)),
        };

        [MenuItem(SetPositionsMenuItem)]
        public static void SetStickerPositions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("失败", $"未找到 prefab: {PrefabPath}", "确定");
                return;
            }

            var root = prefab.transform;
            int applied = 0;

            foreach (var (zoneIndex, pos) in DesiredPositions)
            {
                var zoneTr = root.Find($"StickerZone_{zoneIndex}");
                if (zoneTr == null)
                {
                    Debug.LogWarning($"[AlbumZoneAligner] StickerZone_{zoneIndex} 未找到");
                    continue;
                }

                var rect = zoneTr as RectTransform;
                if (rect == null) continue;

                var oldPos = rect.anchoredPosition;
                rect.anchoredPosition = pos;
                applied++;
                Debug.Log($"[AlbumZoneAligner] StickerZone_{zoneIndex}.anchoredPosition: {oldPos} → {pos}");
            }

            if (applied > 0)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("完成",
                    $"已更新 {applied} 个 StickerZone 的位置。\n" +
                    "StickerZone_0 → (-318, 43)\n" +
                    "StickerZone_2 → (91, 63)\n" +
                    "StickerZone_3 → (-235.09, -128.14)\n" +
                    "StickerZone_4 → (-113, -97)\n" +
                    "StickerZone_5 → (29, -83)",
                    "确定");
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
            }
        }
    }
}
#endif
