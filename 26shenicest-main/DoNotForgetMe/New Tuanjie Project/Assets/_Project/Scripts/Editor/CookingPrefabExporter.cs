#if UNITY_EDITOR
using System;
using System.Reflection;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 做饭小游戏 Prefab 导出工具。
    /// 通过旧代码的渲染逻辑捕获 UI 层级，生成可编辑的 prefab 文件。
    ///
    /// 使用方法：菜单 Tools > MiniGame > Export Cooking Prefab
    /// 前提：RecipeConfig 已通过 Tools > 3C Setup 创建并配置好 sprite。
    /// </summary>
    public static class CookingPrefabExporter
    {
        private const string PrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/CookingView.prefab";
        private const string TomatoEggPrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/CookingView_TomatoEgg.prefab";
        private const string CucumberSaladPrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/CookingView_CucumberSalad.prefab";
        private const string MenuItemPath = "Tools/MiniGame/Export Cooking Prefab";
        private const string MenuItemTomatoEgg = "Tools/MiniGame/Export Cooking Prefab (Tomato Egg)";
        private const string MenuItemCucumberSalad = "Tools/MiniGame/Export Cooking Prefab (Cucumber Salad)";

        [MenuItem(MenuItemPath)]
        public static void Export()
        {
            ExportInternal(PrefabPath, LoadFirstRecipeConfig());
        }

        [MenuItem(MenuItemTomatoEgg)]
        public static void ExportTomatoEgg()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeConfig>(
                "Assets/_Project/Settings/TomatoEggRecipe.asset");
            ExportInternal(TomatoEggPrefabPath, recipe);

            // 更新 RecipeConfig.viewPrefabName
            if (recipe != null) SetViewPrefabName(recipe, "MiniGamePrefabs/CookingView_TomatoEgg");
        }

        [MenuItem(MenuItemCucumberSalad)]
        public static void ExportCucumberSalad()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeConfig>(
                "Assets/_Project/Settings/CucumberSaladRecipe.asset");
            ExportInternal(CucumberSaladPrefabPath, recipe);

            // 更新 RecipeConfig.viewPrefabName
            if (recipe != null) SetViewPrefabName(recipe, "MiniGamePrefabs/CookingView_CucumberSalad");
        }

        private static void SetViewPrefabName(RecipeConfig recipe, string prefabName)
        {
            var so = new SerializedObject(recipe);
            so.FindProperty("viewPrefabName").stringValue = prefabName;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CookingPrefabExporter] {recipe.name} viewPrefabName 已设置为 {prefabName}");
        }

        private static void ExportInternal(string prefabPath, RecipeConfig recipe)
        {
            // 1. 校验 RecipeConfig
            if (recipe == null)
            {
                EditorUtility.DisplayDialog("导出失败",
                    "未找到 RecipeConfig.asset。请先运行 Tools > 3C Setup > Create Basic Scene 生成配置。", "确定");
                return;
            }

            // 2. 创建临时 Canvas
            var tempCanvas = CreateTempCanvas();

            try
            {
                // 3. 构建 prefab 根节点
                var root = new GameObject("CookingView", typeof(RectTransform));
                root.transform.SetParent(tempCanvas.transform, false);
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;

                // 4. 构建完整层级
                BuildHierarchy(root, recipe);

                // 5. 添加 CookingView 组件并自动绑定
                var view = root.AddComponent<CookingView>();
                AutoWireReferences(root, view);

                // 6. 保存为 prefab
                EnsureDirectoryExists();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[CookingPrefabExporter] Prefab 已保存到 {prefabPath}");

                // 7. 选中 prefab 供检查
                AssetDatabase.Refresh();
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorGUIUtility.PingObject(prefabAsset);
                Selection.activeObject = prefabAsset;
            }
            finally
            {
                // 清理临时 Canvas（销毁整个 GameObject，避免组件依赖冲突）
                if (tempCanvas != null) UnityEngine.Object.DestroyImmediate(tempCanvas.gameObject);
            }
        }

        // ==============================
        // 层级构建
        // ==============================

        private static void BuildHierarchy(GameObject root, RecipeConfig recipe)
        {
            var rootTransform = root.transform;

            // --- 共享 ---
            // 背景（电影比例 7:10，上下各留 15% 黑边）
            var bg = CreateUIObject("Background", rootTransform);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.15f);
            bgRect.anchorMax = new Vector2(1, 0.85f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = recipe.CookingBackground;
            bgImg.color = Color.white;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            var bgArf = bg.AddComponent<AspectRatioFitter>();
            bgArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (recipe.CookingBackground != null)
                bgArf.aspectRatio = (float)recipe.CookingBackground.rect.width
                                   / recipe.CookingBackground.rect.height;

            // 等待联机文字
            var waiting = CreateTextObject("WaitingText", rootTransform, "等待联机角色…", 40, Color.white, new Vector2(0, 0));
            waiting.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);

            // --- 母亲端 Panel ---
            var motherPanel = CreateUIObject("MotherPanel", rootTransform);
            StretchRect(motherPanel.GetComponent<RectTransform>());

            // 母亲端：角色文字
            var motherRole = CreateTextObject("MotherRoleText", motherPanel.transform,
                "母亲端 · " + recipe.MotherTaskText, 42, Color.white, new Vector2(0, 460));
            motherRole.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 母亲端：指令文字
            var motherInstruction = CreateTextObject("MotherInstructionText", motherPanel.transform,
                "把需要的食材拖进" + recipe.ContainerDisplayName + "里", 30,
                new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 340));
            motherInstruction.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 母亲端：锅容器 DropZone
            var containerZone = CreateUIObject("MotherContainerZone", motherPanel.transform);
            var czRect = containerZone.GetComponent<RectTransform>();
            czRect.anchorMin = czRect.anchorMax = new Vector2(0.5f, 0.5f);
            czRect.anchoredPosition = new Vector2(0, 60);
            czRect.sizeDelta = new Vector2(500, 360);
            var czImg = containerZone.AddComponent<Image>();
            czImg.color = new Color(1, 1, 1, 0);
            czImg.raycastTarget = false;

            // 母亲端：已放入食材名
            var droppedNames = CreateTextObject("MotherDroppedNamesText", motherPanel.transform,
                "", 28, new Color(0.9f, 0.85f, 0.7f, 0.8f), new Vector2(0, 60));
            droppedNames.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 母亲端：4个食材槽（固定 4 个，取所有配方中食材数的最大值）
            var ingredientIds = new System.Collections.Generic.List<string>();
            ingredientIds.AddRange(recipe.RequiredIngredients);
            ingredientIds.AddRange(recipe.DistractorIngredients);
            for (var i = 0; i < 4; i++)
            {
                var id = i < ingredientIds.Count ? ingredientIds[i] : "placeholder_" + i;
                var sprite = i < ingredientIds.Count ? recipe.GetIngredientSprite(id) : null;
                var posX = -480 + i * 380;
                CreateIngredientSlot("MotherIngredientSlot_" + i, motherPanel.transform,
                    sprite, new Vector2(posX, -340), id);
            }

            // 母亲端：等待提示图（"休息一下~等待女儿操作"）
            var motherWaitingImg = CreateUIObject("MotherWaitingImage", motherPanel.transform);
            var mwImgRect = motherWaitingImg.GetComponent<RectTransform>();
            mwImgRect.anchorMin = mwImgRect.anchorMax = new Vector2(0.5f, 0.5f);
            mwImgRect.anchoredPosition = new Vector2(0, 120);
            mwImgRect.sizeDelta = new Vector2(600, 200);
            var mwImg = motherWaitingImg.AddComponent<Image>();
            mwImg.color = new Color(1f, 1f, 1f, 0);
            mwImg.raycastTarget = false;

            // 母亲端：菜谱改痕
            var recipeNote = CreateTextObject("MotherRecipeNoteText", motherPanel.transform,
                "菜谱改痕：" + recipe.RecipeNote, 30,
                new Color(0.9f, 0.82f, 0.65f), Vector2.zero);
            recipeNote.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 母亲端：提示图
            var motherHint = CreateUIObject("MotherHintImage", motherPanel.transform);
            var mhRect = motherHint.GetComponent<RectTransform>();
            mhRect.anchorMin = mhRect.anchorMax = new Vector2(0.5f, 0.5f);
            mhRect.anchoredPosition = new Vector2(0, -280);
            mhRect.sizeDelta = new Vector2(500, 290);
            var mhImg = motherHint.AddComponent<Image>();
            mhImg.sprite = recipe.MotherCompleteHint;
            mhImg.color = Color.white;
            mhImg.preserveAspect = true;
            mhImg.raycastTarget = false;

            // 母亲端：完成文字
            var motherComplete = CreateTextObject("MotherCompleteText", motherPanel.transform,
                "你们一起完成了这道菜。", 48, new Color(0.9f, 0.7f, 0.4f), new Vector2(0, 250));
            motherComplete.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 母亲端：菜品照片
            var motherDish = CreateUIObject("MotherDishPhoto", motherPanel.transform);
            var mdRect = motherDish.GetComponent<RectTransform>();
            mdRect.anchorMin = mdRect.anchorMax = new Vector2(0.5f, 0.5f);
            mdRect.anchoredPosition = new Vector2(0, -20);
            mdRect.sizeDelta = new Vector2(500, 360);
            var mdImg = motherDish.AddComponent<Image>();
            mdImg.sprite = recipe.DishPhotoSprite;
            mdImg.color = Color.white;
            mdImg.preserveAspect = true;
            mdImg.raycastTarget = false;

            // --- 女儿端 Panel ---
            var daughterPanel = CreateUIObject("DaughterPanel", rootTransform);
            StretchRect(daughterPanel.GetComponent<RectTransform>());

            // 女儿端：角色文字
            var daughterRole = CreateTextObject("DaughterRoleText", daughterPanel.transform,
                "女儿端 · " + recipe.DaughterTaskText, 42, Color.white, new Vector2(0, 460));
            daughterRole.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 女儿端：等待提示图（"休息一下~等待母亲操作"）
            var daughterWaitingImg = CreateUIObject("DaughterWaitingImage", daughterPanel.transform);
            var dwImgRect = daughterWaitingImg.GetComponent<RectTransform>();
            dwImgRect.anchorMin = dwImgRect.anchorMax = new Vector2(0.5f, 0.5f);
            dwImgRect.anchoredPosition = Vector2.zero;
            dwImgRect.sizeDelta = new Vector2(600, 200);
            var dwImg = daughterWaitingImg.AddComponent<Image>();
            dwImg.color = new Color(1f, 1f, 1f, 0);
            dwImg.raycastTarget = false;

            // 女儿端：指令文字
            var daughterInstruction = CreateTextObject("DaughterInstructionText", daughterPanel.transform,
                "拖入正确的调料", 30, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 340));
            daughterInstruction.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 女儿端：菜 DropZone
            var dishZone = CreateUIObject("DaughterDishZone", daughterPanel.transform);
            var dzRect = dishZone.GetComponent<RectTransform>();
            dzRect.anchorMin = dzRect.anchorMax = new Vector2(0.5f, 0.5f);
            dzRect.anchoredPosition = new Vector2(500, 60);
            dzRect.sizeDelta = new Vector2(640, 460);
            var dzImg = dishZone.AddComponent<Image>();
            dzImg.color = new Color(1, 1, 1, 0);
            dzImg.raycastTarget = false;

            // 女儿端：菜图
            var daughterDish = CreateUIObject("DaughterDishPhoto", daughterPanel.transform);
            var ddRect = daughterDish.GetComponent<RectTransform>();
            ddRect.anchorMin = ddRect.anchorMax = new Vector2(0.5f, 0.5f);
            ddRect.anchoredPosition = new Vector2(500, 60);
            ddRect.sizeDelta = new Vector2(640, 460);
            var ddImg = daughterDish.AddComponent<Image>();
            ddImg.sprite = recipe.DishPhotoSprite;
            ddImg.color = Color.white;
            ddImg.preserveAspect = true;
            ddImg.raycastTarget = false;

            // 女儿端：2个调料槽（固定 2 个）
            var seasonings = recipe.SeasoningOptions;
            var seasoningSpacing = 540f / Mathf.Max(seasonings.Length, 1);
            for (var i = 0; i < 2; i++)
            {
                var id = i < seasonings.Length ? seasonings[i] : "placeholder_" + i;
                var sprite = i < seasonings.Length ? recipe.GetIngredientSprite(id) : null;
                var posX = -270f + i * seasoningSpacing;
                CreateIngredientSlot("DaughterSeasoningSlot_" + i, daughterPanel.transform,
                    sprite, new Vector2(posX, -340), id);
            }

            // 女儿端：完成文字
            var daughterComplete = CreateTextObject("DaughterCompleteText", daughterPanel.transform,
                "你们一起完成了这道菜。", 44, new Color(0.9f, 0.7f, 0.4f), new Vector2(0, 250));
            daughterComplete.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 女儿端：奖励照片
            var rewardPhoto = CreateUIObject("RewardPhotoImage", daughterPanel.transform);
            var rpRect = rewardPhoto.GetComponent<RectTransform>();
            rpRect.anchorMin = rpRect.anchorMax = new Vector2(0.5f, 0.5f);
            rpRect.anchoredPosition = new Vector2(0, 40);
            rpRect.sizeDelta = new Vector2(500, 380);
            var rpImg = rewardPhoto.AddComponent<Image>();
            rpImg.sprite = recipe.RewardPhotoSprite;
            rpImg.color = Color.white;
            rpImg.preserveAspect = true;
            rpImg.raycastTarget = false;

            // 女儿端：照片标签
            var photoLabel = CreateTextObject("PhotoLabelText", rewardPhoto.transform,
                "获得照片", 24, new Color(0.9f, 0.85f, 0.65f), new Vector2(0, -8));
            var plRect = photoLabel.GetComponent<RectTransform>();
            plRect.anchorMin = plRect.anchorMax = new Vector2(0.5f, 0);
            plRect.pivot = new Vector2(0.5f, 1);
            plRect.sizeDelta = new Vector2(340, 40);

            // 女儿端：收集按钮
            var collectBtn = CreateUIObject("CollectButtonRoot", daughterPanel.transform);
            var cbRect = collectBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.5f, 0.5f);
            cbRect.anchoredPosition = new Vector2(0, -200);
            cbRect.sizeDelta = new Vector2(280, 80);
            var cbImg = collectBtn.AddComponent<Image>();
            cbImg.color = new Color(0.85f, 0.65f, 0.2f, 0.95f);
            var cbButton = collectBtn.AddComponent<Button>();
            cbButton.targetGraphic = cbImg;
            collectBtn.AddComponent<ButtonHoverEffect>();

            // 收集按钮文字
            var cbLabel = CreateTextObject("CollectButtonLabel", collectBtn.transform,
                "收集照片", 30, Color.white, Vector2.zero);
            var cblRect = cbLabel.GetComponent<RectTransform>();
            cblRect.anchorMin = Vector2.zero;
            cblRect.anchorMax = Vector2.one;
            cblRect.offsetMin = cblRect.offsetMax = Vector2.zero;

            // 女儿端：收集按钮呼吸光
            var collectGlow = CreateUIObject("CollectGlowImage", daughterPanel.transform);
            var cgRect = collectGlow.GetComponent<RectTransform>();
            cgRect.anchorMin = cgRect.anchorMax = new Vector2(0.5f, 0.5f);
            cgRect.anchoredPosition = new Vector2(0, -200);
            cgRect.sizeDelta = new Vector2(340, 140);
            var cgImg = collectGlow.AddComponent<Image>();
            cgImg.color = new Color(1f, 0.78f, 0.28f, 0.25f);
            cgImg.raycastTarget = false;

            // 女儿端：已收集文字
            var collected = CreateTextObject("CollectedText", daughterPanel.transform,
                "照片已收集", 36, new Color(0.7f, 0.8f, 0.5f), new Vector2(0, -100));
            collected.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);
        }

        // ==============================
        // 辅助方法
        // ==============================

        private static GameObject CreateIngredientSlot(string name, Transform parent,
            Sprite sprite, Vector2 position, string itemId)
        {
            var go = new GameObject(name, typeof(Image), typeof(DraggableItem));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(380, 260);

            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.7f, 0.58f, 0.4f);
            }
            img.raycastTarget = true;

            return go;
        }

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

        private static RecipeConfig LoadFirstRecipeConfig()
        {
            // 优先用 TomatoEggRecipe（4 个食材），确保 Prefab 槽位数量取最大值
            var tomatoEgg = AssetDatabase.LoadAssetAtPath<RecipeConfig>(
                "Assets/_Project/Settings/TomatoEggRecipe.asset");
            if (tomatoEgg != null) return tomatoEgg;

            var guids = AssetDatabase.FindAssets("t:RecipeConfig");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RecipeConfig>(path);
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

        /// <summary>
        /// 使用 SerializedObject 自动绑定 CookingView 的 [SerializeField] 字段到 prefab 内的对应元素。
        /// 通过字段名与 GameObject 名的映射关系进行查找。
        /// </summary>
        private static void AutoWireReferences(GameObject root, CookingView view)
        {
            var so = new SerializedObject(view);
            var rootTransform = root.transform;

            // 共享
            so.FindProperty("_background").objectReferenceValue =
                FindComponent<Image>(rootTransform, "Background");
            so.FindProperty("_waitingText").objectReferenceValue =
                FindComponent<Text>(rootTransform, "WaitingText");

            // 母亲端
            var motherPanel = rootTransform.Find("MotherPanel");
            so.FindProperty("_motherPanel").objectReferenceValue =
                motherPanel?.gameObject;
            so.FindProperty("_motherRoleText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherRoleText");
            so.FindProperty("_motherInstructionText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherInstructionText");
            so.FindProperty("_motherContainerZone").objectReferenceValue =
                FindComponent<RectTransform>(motherPanel, "MotherContainerZone");
            so.FindProperty("_motherDroppedNamesText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherDroppedNamesText");
            so.FindProperty("_motherWaitingText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherWaitingText");
            so.FindProperty("_motherWaitingImage").objectReferenceValue =
                FindComponent<Image>(motherPanel, "MotherWaitingImage");
            so.FindProperty("_motherRecipeNoteText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherRecipeNoteText");
            so.FindProperty("_motherHintImage").objectReferenceValue =
                FindComponent<Image>(motherPanel, "MotherHintImage");
            so.FindProperty("_motherCompleteText").objectReferenceValue =
                FindComponent<Text>(motherPanel, "MotherCompleteText");
            so.FindProperty("_motherDishPhoto").objectReferenceValue =
                FindComponent<Image>(motherPanel, "MotherDishPhoto");

            // 母亲端食材槽数组
            WireIngredientSlots(so, "_motherIngredientSlots", motherPanel, "MotherIngredientSlot_");

            // 女儿端
            var daughterPanel = rootTransform.Find("DaughterPanel");
            so.FindProperty("_daughterPanel").objectReferenceValue =
                daughterPanel?.gameObject;
            so.FindProperty("_daughterRoleText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "DaughterRoleText");
            so.FindProperty("_daughterWaitingText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "DaughterWaitingText");
            so.FindProperty("_daughterWaitingImage").objectReferenceValue =
                FindComponent<Image>(daughterPanel, "DaughterWaitingImage");
            so.FindProperty("_daughterInstructionText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "DaughterInstructionText");
            so.FindProperty("_daughterDishZone").objectReferenceValue =
                FindComponent<RectTransform>(daughterPanel, "DaughterDishZone");
            so.FindProperty("_daughterDishPhoto").objectReferenceValue =
                FindComponent<Image>(daughterPanel, "DaughterDishPhoto");
            so.FindProperty("_daughterCompleteText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "DaughterCompleteText");
            so.FindProperty("_rewardPhotoImage").objectReferenceValue =
                FindComponent<Image>(daughterPanel, "RewardPhotoImage");
            so.FindProperty("_photoLabelText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "PhotoLabelText");
            so.FindProperty("_collectButtonRoot").objectReferenceValue =
                FindGameObject(daughterPanel, "CollectButtonRoot");
            so.FindProperty("_collectButton").objectReferenceValue =
                FindComponent<Button>(daughterPanel, "CollectButtonRoot");
            so.FindProperty("_collectButtonImage").objectReferenceValue =
                FindComponent<Image>(daughterPanel, "CollectButtonRoot");
            so.FindProperty("_collectButtonLabel").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "CollectButtonLabel");
            so.FindProperty("_collectGlowImage").objectReferenceValue =
                FindComponent<Image>(daughterPanel, "CollectGlowImage");
            so.FindProperty("_collectedText").objectReferenceValue =
                FindComponent<Text>(daughterPanel, "CollectedText");

            // 女儿端调料槽数组
            WireIngredientSlots(so, "_daughterSeasoningSlots", daughterPanel, "DaughterSeasoningSlot_");

            so.ApplyModifiedProperties();
            Debug.Log("[CookingPrefabExporter] CookingView 引用已自动绑定");
        }

        private static void WireIngredientSlots(SerializedObject so, string fieldName,
            Transform parent, string namePrefix)
        {
            if (parent == null) return;
            var slotsProp = so.FindProperty(fieldName);
            if (slotsProp == null) return;

            var count = 0;
            for (var i = 0; i < 4; i++)
            {
                var child = parent.Find(namePrefix + i);
                if (child == null) break;
                count++;
            }

            slotsProp.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var child = parent.Find(namePrefix + i);
                var element = slotsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = child.gameObject;
                element.FindPropertyRelative("image").objectReferenceValue =
                    child.GetComponent<Image>();
                element.FindPropertyRelative("draggable").objectReferenceValue =
                    child.GetComponent<DraggableItem>();
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
