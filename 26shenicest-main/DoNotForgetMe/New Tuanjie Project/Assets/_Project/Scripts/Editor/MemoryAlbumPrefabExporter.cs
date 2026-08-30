#if UNITY_EDITOR
using DoNotForgetMe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 家庭记忆相册（物品栏）Prefab 导出工具。
    /// 将 MemoryAlbumController 中代码生成的 Canvas + Album 面板 + 照片槽位 + InputBlocker
    /// 保存为可编辑 Prefab。
    ///
    /// 使用方法：菜单 Tools > UI > Export Memory Album Prefab
    /// </summary>
    public static class MemoryAlbumPrefabExporter
    {
        private const int SlotCount = 6;
        private const string PrefabPath = "Assets/_Project/Resources/UIPrefabs/MemoryAlbumView.prefab";
        private const string MenuItemPath = "Tools/UI/Export Memory Album Prefab";

        [MenuItem(MenuItemPath)]
        public static void Export()
        {
            var paperBg = LoadPaperBgSprite();

            // --- 根节点：Canvas + CanvasScaler + GraphicRaycaster ---
            var root = new GameObject("MemoryAlbumView", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            root.AddComponent<GraphicRaycaster>();
            StretchRect(root.GetComponent<RectTransform>());

            var albumPanel = BuildAlbumPanel(root.transform, paperBg);
            var inputBlocker = BuildInputBlocker(root.transform);
            var preview = BuildPreviewPanel(root.transform, paperBg);

            // --- 添加 View 并绑定引用 ---
            var view = root.AddComponent<MemoryAlbumView>();
            AutoWireReferences(view, canvas, albumPanel, inputBlocker, root.transform, paperBg, preview);

            // --- 添加 Controller（Inspector 中可编辑序列化字段） ---
            var controller = root.AddComponent<MemoryAlbumController>();
            WirePhotoSprites(controller);

            // --- 保存 Prefab ---
            EnsureDirectoryExists();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            Debug.Log($"[MemoryAlbumPrefabExporter] Prefab 已保存到 {PrefabPath}");

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorGUIUtility.PingObject(prefabAsset);
            Selection.activeObject = prefabAsset;
        }

        // ==============================
        // 层级构建
        // ==============================

        private static RectTransform BuildAlbumPanel(Transform parent, Sprite paperBg)
        {
            var albumGo = new GameObject("MemoryAlbum", typeof(RectTransform));
            albumGo.transform.SetParent(parent, false);
            var albumRect = albumGo.GetComponent<RectTransform>();
            albumRect.anchorMin = albumRect.anchorMax = new Vector2(0, 1);
            albumRect.pivot = new Vector2(0, 1);
            albumRect.anchoredPosition = new Vector2(24, -24);
            albumRect.sizeDelta = new Vector2(800, 150);

            // --- 6 个稿纸底图（排在最前，渲染在最底层） ---
            for (var i = 0; i < SlotCount; i++)
            {
                var slotCenter = GetSlotCenter(i);
                var paperGo = new GameObject("PaperBg_" + i, typeof(Image));
                paperGo.transform.SetParent(albumRect, false);
                var paperRect = paperGo.GetComponent<RectTransform>();
                paperRect.anchorMin = paperRect.anchorMax = new Vector2(0.5f, 0.5f);
                paperRect.anchoredPosition = slotCenter;
                paperRect.sizeDelta = new Vector2(125, 125);
                var paperImg = paperGo.GetComponent<Image>();
                if (paperBg != null)
                {
                    paperImg.sprite = paperBg;
                    paperImg.color = Color.white;
                    paperImg.preserveAspect = false;
                }
                else
                {
                    paperImg.color = new Color(0.95f, 0.88f, 0.7f, 0.5f);
                }
                paperImg.raycastTarget = false;
            }

            // --- 标题文字 ---
            var titleGo = new GameObject("AlbumTitleText", typeof(Text));
            titleGo.transform.SetParent(albumRect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(40, -14);
            titleRect.sizeDelta = new Vector2(80, 30);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = GetDefaultFont();
            titleText.fontSize = 20;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;
            titleText.text = "相册";
            titleText.raycastTarget = false;

            // --- 6 个照片槽位（排在最后，渲染在最顶层） ---
            for (var i = 0; i < SlotCount; i++)
            {
                var slotCenter = GetSlotCenter(i);
                var slotGo = new GameObject("PhotoSlot_" + i, typeof(Image), typeof(Button));
                slotGo.transform.SetParent(albumRect, false);
                var slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = slotCenter;
                slotRect.sizeDelta = new Vector2(85, 85);
                var slotImg = slotGo.GetComponent<Image>();
                slotImg.color = new Color(0.73f, 0.59f, 0.38f, 1f);
                var slotBtn = slotGo.GetComponent<Button>();
                slotBtn.targetGraphic = slotImg;

                // 占位文字
                var phGo = new GameObject("PlaceholderText", typeof(Text));
                phGo.transform.SetParent(slotRect, false);
                var phRect = phGo.GetComponent<RectTransform>();
                phRect.anchorMin = phRect.anchorMax = new Vector2(0.5f, 0.5f);
                phRect.anchoredPosition = Vector2.zero;
                phRect.sizeDelta = new Vector2(58, 30);
                var phText = phGo.GetComponent<Text>();
                phText.font = GetDefaultFont();
                phText.fontSize = 13;
                phText.alignment = TextAnchor.MiddleCenter;
                phText.color = Color.white;
                phText.text = "相片";
                phText.raycastTarget = false;
            }

            return albumRect;
        }

        private static GameObject BuildInputBlocker(Transform parent)
        {
            var blockerGo = new GameObject("InputBlocker", typeof(Image));
            blockerGo.transform.SetParent(parent, false);
            var blockerRect = blockerGo.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            var blockerImage = blockerGo.GetComponent<Image>();
            blockerImage.color = new Color(0, 0, 0, 0);
            blockerImage.raycastTarget = true;
            blockerGo.SetActive(false);
            return blockerGo;
        }

        private static RectTransform BuildPreviewPanel(Transform parent, Sprite paperBg)
        {
            var previewGo = new GameObject("PhotoPreview", typeof(Image));
            previewGo.transform.SetParent(parent, false);
            var previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = previewRect.offsetMax = Vector2.zero;
            var bgImg = previewGo.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.03f, 0.02f, 0.9f);

            // 稿纸底图
            var paperGo = new GameObject("PreviewPaperBg", typeof(Image));
            paperGo.transform.SetParent(previewRect, false);
            var paperRect = paperGo.GetComponent<RectTransform>();
            paperRect.anchorMin = paperRect.anchorMax = new Vector2(0.5f, 0.5f);
            paperRect.anchoredPosition = Vector2.zero;
            paperRect.sizeDelta = new Vector2(960, 720);
            var paperImg = paperGo.GetComponent<Image>();
            if (paperBg != null)
            {
                paperImg.sprite = paperBg;
                paperImg.color = Color.white;
            }
            paperImg.raycastTarget = false;

            // 照片
            var photoGo = new GameObject("PreviewPhotoImage", typeof(Image));
            photoGo.transform.SetParent(previewRect, false);
            var photoRect = photoGo.GetComponent<RectTransform>();
            photoRect.anchorMin = photoRect.anchorMax = new Vector2(0.5f, 0.5f);
            photoRect.anchoredPosition = Vector2.zero;
            photoRect.sizeDelta = new Vector2(760, 520);
            var photoImg = photoGo.GetComponent<Image>();
            var previewSprite = LoadSprite("photo_hongqiang");
            if (previewSprite != null)
            {
                photoImg.sprite = previewSprite;
                photoImg.color = Color.white;
                photoImg.preserveAspect = true;
            }
            else
            {
                photoImg.color = new Color(0.62f, 0.48f, 0.3f);
            }

            // 标题文字
            var titleGo = new GameObject("PreviewTitleText", typeof(Text));
            titleGo.transform.SetParent(photoRect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(680, 100);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = GetDefaultFont();
            titleText.fontSize = 40;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "标题";
            titleText.raycastTarget = false;

            // 关闭按钮
            var closeGo = new GameObject("PreviewCloseButtonRoot", typeof(Image), typeof(Button));
            closeGo.transform.SetParent(previewRect, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(700, 390);
            closeRect.sizeDelta = new Vector2(70, 70);
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.color = new Color(0.55f, 0.18f, 0.15f, 1f);
            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            // 关闭按钮文字
            var closeLabel = new GameObject("PreviewCloseButtonLabel", typeof(Text));
            closeLabel.transform.SetParent(closeRect, false);
            var clRect = closeLabel.GetComponent<RectTransform>();
            clRect.anchorMin = Vector2.zero;
            clRect.anchorMax = Vector2.one;
            clRect.offsetMin = clRect.offsetMax = Vector2.zero;
            var clText = closeLabel.GetComponent<Text>();
            clText.font = GetDefaultFont();
            clText.fontSize = 42;
            clText.alignment = TextAnchor.MiddleCenter;
            clText.color = Color.white;
            clText.text = "×";
            clText.raycastTarget = false;

            // 等待文字
            var waitingGo = new GameObject("PreviewWaitingText", typeof(Text));
            waitingGo.transform.SetParent(previewRect, false);
            var wtRect = waitingGo.GetComponent<RectTransform>();
            wtRect.anchorMin = wtRect.anchorMax = new Vector2(0.5f, 0.5f);
            wtRect.anchoredPosition = new Vector2(0, -340);
            wtRect.sizeDelta = new Vector2(600, 60);
            var wtText = waitingGo.GetComponent<Text>();
            wtText.font = GetDefaultFont();
            wtText.fontSize = 24;
            wtText.alignment = TextAnchor.MiddleCenter;
            wtText.color = Color.white;
            wtText.text = "等待 Host 关闭照片预览";
            wtText.raycastTarget = false;
            waitingGo.SetActive(false);

            previewGo.SetActive(false);
            return previewRect;
        }

        // ==============================
        // 自动绑定
        // ==============================

        private static void AutoWireReferences(MemoryAlbumView view, Canvas canvas,
            RectTransform albumPanel, GameObject inputBlocker, Transform root, Sprite paperBg,
            RectTransform preview)
        {
            var so = new SerializedObject(view);

            so.FindProperty("_canvas").objectReferenceValue = canvas;
            so.FindProperty("_albumPanel").objectReferenceValue = albumPanel;
            so.FindProperty("_albumTitleText").objectReferenceValue =
                albumPanel.Find("AlbumTitleText")?.GetComponent<Text>();
            so.FindProperty("_inputBlocker").objectReferenceValue = inputBlocker;

            // 照片槽位数组
            var slotsProp = so.FindProperty("_photoSlots");
            slotsProp.arraySize = SlotCount;
            for (var i = 0; i < SlotCount; i++)
            {
                var elem = slotsProp.GetArrayElementAtIndex(i);

                var paperTr = albumPanel.Find("PaperBg_" + i);
                elem.FindPropertyRelative("paperRoot").objectReferenceValue = paperTr?.gameObject;
                elem.FindPropertyRelative("paperImage").objectReferenceValue = paperTr?.GetComponent<Image>();

                var slotTr = albumPanel.Find("PhotoSlot_" + i);
                elem.FindPropertyRelative("slotRoot").objectReferenceValue = slotTr?.gameObject;
                elem.FindPropertyRelative("photoImage").objectReferenceValue = slotTr?.GetComponent<Image>();
                elem.FindPropertyRelative("photoButton").objectReferenceValue = slotTr?.GetComponent<Button>();

                var phTr = slotTr?.Find("PlaceholderText");
                elem.FindPropertyRelative("placeholderText").objectReferenceValue = phTr?.GetComponent<Text>();
            }

            // 预览面板引用
            so.FindProperty("_previewRoot").objectReferenceValue = preview?.gameObject;
            so.FindProperty("_previewBackground").objectReferenceValue = preview?.GetComponent<Image>();
            so.FindProperty("_previewPaperBg").objectReferenceValue = FindComponent<Image>(preview, "PreviewPaperBg");
            so.FindProperty("_previewPhotoImage").objectReferenceValue = FindComponent<Image>(preview, "PreviewPhotoImage");
            so.FindProperty("_previewTitleText").objectReferenceValue = FindComponent<Text>(preview, "PreviewPhotoImage/PreviewTitleText");
            so.FindProperty("_previewCloseButtonRoot").objectReferenceValue = FindGameObject(preview, "PreviewCloseButtonRoot");
            so.FindProperty("_previewCloseButton").objectReferenceValue = FindComponent<Button>(preview, "PreviewCloseButtonRoot");
            so.FindProperty("_previewCloseButtonImage").objectReferenceValue = FindComponent<Image>(preview, "PreviewCloseButtonRoot");
            so.FindProperty("_previewCloseButtonLabel").objectReferenceValue = FindComponent<Text>(preview, "PreviewCloseButtonRoot/PreviewCloseButtonLabel");
            so.FindProperty("_previewWaitingText").objectReferenceValue = FindGameObject(preview, "PreviewWaitingText");

            so.ApplyModifiedProperties();
            Debug.Log("[MemoryAlbumPrefabExporter] MemoryAlbumView 引用已自动绑定");
        }

        [MenuItem("Tools/UI/Assign Photo Sprites to Memory Album Prefab")]
        public static void AssignPhotoSpritesToExisting()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MemoryAlbumPrefabExporter] 找不到 Prefab: {PrefabPath}");
                return;
            }

            var controller = prefab.GetComponent<MemoryAlbumController>();
            if (controller == null)
            {
                Debug.LogError("[MemoryAlbumPrefabExporter] Prefab 上缺少 MemoryAlbumController");
                return;
            }

            WirePhotoSprites(controller);
            WirePreviewSprites(prefab);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[MemoryAlbumPrefabExporter] 照片 Sprite 已绑定到现有 Prefab");
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }

        [MenuItem("Tools/UI/Assign Preview Sprites to Memory Album Prefab")]
        public static void AssignPreviewSpritesToExisting()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MemoryAlbumPrefabExporter] 找不到 Prefab: {PrefabPath}");
                return;
            }

            WirePreviewSprites(prefab);
            RewireViewReferences(prefab);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[MemoryAlbumPrefabExporter] 预览面板 Sprite 已绑定，可在编辑器中调整位置");
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }

        [MenuItem("Tools/UI/Rewire Memory Album View References")]
        public static void RewireExistingViewReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MemoryAlbumPrefabExporter] 找不到 Prefab: {PrefabPath}");
                return;
            }

            RewireViewReferences(prefab);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[MemoryAlbumPrefabExporter] View 引用已重新绑定");
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
        }

        // ==============================
        // View 引用重新绑定
        // ==============================

        private static void RewireViewReferences(GameObject prefab)
        {
            var view = prefab.GetComponent<MemoryAlbumView>();
            if (view == null)
            {
                Debug.LogError("[MemoryAlbumPrefabExporter] Prefab 上缺少 MemoryAlbumView");
                return;
            }

            var previewTr = prefab.transform.Find("PhotoPreview");
            var albumTr = prefab.transform.Find("MemoryAlbum");

            var so = new SerializedObject(view);

            // 修复嵌套节点引用
            if (previewTr != null)
            {
                var titleText = previewTr.Find("PreviewPhotoImage/PreviewTitleText")?.GetComponent<Text>();
                so.FindProperty("_previewTitleText").objectReferenceValue = titleText;
                Debug.Log($"[MemoryAlbumPrefabExporter] _previewTitleText => {(titleText != null ? "已绑定" : "未找到")}");

                var closeLabel = previewTr.Find("PreviewCloseButtonRoot/PreviewCloseButtonLabel")?.GetComponent<Text>();
                so.FindProperty("_previewCloseButtonLabel").objectReferenceValue = closeLabel;
                Debug.Log($"[MemoryAlbumPrefabExporter] _previewCloseButtonLabel => {(closeLabel != null ? "已绑定" : "未找到")}");
            }

            so.ApplyModifiedProperties();
        }

        // ==============================
        // 照片 Sprite 绑定
        // ==============================

        private static void WirePhotoSprites(MemoryAlbumController controller)
        {
            var so = new SerializedObject(controller);

            var hongqiang = LoadSprite("photo_hongqiang");
            var hongfang = LoadSprite("photo_hongfang");
            var oldFamily = LoadSprite("bagua_old_family_photo");

            so.FindProperty("photo_hongqiang").objectReferenceValue = hongqiang;
            so.FindProperty("photo_hongfang").objectReferenceValue = hongfang;
            so.FindProperty("bagua_old_family_photo").objectReferenceValue = oldFamily;
            so.ApplyModifiedProperties();

            Debug.Log($"[MemoryAlbumPrefabExporter] 照片 Sprite 绑定: " +
                      $"hongqiang={hongqiang?.name}, hongfang={hongfang?.name}, oldFamily={oldFamily?.name}");
        }

        private static void WirePreviewSprites(GameObject prefab)
        {
            var previewTr = prefab.transform.Find("PhotoPreview");
            if (previewTr == null)
            {
                Debug.LogError("[MemoryAlbumPrefabExporter] Prefab 中找不到 PhotoPreview");
                return;
            }

            var paperBg = LoadPaperBgSprite();
            var photoSprite = LoadSprite("photo_hongqiang");

            // PreviewPaperBg
            var paperImg = previewTr.Find("PreviewPaperBg")?.GetComponent<Image>();
            if (paperImg != null && paperBg != null)
            {
                paperImg.sprite = paperBg;
                paperImg.color = Color.white;
                EditorUtility.SetDirty(paperImg);
            }

            // PreviewPhotoImage — 启用并绑定占位 sprite
            var photoImg = previewTr.Find("PreviewPhotoImage")?.GetComponent<Image>();
            if (photoImg != null)
            {
                photoImg.enabled = true;
                if (photoSprite != null)
                {
                    photoImg.sprite = photoSprite;
                    photoImg.color = Color.white;
                    photoImg.preserveAspect = true;
                }
                EditorUtility.SetDirty(photoImg);
            }

            // PreviewTitleText — 设置占位文字
            var titleTr = previewTr.Find("PreviewPhotoImage/PreviewTitleText");
            var titleText = titleTr?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = "照片标题预览";
                EditorUtility.SetDirty(titleText);
            }

            // PreviewCloseButtonLabel — 启用
            var closeLabelTr = previewTr.Find("PreviewCloseButtonRoot/PreviewCloseButtonLabel");
            var closeLabel = closeLabelTr?.GetComponent<Text>();
            if (closeLabel != null)
            {
                closeLabel.enabled = true;
                EditorUtility.SetDirty(closeLabel);
            }

            Debug.Log("[MemoryAlbumPrefabExporter] 预览面板元素已绑定 Sprite 并启用");
        }

        private static Sprite LoadSprite(string assetName)
        {
            var path = $"Assets/_Project/Art/Resources/{assetName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                var guids = AssetDatabase.FindAssets(assetName);
                if (guids.Length > 0)
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (sprite == null)
                Debug.LogWarning($"[MemoryAlbumPrefabExporter] 找不到 Sprite: {assetName}");
            return sprite;
        }

        // ==============================
        // 辅助方法
        // ==============================

        private static Vector2 GetSlotCenter(int index)
        {
            return new Vector2(40 + index * 120 + 50, -80);
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

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static Sprite LoadPaperBgSprite()
        {
            var guids = AssetDatabase.FindAssets("paper_bg");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void EnsureDirectoryExists()
        {
            var dir = "Assets/_Project/Resources/UIPrefabs";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "UIPrefabs");
            }
        }
    }
}
#endif
