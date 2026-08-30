#if UNITY_EDITOR
using System.IO;
using DoNotForgetMe.MiniGame.Album;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 用"勿忘我第三关"美术资源替换 AlbumView prefab 和 AlbumConfig 中的旧素材。
    /// 使用方法：菜单 Tools > MiniGame > Replace Album Assets (Level 3)
    /// </summary>
    public static class AlbumAssetReplacer
    {
        private const string ArtDir = "Assets/_Project/Art/勿忘我第三关";
        private const string PrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/AlbumView.prefab";
        private const string ConfigPath = "Assets/_Project/Settings/AlbumConfig.asset";
        private const string MenuItemPath = "Tools/MiniGame/Replace Album Assets (Level 3)";

        [MenuItem(MenuItemPath)]
        public static void Replace()
        {
            var config = AssetDatabase.LoadAssetAtPath<AlbumConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("替换失败", $"未找到 AlbumConfig: {ConfigPath}", "确定");
                return;
            }

            // 加载新美术资源
            var bgSprite = LoadSprite($"{ArtDir}/背景.jpg");
            var albumBaseSprite = LoadSprite($"{ArtDir}/albumbase_image.png");
            var silhouetteSprite = LoadSprite($"{ArtDir}/剪影托槽.png");
            var person1 = LoadSprite($"{ArtDir}/人物1.png");
            var person2 = LoadSprite($"{ArtDir}/人物2.png");
            var person3 = LoadSprite($"{ArtDir}/人物3.png");
            var person4 = LoadSprite($"{ArtDir}/人物4.png");
            var person5 = LoadSprite($"{ArtDir}/人物5.png");
            var person6 = LoadSprite($"{ArtDir}/人物6.png");

            var decorationBox = LoadSprite($"{ArtDir}/背景装饰（可以看着加）-盒子.png");
            var decorationKey = LoadSprite($"{ArtDir}/背景装饰（可以看着加）-钥匙.png");
            var decorationCar = LoadSprite($"{ArtDir}/背景装饰（看着可以加）-汽车.png");

            var personSprites = new[] { person1, person2, person3, person4, person5, person6 };

            // 1. 更新 AlbumConfig 中的 stickerSprite
            var so = new SerializedObject(config);
            var entriesProp = so.FindProperty("entries");
            if (entriesProp != null && entriesProp.arraySize > 0)
            {
                for (var i = 0; i < entriesProp.arraySize && i < personSprites.Length; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var stickerProp = entry.FindPropertyRelative("stickerSprite");
                    if (stickerProp != null && personSprites[i] != null)
                    {
                        stickerProp.objectReferenceValue = personSprites[i];
                        Debug.Log($"[AlbumAssetReplacer] entries[{i}].stickerSprite => 人物{i + 1}.png");
                    }
                }
            }

            // realisticFamilyPortrait 也替换为背景图（作为完成时的全家福展示）
            var portraitProp = so.FindProperty("realisticFamilyPortrait");
            if (portraitProp != null && bgSprite != null)
            {
                portraitProp.objectReferenceValue = bgSprite;
                Debug.Log("[AlbumAssetReplacer] realisticFamilyPortrait => 背景.jpg");
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);

            // 2. 更新 prefab 中的直接 sprite 引用
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("替换失败", $"未找到 prefab: {PrefabPath}", "确定");
                return;
            }

            var prefabPath = PrefabPath;
            var root = prefab.transform;
            bool changed = false;

            // AlbumBaseImage -> albumbase_image.png (AI生成的相册背景)
            var albumBase = root.Find("AlbumBaseImage");
            if (albumBase != null && albumBaseSprite != null)
            {
                var img = albumBase.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = albumBaseSprite;
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    changed = true;
                    Debug.Log("[AlbumAssetReplacer] AlbumBaseImage.sprite => albumbase_image.png");
                }
            }

            // StickerZone_0~5 -> 剪影托槽.png 作为背景
            if (silhouetteSprite != null)
            {
                for (var i = 0; i < 6; i++)
                {
                    var zone = root.Find($"StickerZone_{i}");
                    if (zone == null) continue;
                    var img = zone.GetComponent<Image>();
                    if (img == null) continue;
                    img.sprite = silhouetteSprite;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    // 保持半透明，放置贴纸后会覆盖
                    img.color = new Color(1f, 1f, 1f, 0.15f);
                    changed = true;
                    Debug.Log($"[AlbumAssetReplacer] StickerZone_{i}.sprite => 剪影托槽.png");
                }
            }

            // StickerDraggable_0~4 -> 对应人物贴纸
            // GetStickerEntries 排除了 hasSticker=false 的人物（刘洪梅）
            // 所以 sticker draggable 索引对应 entries 中 hasSticker=true 的条目
            var stickerEntries = config.GetStickerEntries();
            if (stickerEntries != null)
            {
                for (var i = 0; i < stickerEntries.Length; i++)
                {
                    var draggable = root.Find($"StickerDraggable_{i}");
                    if (draggable == null) continue;
                    var img = draggable.GetComponent<Image>();
                    if (img == null) continue;

                    var entry = stickerEntries[i];
                    if (entry.stickerSprite != null)
                    {
                        img.sprite = entry.stickerSprite;
                        img.color = Color.white;
                        img.preserveAspect = true;
                        changed = true;
                        Debug.Log($"[AlbumAssetReplacer] StickerDraggable_{i}.sprite => {entry.characterId}");
                    }
                }
            }

            // FamilyPortraitImage -> 背景.jpg
            var portrait = root.Find("FamilyPortraitImage");
            if (portrait != null && bgSprite != null)
            {
                var img = portrait.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = bgSprite;
                    img.preserveAspect = true;
                    changed = true;
                    Debug.Log("[AlbumAssetReplacer] FamilyPortraitImage.sprite => 背景.jpg");
                }
            }

            // 3. 添加背景装饰（盒子/钥匙/汽车）
            if (decorationBox != null || decorationKey != null || decorationCar != null)
            {
                var existingDecor = root.Find("Decoration_盒子");
                if (existingDecor == null)
                {
                    AddDecoration(root, "Decoration_盒子", decorationBox, new Vector2(-480, -120), new Vector2(120, 120));
                    AddDecoration(root, "Decoration_钥匙", decorationKey, new Vector2(520, 80), new Vector2(80, 80));
                    AddDecoration(root, "Decoration_汽车", decorationCar, new Vector2(-520, 200), new Vector2(140, 80));
                    changed = true;
                    Debug.Log("[AlbumAssetReplacer] 已添加背景装饰（盒子/钥匙/汽车）");
                }
            }

            if (changed)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log("[AlbumAssetReplacer] Prefab 已保存");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;

            EditorUtility.DisplayDialog("替换完成",
                "已用勿忘我第三关美术资源替换 AlbumView prefab 和 AlbumConfig。\n\n" +
                "• 人物1-6.png → stickerSprite\n" +
                "• 背景.jpg → FamilyPortraitImage\n" +
                "• albumbase_image.png → AlbumBaseImage\n" +
                "• 剪影托槽.png → StickerZone 背景\n" +
                "• 背景装饰（盒子/钥匙/汽车）→ 已添加",
                "确定");
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[AlbumAssetReplacer] 无法加载: {path}");
            return sprite;
        }

        private static void AddDecoration(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
        {
            if (sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(1f, 1f, 1f, 0.7f);
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }
}
#endif
