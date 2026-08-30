#if UNITY_EDITOR
using DoNotForgetMe.Cutscene;
using DoNotForgetMe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 庭院偷听视图 Prefab 导出工具。
    /// 将原来代码构建的 Canvas + "进入偷听"按钮生成为可编辑的 prefab。
    ///
    /// 使用方法：菜单 Tools > Scene > Export Courtyard Eavesdrop Prefab
    /// 前提：确保 ButtonHoverEffect 脚本可编译。
    /// </summary>
    public static class CourtyardEavesdropViewExporter
    {
        private const string PrefabPath = "Assets/_Project/Resources/ScenePrefabs/CourtyardEavesdropView.prefab";
        private const string MenuItemPath = "Tools/Scene/Export Courtyard Eavesdrop Prefab";

        [MenuItem(MenuItemPath)]
        public static void Export()
        {
            var tempCanvas = CreateTempCanvas();

            try
            {
                // --- 根节点 (CourtyardEavesdropView) ---
                var root = new GameObject("CourtyardEavesdropView", typeof(RectTransform));
                root.transform.SetParent(tempCanvas.transform, false);
                var rootRect = root.GetComponent<RectTransform>();
                StretchRect(rootRect);

                // --- Canvas 子节点 ---
                var canvasGo = new GameObject("CourtyardEavesdropCanvas",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(root.transform, false);
                StretchRect(canvasGo.GetComponent<RectTransform>());

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                // --- "进入偷听"按钮 ---
                var enterBtnGo = new GameObject("EnterEavesdropBtn",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                enterBtnGo.transform.SetParent(canvasGo.transform, false);
                var enterBtnRect = enterBtnGo.GetComponent<RectTransform>();
                enterBtnRect.anchorMin = enterBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
                enterBtnRect.anchoredPosition = new Vector2(0, -400);
                enterBtnRect.sizeDelta = new Vector2(360, 100);

                var enterBtnImg = enterBtnGo.GetComponent<Image>();
                enterBtnImg.color = new Color(0.5f, 0.35f, 0.15f, 0.95f);

                var enterButton = enterBtnGo.GetComponent<Button>();
                enterButton.targetGraphic = enterBtnImg;

                enterBtnGo.AddComponent<ButtonHoverEffect>();

                // --- "进入偷听"按钮文字 ---
                var enterLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                enterLabelGo.transform.SetParent(enterBtnGo.transform, false);
                var enterLabelRect = enterLabelGo.GetComponent<RectTransform>();
                enterLabelRect.anchorMin = Vector2.zero;
                enterLabelRect.anchorMax = Vector2.one;
                enterLabelRect.offsetMin = enterLabelRect.offsetMax = Vector2.zero;

                var enterLabelText = enterLabelGo.GetComponent<Text>();
                enterLabelText.font = GetDefaultFont();
                enterLabelText.fontSize = 38;
                enterLabelText.color = Color.white;
                enterLabelText.alignment = TextAnchor.MiddleCenter;
                enterLabelText.text = "进入偷听";
                enterLabelText.raycastTarget = false;

                // --- "结束偷听"按钮（正下方） ---
                var exitBtnGo = new GameObject("ExitEavesdropBtn",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                exitBtnGo.transform.SetParent(canvasGo.transform, false);
                var exitBtnRect = exitBtnGo.GetComponent<RectTransform>();
                exitBtnRect.anchorMin = exitBtnRect.anchorMax = new Vector2(0.5f, 0f);
                exitBtnRect.anchoredPosition = new Vector2(0, 80);
                exitBtnRect.sizeDelta = new Vector2(360, 100);

                var exitBtnImg = exitBtnGo.GetComponent<Image>();
                exitBtnImg.color = new Color(0.5f, 0.35f, 0.15f, 0.95f);

                var exitButton = exitBtnGo.GetComponent<Button>();
                exitButton.targetGraphic = exitBtnImg;

                exitBtnGo.AddComponent<ButtonHoverEffect>();

                // --- "结束偷听"按钮文字 ---
                var exitLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                exitLabelGo.transform.SetParent(exitBtnGo.transform, false);
                var exitLabelRect = exitLabelGo.GetComponent<RectTransform>();
                exitLabelRect.anchorMin = Vector2.zero;
                exitLabelRect.anchorMax = Vector2.one;
                exitLabelRect.offsetMin = exitLabelRect.offsetMax = Vector2.zero;

                var exitLabelText = exitLabelGo.GetComponent<Text>();
                exitLabelText.font = GetDefaultFont();
                exitLabelText.fontSize = 38;
                exitLabelText.color = Color.white;
                exitLabelText.alignment = TextAnchor.MiddleCenter;
                exitLabelText.text = "结束偷听";
                exitLabelText.raycastTarget = false;

                // --- 添加 CourtyardEavesdropView 组件并绑定引用 ---
                var view = root.AddComponent<CourtyardEavesdropView>();
                var so = new SerializedObject(view);
                so.FindProperty("_enterButton").objectReferenceValue = enterButton;
                so.FindProperty("_exitButton").objectReferenceValue = exitButton;
                so.ApplyModifiedProperties();

                // --- 保存为 prefab ---
                EnsureDirectoryExists();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[CourtyardEavesdropViewExporter] Prefab 已保存到 {PrefabPath}");

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
        // 辅助方法
        // ==============================

        private static void StretchRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
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

        private static void EnsureDirectoryExists()
        {
            var dir = "Assets/_Project/Resources/ScenePrefabs";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "ScenePrefabs");
            }
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
#endif
