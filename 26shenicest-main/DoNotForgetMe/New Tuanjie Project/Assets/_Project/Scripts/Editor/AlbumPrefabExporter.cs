#if UNITY_EDITOR
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 全家福相册小游戏 Prefab 导出工具。
    /// 使用方法：菜单 Tools > MiniGame > Export Album Prefab
    /// </summary>
    public static class AlbumPrefabExporter
    {
        private const string PrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/AlbumView.prefab";
        private const string MenuItemPath = "Tools/MiniGame/Export Album Prefab";
        private const string BG_ALBUM_PATH = "Assets/_Project/Art/Backgrounds/bg_game3.png";

        [MenuItem(MenuItemPath)]
        public static void Export()
        {
            var config = LoadFirstAlbumConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("导出失败",
                    "未找到 AlbumConfig.asset。请先运行 Tools > 3C Setup > Create Basic Scene 生成配置。", "确定");
                return;
            }

            var tempCanvas = CreateTempCanvas();
            try
            {
                var root = new GameObject("AlbumView", typeof(RectTransform));
                root.transform.SetParent(tempCanvas.transform, false);
                var rootRect = root.GetComponent<RectTransform>();
                StretchRect(rootRect);

                BuildHierarchy(root, config);

                var view = root.AddComponent<AlbumView>();
                AutoWireReferences(root, view, config);

                EnsureDirectoryExists();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[AlbumPrefabExporter] Prefab 已保存到 {PrefabPath}");

                AssetDatabase.Refresh();
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                EditorGUIUtility.PingObject(prefabAsset);
                Selection.activeObject = prefabAsset;
            }
            finally
            {
                if (tempCanvas != null) UnityEngine.Object.DestroyImmediate(tempCanvas.gameObject);
            }
        }

        // ==============================
        // 层级构建
        // ==============================

        private static void BuildHierarchy(GameObject root, AlbumConfig config)
        {
            var rt = root.transform;
            var entries = config.Entries;

            // --- 背景 ---
            var bg = CreateUIObject("Background", rt);
            var bgRect = bg.GetComponent<RectTransform>();
            StretchRect(bgRect);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_ALBUM_PATH);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            var bgArf = bg.AddComponent<AspectRatioFitter>();
            bgArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (bgSprite != null)
                bgArf.aspectRatio = (float)bgSprite.rect.width / bgSprite.rect.height;

            // --- 相册底图 ---
            var albumBase = CreateUIObject("AlbumBaseImage", rt);
            var abRect = albumBase.GetComponent<RectTransform>();
            abRect.anchorMin = abRect.anchorMax = new Vector2(0.5f, 0.5f);
            abRect.anchoredPosition = new Vector2(0, 60);
            abRect.sizeDelta = new Vector2(1200, 600);
            var abImg = albumBase.AddComponent<Image>();
            abImg.color = new Color(0.22f, 0.18f, 0.14f, 0.95f);
            abImg.raycastTarget = false;

            // --- 标题 ---
            var title = CreateTextObject("TitleText", rt, "她叫什么名字？", 42,
                new Color(0.9f, 0.85f, 0.65f), new Vector2(0, 460));
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // --- 指令文字 ---
            var instruction = CreateTextObject("InstructionText", rt, "", 28,
                new Color(0.85f, 0.8f, 0.7f), new Vector2(0, -420));
            instruction.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // --- 轮廓区域 ---
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry == null) continue;

                    // 贴纸轮廓区
                    var stickerZone = CreateUIObject("StickerZone_" + i, rt);
                    var szRect = stickerZone.GetComponent<RectTransform>();
                    szRect.anchorMin = szRect.anchorMax = new Vector2(0.5f, 0.5f);
                    szRect.anchoredPosition = entry.stickerZonePosition + new Vector2(0, 60);
                    szRect.sizeDelta = entry.stickerZoneSize;
                    var szImg = stickerZone.AddComponent<Image>();
                    szImg.color = entry.hasSticker ? new Color(1f, 1f, 1f, 0.08f) : new Color(0.3f, 0.25f, 0.2f, 0.15f);
                    szImg.raycastTarget = false;

                    // 姓名名牌区
                    var nameTagZone = CreateUIObject("NameTagZone_" + i, rt);
                    var nzRect = nameTagZone.GetComponent<RectTransform>();
                    nzRect.anchorMin = nzRect.anchorMax = new Vector2(0.5f, 0.5f);
                    nzRect.anchoredPosition = entry.nameTagZonePosition + new Vector2(0, 60);
                    nzRect.sizeDelta = entry.nameTagZoneSize;
                    var nzImg = nameTagZone.AddComponent<Image>();
                    nzImg.color = new Color(1f, 1f, 1f, 0.06f);
                    nzImg.raycastTarget = false;
                    var nzLabel = CreateTextObject("Label", nameTagZone.transform, "", 22,
                        Color.white, Vector2.zero);
                    var nlRect = nzLabel.GetComponent<RectTransform>();
                    nlRect.anchorMin = Vector2.zero;
                    nlRect.anchorMax = Vector2.one;
                    nlRect.offsetMin = nlRect.offsetMax = Vector2.zero;
                }
            }

            // --- 可拖拽贴纸 ---
            var stickerEntries = config.GetStickerEntries();
            if (stickerEntries != null)
            {
                var spacing = 220f;
                var startX = -(stickerEntries.Length - 1) * spacing / 2f;
                for (var i = 0; i < stickerEntries.Length; i++)
                {
                    var entry = stickerEntries[i];
                    if (entry == null) continue;

                    var pos = new Vector2(startX + i * spacing, -320);
                    var stickerGo = new GameObject("StickerDraggable_" + i, typeof(Image), typeof(DraggableItem));
                    stickerGo.transform.SetParent(rt, false);
                    var sRect = stickerGo.GetComponent<RectTransform>();
                    sRect.anchorMin = sRect.anchorMax = new Vector2(0.5f, 0.5f);
                    sRect.anchoredPosition = pos;
                    sRect.sizeDelta = new Vector2(120, 160);
                    var sImg = stickerGo.GetComponent<Image>();
                    if (entry.stickerSprite != null)
                    {
                        sImg.sprite = entry.stickerSprite;
                        sImg.color = Color.white;
                        sImg.preserveAspect = true;
                    }
                    else
                    {
                        sImg.color = new Color(0.6f, 0.48f, 0.32f);
                    }
                }
            }

            // --- 可拖拽姓名标签 ---
            if (stickerEntries != null)
            {
                var spacing = 220f;
                var startX = -(stickerEntries.Length - 1) * spacing / 2f;
                for (var i = 0; i < stickerEntries.Length; i++)
                {
                    var entry = stickerEntries[i];
                    if (entry == null) continue;

                    var pos = new Vector2(startX + i * spacing, -320);
                    var tagGo = new GameObject("NameTagDraggable_" + i, typeof(Image), typeof(DraggableItem));
                    tagGo.transform.SetParent(rt, false);
                    var tRect = tagGo.GetComponent<RectTransform>();
                    tRect.anchorMin = tRect.anchorMax = new Vector2(0.5f, 0.5f);
                    tRect.anchoredPosition = pos;
                    tRect.sizeDelta = new Vector2(200, 60);
                    var tImg = tagGo.GetComponent<Image>();
                    tImg.color = new Color(0.7f, 0.58f, 0.4f);
                    var tLabel = CreateTextObject("LabelText", tagGo.transform, entry.displayName, 24,
                        Color.white, Vector2.zero);
                    var tlRect = tLabel.GetComponent<RectTransform>();
                    tlRect.anchorMin = Vector2.zero;
                    tlRect.anchorMax = Vector2.one;
                    tlRect.offsetMin = tlRect.offsetMax = Vector2.zero;
                }
            }

            // --- 线索按钮 ---
            var clueBtn = CreateUIObject("ClueButtonRoot", rt);
            var cbRect = clueBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0, 1);
            cbRect.anchoredPosition = new Vector2(120, -60);
            cbRect.sizeDelta = new Vector2(200, 80);
            var cbImg = clueBtn.AddComponent<Image>();
            cbImg.color = new Color(0.42f, 0.32f, 0.2f, 0.9f);
            var cbButton = clueBtn.AddComponent<Button>();
            cbButton.targetGraphic = cbImg;
            var cbLabel = CreateTextObject("ClueButtonLabel", clueBtn.transform, "查看线索", 24,
                Color.white, Vector2.zero);
            var cblRect = cbLabel.GetComponent<RectTransform>();
            cblRect.anchorMin = Vector2.zero;
            cblRect.anchorMax = Vector2.one;
            cblRect.offsetMin = cblRect.offsetMax = Vector2.zero;

            // --- 完成按钮 ---
            var completeBtn = CreateUIObject("CompleteButtonRoot", rt);
            var cpRect = completeBtn.GetComponent<RectTransform>();
            cpRect.anchorMin = cpRect.anchorMax = new Vector2(0.5f, 0.5f);
            cpRect.anchoredPosition = new Vector2(0, -400);
            cpRect.sizeDelta = new Vector2(300, 90);
            var cpImg = completeBtn.AddComponent<Image>();
            cpImg.color = new Color(0.5f, 0.35f, 0.15f);
            var cpButton = completeBtn.AddComponent<Button>();
            cpButton.targetGraphic = cpImg;
            completeBtn.AddComponent<ButtonHoverEffect>();
            var cpLabel = CreateTextObject("CompleteButtonLabel", completeBtn.transform,
                "完成", 32, Color.white, Vector2.zero);
            var cplRect = cpLabel.GetComponent<RectTransform>();
            cplRect.anchorMin = Vector2.zero;
            cplRect.anchorMax = Vector2.one;
            cplRect.offsetMin = cplRect.offsetMax = Vector2.zero;

            // --- 线索面板 ---
            var cluePanel = CreateUIObject("CluePanelRoot", rt);
            var cpnRect = cluePanel.GetComponent<RectTransform>();
            cpnRect.anchorMin = cpnRect.anchorMax = new Vector2(0.5f, 0.5f);
            cpnRect.anchoredPosition = Vector2.zero;
            cpnRect.sizeDelta = new Vector2(1600, 900);
            var cpnImg = cluePanel.AddComponent<Image>();
            cpnImg.color = new Color(0.05f, 0.04f, 0.03f, 0.95f);

            var clueTitle = CreateTextObject("CluePanelTitle", cluePanel.transform, "已收集照片", 36,
                new Color(0.9f, 0.85f, 0.65f), new Vector2(0, 380));
            clueTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 60);

            // 线索照片
            if (entries != null)
            {
                var photoSpacing = 320f;
                var count = 0;
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry == null || entry.photoSprite == null) continue;

                    var photoPos = new Vector2(-photoSpacing * 2 + count * photoSpacing, 50);
                    var photoGo = CreateUIObject("CluePhoto_" + count, cluePanel.transform);
                    var phRect = photoGo.GetComponent<RectTransform>();
                    phRect.anchorMin = phRect.anchorMax = new Vector2(0.5f, 0.5f);
                    phRect.anchoredPosition = photoPos;
                    phRect.sizeDelta = new Vector2(280, 350);
                    var phImg = photoGo.AddComponent<Image>();
                    phImg.sprite = entry.photoSprite;
                    phImg.preserveAspect = true;
                    phImg.color = new Color(0.95f, 0.9f, 0.8f);

                    // 便签
                    var noteGo = CreateUIObject("Note", photoGo.transform);
                    var nRect = noteGo.GetComponent<RectTransform>();
                    nRect.anchorMin = nRect.anchorMax = new Vector2(0.5f, 0.5f);
                    nRect.anchoredPosition = new Vector2(-80, -80);
                    nRect.sizeDelta = new Vector2(260, 120);
                    var nImg = noteGo.AddComponent<Image>();
                    nImg.color = new Color(0.95f, 0.82f, 0.35f, 0.95f);

                    var noteText = CreateTextObject("NoteText", noteGo.transform, entry.clueText ?? "", 18,
                        new Color(0.2f, 0.15f, 0.1f), Vector2.zero);
                    var ntRect = noteText.GetComponent<RectTransform>();
                    ntRect.anchorMin = Vector2.zero;
                    ntRect.anchorMax = Vector2.one;
                    ntRect.offsetMin = ntRect.offsetMax = Vector2.zero;

                    count++;
                }
            }

            // 关闭线索按钮
            var closeBtn = CreateUIObject("CloseClueButtonRoot", cluePanel.transform);
            var ccRect = closeBtn.GetComponent<RectTransform>();
            ccRect.anchorMin = ccRect.anchorMax = new Vector2(0.5f, 0.5f);
            ccRect.anchoredPosition = new Vector2(0, -380);
            ccRect.sizeDelta = new Vector2(200, 70);
            var ccImg = closeBtn.AddComponent<Image>();
            ccImg.color = new Color(0.42f, 0.32f, 0.2f);
            var ccButton = closeBtn.AddComponent<Button>();
            ccButton.targetGraphic = ccImg;
            var ccLabel = CreateTextObject("CloseClueButtonLabel", closeBtn.transform, "关闭", 26,
                Color.white, Vector2.zero);
            var cclRect = ccLabel.GetComponent<RectTransform>();
            cclRect.anchorMin = Vector2.zero;
            cclRect.anchorMax = Vector2.one;
            cclRect.offsetMin = cclRect.offsetMax = Vector2.zero;

            cluePanel.SetActive(false);

            // --- 完成动画 ---
            var portrait = CreateUIObject("FamilyPortraitImage", rt);
            var fpRect = portrait.GetComponent<RectTransform>();
            fpRect.anchorMin = fpRect.anchorMax = new Vector2(0.5f, 0.5f);
            fpRect.anchoredPosition = Vector2.zero;
            fpRect.sizeDelta = new Vector2(1920, 1080);
            var fpImg = portrait.AddComponent<Image>();
            fpImg.sprite = config.RealisticFamilyPortrait;
            fpImg.color = Color.white;
            fpImg.preserveAspect = true;
            fpImg.raycastTarget = false;
            portrait.SetActive(false);

            var blackScreen = CreateUIObject("BlackScreenImage", rt);
            var bsRect = blackScreen.GetComponent<RectTransform>();
            bsRect.anchorMin = bsRect.anchorMax = new Vector2(0.5f, 0.5f);
            bsRect.anchoredPosition = Vector2.zero;
            bsRect.sizeDelta = new Vector2(1920, 1080);
            var bsImg = blackScreen.AddComponent<Image>();
            bsImg.color = Color.black;
            bsImg.raycastTarget = false;
            blackScreen.SetActive(false);

            // --- 终局全家福照片（AlbumMiniGameView 按名称查找此对象做弹出动画） ---
            const string PORTRAIT_LAYER_PATH = "Assets/_Project/Art/微信图片_20260830062314_1474_3230.png";
            var portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PORTRAIT_LAYER_PATH);
            var portraitLayer = CreateUIObject("微信图片_20260830062314_1474_3230", rt);
            var plRect = portraitLayer.GetComponent<RectTransform>();
            plRect.anchorMin = plRect.anchorMax = new Vector2(0.5f, 0.5f);
            plRect.anchoredPosition = Vector2.zero;
            plRect.sizeDelta = new Vector2(1920, 1080);
            var plImg = portraitLayer.AddComponent<Image>();
            plImg.sprite = portraitSprite;
            plImg.color = Color.white;
            plImg.preserveAspect = true;
            plImg.raycastTarget = false;
            portraitLayer.SetActive(false);

            // 子图层（ShowFamilyPortraitAndFinish 会对 GetChild(0) 做二级弹出动画）
            var portraitChild = CreateUIObject("PortraitOverlay", portraitLayer.transform);
            var pchRect = portraitChild.GetComponent<RectTransform>();
            pchRect.anchorMin = Vector2.zero;
            pchRect.anchorMax = Vector2.one;
            pchRect.offsetMin = pchRect.offsetMax = Vector2.zero;
            var pchImg = portraitChild.AddComponent<Image>();
            pchImg.color = new Color(1, 1, 1, 0);
            pchImg.raycastTarget = false;
        }

        // ==============================
        // 辅助方法
        // ==============================

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateTextObject(string name, Transform parent, string content,
            int fontSize, Color color, Vector2 position)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(800, 100);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static Canvas CreateTempCanvas()
        {
            var go = new GameObject("TempExportCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static AlbumConfig LoadFirstAlbumConfig()
        {
            var guids = AssetDatabase.FindAssets("t:AlbumConfig");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AlbumConfig>(path);
        }

        private static void EnsureDirectoryExists()
        {
            var dir = "Assets/_Project/Resources/MiniGamePrefabs";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "MiniGamePrefabs");
            }
        }

        // ==============================
        // 自动绑定
        // ==============================

        private static void AutoWireReferences(GameObject root, AlbumView view, AlbumConfig config)
        {
            var so = new SerializedObject(view);
            var rt = root.transform;
            var entries = config.Entries;
            var entryCount = entries != null ? entries.Length : 0;
            var stickerEntries = config.GetStickerEntries();
            var stickerCount = stickerEntries != null ? stickerEntries.Length : 0;

            // 共享
            so.FindProperty("_albumBaseImage").objectReferenceValue = FindComponent<Image>(rt, "AlbumBaseImage");
            so.FindProperty("_titleText").objectReferenceValue = FindComponent<Text>(rt, "TitleText");
            so.FindProperty("_instructionText").objectReferenceValue = FindComponent<Text>(rt, "InstructionText");

            // 轮廓区域
            WireStickerZones(so, "_stickerZones", rt, "StickerZone_", entryCount);
            WireNameTagZones(so, "_nameTagZones", rt, "NameTagZone_", entryCount);

            // 可拖拽
            WireStickerDraggables(so, "_stickerDraggables", rt, "StickerDraggable_", stickerCount);
            WireNameTagDraggables(so, "_nameTagDraggables", rt, "NameTagDraggable_", stickerCount);

            // 按钮
            so.FindProperty("_clueButtonRoot").objectReferenceValue = FindGameObject(rt, "ClueButtonRoot");
            so.FindProperty("_clueButton").objectReferenceValue = FindComponent<Button>(rt, "ClueButtonRoot");
            so.FindProperty("_clueButtonLabel").objectReferenceValue = FindComponent<Text>(rt, "ClueButtonLabel");
            so.FindProperty("_completeButtonRoot").objectReferenceValue = FindGameObject(rt, "CompleteButtonRoot");
            so.FindProperty("_completeButton").objectReferenceValue = FindComponent<Button>(rt, "CompleteButtonRoot");
            so.FindProperty("_completeButtonLabel").objectReferenceValue = FindComponent<Text>(rt, "CompleteButtonLabel");

            // 线索面板
            var cluePanel = rt.Find("CluePanelRoot");
            so.FindProperty("_cluePanelRoot").objectReferenceValue = cluePanel?.gameObject;
            so.FindProperty("_cluePanelImage").objectReferenceValue = FindComponent<Image>(cluePanel, "CluePanelRoot");
            so.FindProperty("_cluePanelTitle").objectReferenceValue = FindComponent<Text>(cluePanel, "CluePanelTitle");
            so.FindProperty("_closeClueButtonRoot").objectReferenceValue = FindGameObject(cluePanel, "CloseClueButtonRoot");
            so.FindProperty("_closeClueButton").objectReferenceValue = FindComponent<Button>(cluePanel, "CloseClueButtonRoot");
            so.FindProperty("_closeClueButtonLabel").objectReferenceValue = FindComponent<Text>(cluePanel, "CloseClueButtonLabel");

            // 完成动画
            so.FindProperty("_familyPortraitImage").objectReferenceValue = FindComponent<Image>(rt, "FamilyPortraitImage");
            so.FindProperty("_blackScreenImage").objectReferenceValue = FindComponent<Image>(rt, "BlackScreenImage");

            so.ApplyModifiedProperties();
            Debug.Log("[AlbumPrefabExporter] AlbumView 引用已自动绑定");
        }

        private static void WireStickerZones(SerializedObject so, string fieldName,
            Transform parent, string namePrefix, int count)
        {
            if (parent == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var child = parent.Find(namePrefix + i);
                if (child == null) continue;
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = child.gameObject;
                element.FindPropertyRelative("image").objectReferenceValue = child.GetComponent<Image>();
            }
        }

        private static void WireNameTagZones(SerializedObject so, string fieldName,
            Transform parent, string namePrefix, int count)
        {
            if (parent == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var child = parent.Find(namePrefix + i);
                if (child == null) continue;
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = child.gameObject;
                element.FindPropertyRelative("image").objectReferenceValue = child.GetComponent<Image>();
                element.FindPropertyRelative("labelText").objectReferenceValue = FindComponent<Text>(child, "Label");
            }
        }

        private static void WireStickerDraggables(SerializedObject so, string fieldName,
            Transform parent, string namePrefix, int count)
        {
            if (parent == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var child = parent.Find(namePrefix + i);
                if (child == null) continue;
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = child.gameObject;
                element.FindPropertyRelative("image").objectReferenceValue = child.GetComponent<Image>();
                element.FindPropertyRelative("draggable").objectReferenceValue = child.GetComponent<DraggableItem>();
            }
        }

        private static void WireNameTagDraggables(SerializedObject so, string fieldName,
            Transform parent, string namePrefix, int count)
        {
            if (parent == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var child = parent.Find(namePrefix + i);
                if (child == null) continue;
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = child.gameObject;
                element.FindPropertyRelative("image").objectReferenceValue = child.GetComponent<Image>();
                element.FindPropertyRelative("draggable").objectReferenceValue = child.GetComponent<DraggableItem>();
                element.FindPropertyRelative("labelText").objectReferenceValue = FindComponent<Text>(child, "LabelText");
            }
        }

        private static T FindComponent<T>(Transform parent, string name) where T : Component
        {
            if (parent == null) return null;
            var child = parent.Find(name);
            if (child == null) return null;
            return child.GetComponent<T>();
        }

        private static GameObject FindGameObject(Transform parent, string name)
        {
            if (parent == null) return null;
            var child = parent.Find(name);
            return child?.gameObject;
        }
    }
}
#endif
