#if UNITY_EDITOR
using System.Collections.Generic;
using DoNotForgetMe.MiniGame.Bagua;
using DoNotForgetMe.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.EditorTools
{
    /// <summary>
    /// 八卦小游戏 Prefab 导出工具。
    /// 使用方法：菜单 Tools > MiniGame > Export Bagua Prefab
    /// </summary>
    public static class BaguaPrefabExporter
    {
        private const string PrefabPath = "Assets/_Project/Resources/MiniGamePrefabs/BaguaView.prefab";
        private const string MenuItemPath = "Tools/MiniGame/Export Bagua Prefab";

        [MenuItem(MenuItemPath)]
        public static void Export()
        {
            var config = LoadFirstBaguaConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("导出失败",
                    "未找到 BaguaStoryConfig.asset。请先运行 Tools > 3C Setup > Create Basic Scene 生成配置。", "确定");
                return;
            }

            var tempCanvas = CreateTempCanvas();
            try
            {
                var root = new GameObject("BaguaView", typeof(RectTransform));
                root.transform.SetParent(tempCanvas.transform, false);
                var rootRect = root.GetComponent<RectTransform>();
                StretchRect(rootRect);

                BuildHierarchy(root, config);

                var view = root.AddComponent<BaguaView>();
                AutoWireReferences(root, view, config);

                EnsureDirectoryExists();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[BaguaPrefabExporter] Prefab 已保存到 {PrefabPath}");

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

        private static void BuildHierarchy(GameObject root, BaguaStoryConfig config)
        {
            var rootTransform = root.transform;

            // --- 共享 ---
            var bg = CreateUIObject("Background", rootTransform);
            var bgRect = bg.GetComponent<RectTransform>();
            StretchRect(bgRect);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = config.DeskBackground;
            bgImg.color = Color.white;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;
            var bgArf = bg.AddComponent<AspectRatioFitter>();
            bgArf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (config.DeskBackground != null)
                bgArf.aspectRatio = (float)config.DeskBackground.rect.width
                                   / config.DeskBackground.rect.height;

            var waiting = CreateTextObject("WaitingText", rootTransform, "等待联机角色…", 40, Color.white, new Vector2(0, 0));
            waiting.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);

            // --- 字幕条 ---
            var subtitleBar = CreateUIObject("SubtitleBarRoot", rootTransform);
            var sbRect = subtitleBar.GetComponent<RectTransform>();
            sbRect.anchorMin = sbRect.anchorMax = new Vector2(0.5f, 0);
            sbRect.anchoredPosition = new Vector2(0, 20);
            sbRect.sizeDelta = new Vector2(1600, 100);
            var sbImg = subtitleBar.AddComponent<Image>();
            sbImg.color = new Color(0.05f, 0.04f, 0.03f, 0.92f);
            sbImg.raycastTarget = false;

            var subtitleText = CreateTextObject("SubtitleText", subtitleBar.transform, "", 30,
                new Color(0.95f, 0.92f, 0.8f), Vector2.zero);
            var stRect = subtitleText.GetComponent<RectTransform>();
            stRect.sizeDelta = new Vector2(1500, 80);
            subtitleBar.SetActive(false);

            // --- 母亲端 Panel ---
            var clientPanel = CreateUIObject("ClientPanel", rootTransform);
            StretchRect(clientPanel.GetComponent<RectTransform>());

            // 任务横幅
            var taskBanner = CreateTextObject("TaskBannerText", clientPanel.transform,
                "点击人物听八卦，把听到的物品拖到对应的人物吧！", 28,
                new Color(0.9f, 0.85f, 0.65f), new Vector2(0, 430));
            taskBanner.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 60);

            // 桌面背景
            var tray = CreateUIObject("DesktopTrayImage", clientPanel.transform);
            var trayRect = tray.GetComponent<RectTransform>();
            StretchRect(trayRect);
            var trayImg = tray.AddComponent<Image>();
            if (config.DeskBackground != null)
            {
                trayImg.sprite = config.DeskBackground;
                trayImg.color = Color.white;
            }
            else
            {
                trayImg.color = new Color(0.28f, 0.2f, 0.12f, 0.9f);
            }
            trayImg.raycastTarget = false;

            // 桌面物件
            var placements = config.ItemPlacements;
            if (placements != null)
            {
                for (var i = 0; i < placements.Length; i++)
                {
                    var item = placements[i];
                    CreateBaguaItemSlot("DesktopItemSlot_" + i, clientPanel.transform,
                        item.sprite, item.anchoredPosition + new Vector2(0, 60), item.itemId);
                }
            }

            // 人物卡
            var entries = config.Entries;
            if (entries != null)
            {
                var spacing = 560;
                var startX = -(entries.Length - 1) * spacing / 2f;
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var cardPos = new Vector2(startX + i * spacing, -320);
                    CreateCharacterCard("CharacterCard_" + i, clientPanel.transform, entry, cardPos, config);
                }
            }

            // 母亲端等待文字
            var clientWaiting = CreateTextObject("ClientWaitingText", clientPanel.transform,
                "配对完成，等待女儿认人…", 34, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, -100));
            clientWaiting.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // --- 女儿端 Panel ---
            var hostPanel = CreateUIObject("HostPanel", rootTransform);
            StretchRect(hostPanel.GetComponent<RectTransform>());

            // 角色文字
            var hostRole = CreateTextObject("HostRoleText", hostPanel.transform,
                "女儿端 · 八卦旧事", 42, Color.white, new Vector2(0, 460));
            hostRole.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 等待文字
            var hostWaiting = CreateTextObject("HostWaitingText", hostPanel.transform,
                "等待母亲听故事并配对物品…", 34, new Color(0.85f, 0.8f, 0.7f), Vector2.zero);
            hostWaiting.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 照片背景
            var photoBg = CreateUIObject("PhotoBackgroundImage", hostPanel.transform);
            var pbRect = photoBg.GetComponent<RectTransform>();
            pbRect.anchorMin = pbRect.anchorMax = new Vector2(0.5f, 0.5f);
            pbRect.anchoredPosition = new Vector2(0, 60);
            pbRect.sizeDelta = new Vector2(1500, 800);
            var pbImg = photoBg.AddComponent<Image>();
            pbImg.sprite = config.DaughterPhotoBackground;
            pbImg.color = Color.white;
            pbImg.preserveAspect = true;
            pbImg.raycastTarget = false;

            // 照片指令
            var photoInstruction = CreateTextObject("PhotoInstructionText", hostPanel.transform,
                "把姓名标签拖到照片中对应的人", 28, new Color(0.85f, 0.8f, 0.7f), new Vector2(0, 460));
            photoInstruction.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 照片投放区
            var zones = config.PhotoZones;
            if (zones != null)
            {
                for (var i = 0; i < zones.Length; i++)
                {
                    var zone = zones[i];
                    CreatePhotoZone("PhotoZone_" + i, hostPanel.transform, config, zone);
                }
            }

            // 姓名标签
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var tagPos = new Vector2(-350 + i * 350, -380);
                    CreateNameTagSlot("NameTagSlot_" + i, hostPanel.transform, entry, tagPos);
                }
            }

            // --- 完成视图 ---
            var completeText = CreateTextObject("CompleteText", rootTransform,
                "你们一起完成了八卦旧事。", 44, new Color(0.9f, 0.7f, 0.4f), new Vector2(0, 250));
            completeText.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 100);

            // 奖励照片
            var rewardPhoto = CreateUIObject("RewardPhotoImage", rootTransform);
            var rpRect = rewardPhoto.GetComponent<RectTransform>();
            rpRect.anchorMin = rpRect.anchorMax = new Vector2(0.5f, 0.5f);
            rpRect.anchoredPosition = new Vector2(0, 40);
            rpRect.sizeDelta = new Vector2(500, 380);
            var rpImg = rewardPhoto.AddComponent<Image>();
            rpImg.sprite = config.OldFamilyPhoto;
            rpImg.color = Color.white;
            rpImg.preserveAspect = true;
            rpImg.raycastTarget = false;

            var photoLabel = CreateTextObject("PhotoLabelText", rewardPhoto.transform,
                "获得照片", 24, new Color(0.9f, 0.85f, 0.65f), new Vector2(0, -8));
            var plRect = photoLabel.GetComponent<RectTransform>();
            plRect.anchorMin = plRect.anchorMax = new Vector2(0.5f, 0);
            plRect.pivot = new Vector2(0.5f, 1);
            plRect.sizeDelta = new Vector2(340, 40);

            // 收集按钮
            var collectBtn = CreateUIObject("CollectButtonRoot", rootTransform);
            var cbRect = collectBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.5f, 0.5f);
            cbRect.anchoredPosition = new Vector2(0, -200);
            cbRect.sizeDelta = new Vector2(280, 80);
            var cbImg = collectBtn.AddComponent<Image>();
            cbImg.color = new Color(0.85f, 0.65f, 0.2f, 0.95f);
            var cbButton = collectBtn.AddComponent<Button>();
            cbButton.targetGraphic = cbImg;
            collectBtn.AddComponent<ButtonHoverEffect>();
            var cbLabel = CreateTextObject("CollectButtonLabel", collectBtn.transform,
                "收集照片", 30, Color.white, Vector2.zero);
            var cblRect = cbLabel.GetComponent<RectTransform>();
            cblRect.anchorMin = Vector2.zero;
            cblRect.anchorMax = Vector2.one;
            cblRect.offsetMin = cblRect.offsetMax = Vector2.zero;

            // 已收集文字
            var collected = CreateTextObject("CollectedText", rootTransform,
                "照片已收集", 36, new Color(0.7f, 0.8f, 0.5f), new Vector2(0, -100));
            collected.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 100);
        }

        // ==============================
        // 子元素创建
        // ==============================

        private static void CreateBaguaItemSlot(string name, Transform parent, Sprite sprite, Vector2 position, string itemId)
        {
            var go = new GameObject(name, typeof(Image), typeof(DraggableItem));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220, 220);
            var img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.6f, 0.48f, 0.32f);
            }
            img.raycastTarget = true;
        }

        private static void CreateCharacterCard(string name, Transform parent, BaguaStoryEntry entry,
            Vector2 position, BaguaStoryConfig config)
        {
            var cardWidth = 520;
            var cardHeight = 292;
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var cardRect = go.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = position;
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
            var cardImage = go.GetComponent<Image>();
            cardImage.color = new Color(0.38f, 0.31f, 0.23f, 0.95f);
            cardImage.raycastTarget = true;

            // 立绘
            var portrait = CreateUIObject("PortraitImage", go.transform);
            var pRect = portrait.GetComponent<RectTransform>();
            pRect.anchorMin = pRect.anchorMax = new Vector2(0, 0.5f);
            pRect.anchoredPosition = new Vector2(80, 0);
            pRect.sizeDelta = new Vector2(130, 160);
            var pImg = portrait.AddComponent<Image>();
            pImg.sprite = entry.portrait;
            pImg.color = entry.portrait != null ? Color.white : new Color(0.55f, 0.45f, 0.35f);
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;

            // 姓名
            var nameText = CreateTextObject("NameText", go.transform, entry.displayName, 28,
                Color.white, new Vector2(30, 45));
            nameText.GetComponent<RectTransform>().anchorMin = nameText.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 40);

            // 年龄·排行
            var ageTitle = (entry.age ?? "") + " · " + (entry.title ?? "");
            var ageText = CreateTextObject("AgeTitleText", go.transform, ageTitle, 20,
                new Color(0.8f, 0.75f, 0.6f), new Vector2(30, 10));
            ageText.GetComponent<RectTransform>().anchorMin = ageText.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            ageText.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);

            // 声音按钮
            var audioBtn = CreateUIObject("AudioButtonRoot", go.transform);
            var abRect = audioBtn.GetComponent<RectTransform>();
            abRect.anchorMin = abRect.anchorMax = new Vector2(0, 0.5f);
            abRect.anchoredPosition = new Vector2(30, -30);
            abRect.sizeDelta = new Vector2(60, 60);
            var abImg = audioBtn.AddComponent<Image>();
            abImg.sprite = config.ListenButtonSprite;
            abImg.color = config.ListenButtonSprite != null ? Color.white : new Color(0.8f, 0.2f, 0.15f);
            abImg.preserveAspect = true;
            var abButton = audioBtn.AddComponent<Button>();
            abButton.targetGraphic = abImg;
            var abLabel = CreateTextObject("AudioButtonLabel", audioBtn.transform, "听", 24,
                Color.white, Vector2.zero);
            abLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 30);

            // 虚线放置槽
            var dropSlot = CreateUIObject("DropSlot", go.transform);
            var dsRect = dropSlot.GetComponent<RectTransform>();
            dsRect.anchorMin = dsRect.anchorMax = new Vector2(1, 0.5f);
            dsRect.anchoredPosition = new Vector2(-100, 0);
            dsRect.sizeDelta = new Vector2(120, 120);
            var dsImg = dropSlot.AddComponent<Image>();
            dsImg.color = new Color(1f, 1f, 1f, 0.06f);
            dsImg.raycastTarget = false;

            // 已填入物品
            var filled = CreateUIObject("FilledItemRoot", go.transform);
            var fRect = filled.GetComponent<RectTransform>();
            fRect.anchorMin = fRect.anchorMax = new Vector2(1, 0.5f);
            fRect.anchoredPosition = new Vector2(-100, 0);
            fRect.sizeDelta = new Vector2(120, 120);
            var fImg = filled.AddComponent<Image>();
            fImg.color = new Color(0.7f, 0.58f, 0.4f);
            fImg.raycastTarget = false;
            filled.SetActive(false);
        }

        private static void CreatePhotoZone(string name, Transform parent, BaguaStoryConfig config,
            BaguaStoryConfig.PhotoZoneConfig zone)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = zone.anchoredPosition + new Vector2(0, 60);
            rect.sizeDelta = zone.size * 1.5f;
            var img = go.GetComponent<Image>();
            img.sprite = config.PhotoZoneSprite;
            img.color = config.PhotoZoneSprite != null ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.08f);
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.AddComponent<PhotoNameDropZone>();
        }

        private static void CreateNameTagSlot(string name, Transform parent, BaguaStoryEntry entry, Vector2 position)
        {
            var go = new GameObject(name, typeof(Image), typeof(DraggableItem));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 80);
            var img = go.GetComponent<Image>();
            img.sprite = entry.nameTagSprite;
            img.color = entry.nameTagSprite != null ? Color.white : new Color(0.7f, 0.58f, 0.4f);
            img.preserveAspect = true;
            img.raycastTarget = true;

            var label = CreateTextObject("LabelText", go.transform, entry.displayName, 26,
                Color.white, Vector2.zero);
            var lRect = label.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = lRect.offsetMax = Vector2.zero;
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

        private static BaguaStoryConfig LoadFirstBaguaConfig()
        {
            var guids = AssetDatabase.FindAssets("t:BaguaStoryConfig");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BaguaStoryConfig>(path);
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

        private static void AutoWireReferences(GameObject root, BaguaView view, BaguaStoryConfig config)
        {
            var so = new SerializedObject(view);
            var rt = root.transform;

            // 共享
            so.FindProperty("_background").objectReferenceValue = FindComponent<Image>(rt, "Background");
            so.FindProperty("_waitingText").objectReferenceValue = FindComponent<Text>(rt, "WaitingText");

            // 字幕条
            so.FindProperty("_subtitleBarRoot").objectReferenceValue = FindGameObject(rt, "SubtitleBarRoot");
            so.FindProperty("_subtitleText").objectReferenceValue = FindComponent<Text>(rt, "SubtitleText");

            // 母亲端
            var clientPanel = rt.Find("ClientPanel");
            so.FindProperty("_clientPanel").objectReferenceValue = clientPanel?.gameObject;
            so.FindProperty("_taskBannerText").objectReferenceValue = FindComponent<Text>(clientPanel, "TaskBannerText");
            so.FindProperty("_desktopTrayImage").objectReferenceValue = FindComponent<Image>(clientPanel, "DesktopTrayImage");
            so.FindProperty("_clientWaitingText").objectReferenceValue = FindComponent<Text>(clientPanel, "ClientWaitingText");

            // 桌面物件槽数组
            WireBaguaItemSlots(so, "_desktopItemSlots", clientPanel, "DesktopItemSlot_", config.ItemPlacements != null ? config.ItemPlacements.Length : 0);

            // 人物卡数组
            var entries = config.Entries;
            WireCharacterCards(so, "_characterCards", clientPanel, "CharacterCard_", entries != null ? entries.Length : 0);

            // 女儿端
            var hostPanel = rt.Find("HostPanel");
            so.FindProperty("_hostPanel").objectReferenceValue = hostPanel?.gameObject;
            so.FindProperty("_hostRoleText").objectReferenceValue = FindComponent<Text>(hostPanel, "HostRoleText");
            so.FindProperty("_hostWaitingText").objectReferenceValue = FindComponent<Text>(hostPanel, "HostWaitingText");
            so.FindProperty("_photoBackgroundImage").objectReferenceValue = FindComponent<Image>(hostPanel, "PhotoBackgroundImage");
            so.FindProperty("_photoInstructionText").objectReferenceValue = FindComponent<Text>(hostPanel, "PhotoInstructionText");

            // 照片投放区数组
            var zones = config.PhotoZones;
            WirePhotoZones(so, "_photoZones", hostPanel, "PhotoZone_", zones != null ? zones.Length : 0);

            // 姓名标签数组
            WireNameTagSlots(so, "_nameTagSlots", hostPanel, "NameTagSlot_", entries != null ? entries.Length : 0);

            // 完成视图
            so.FindProperty("_completeText").objectReferenceValue = FindComponent<Text>(rt, "CompleteText");
            so.FindProperty("_rewardPhotoImage").objectReferenceValue = FindComponent<Image>(rt, "RewardPhotoImage");
            so.FindProperty("_photoLabelText").objectReferenceValue = FindComponent<Text>(rt, "PhotoLabelText");
            so.FindProperty("_collectButtonRoot").objectReferenceValue = FindGameObject(rt, "CollectButtonRoot");
            so.FindProperty("_collectButton").objectReferenceValue = FindComponent<Button>(rt, "CollectButtonRoot");
            so.FindProperty("_collectButtonLabel").objectReferenceValue = FindComponent<Text>(rt, "CollectButtonLabel");
            so.FindProperty("_collectedText").objectReferenceValue = FindComponent<Text>(rt, "CollectedText");

            so.ApplyModifiedProperties();
            Debug.Log("[BaguaPrefabExporter] BaguaView 引用已自动绑定");
        }

        private static void WireBaguaItemSlots(SerializedObject so, string fieldName,
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

        private static void WireCharacterCards(SerializedObject so, string fieldName,
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
                element.FindPropertyRelative("cardImage").objectReferenceValue = child.GetComponent<Image>();
                element.FindPropertyRelative("portraitImage").objectReferenceValue = FindComponent<Image>(child, "PortraitImage");
                element.FindPropertyRelative("nameText").objectReferenceValue = FindComponent<Text>(child, "NameText");
                element.FindPropertyRelative("ageTitleText").objectReferenceValue = FindComponent<Text>(child, "AgeTitleText");
                element.FindPropertyRelative("audioButtonRoot").objectReferenceValue = FindGameObject(child, "AudioButtonRoot");
                element.FindPropertyRelative("audioButton").objectReferenceValue = FindComponent<Button>(child, "AudioButtonRoot");
                element.FindPropertyRelative("audioButtonImage").objectReferenceValue = FindComponent<Image>(child, "AudioButtonRoot");
                element.FindPropertyRelative("dropSlotRect").objectReferenceValue = FindComponent<RectTransform>(child, "DropSlot");
                element.FindPropertyRelative("filledItemRoot").objectReferenceValue = FindGameObject(child, "FilledItemRoot");
                element.FindPropertyRelative("filledItemImage").objectReferenceValue = FindComponent<Image>(child, "FilledItemRoot");
                element.FindPropertyRelative("filledItemNameText").objectReferenceValue = FindComponent<Text>(child, "FilledItemRoot");
            }
        }

        private static void WirePhotoZones(SerializedObject so, string fieldName,
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
                element.FindPropertyRelative("dropZone").objectReferenceValue = child.GetComponent<PhotoNameDropZone>();
            }
        }

        private static void WireNameTagSlots(SerializedObject so, string fieldName,
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
