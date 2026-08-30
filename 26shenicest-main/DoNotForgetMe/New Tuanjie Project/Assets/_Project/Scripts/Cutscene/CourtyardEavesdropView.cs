using UnityEngine;
using UnityEngine.UI;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.Network.Local;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Core;
using DoNotForgetMe.UI;
using DoNotForgetMe.MiniGame;
using DoNotForgetMe.MiniGame.Bagua;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Cooking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DoNotForgetMe.Cutscene
{
    /// <summary>
    /// Courtyard 子页面控制器。
    /// 不显示角色、不显示物品栏，只有全屏背景 + "进入偷听"按钮。
    /// 八卦完成后 → 照片收集 → 自动转场回 LivingRoom（相册小游戏）。
    /// </summary>
    public class CourtyardEavesdropView : MonoBehaviour
    {
        [SerializeField] private Button _enterButton;
        [SerializeField] private Button _exitButton;

        private bool _baguaCompleted;

        private void Awake()
        {
            // 未通过 Prefab 绑定时（旧场景），运行时构建 UI
            if (_enterButton == null)
                CreateUICodeFallback();

            WireButtons();
        }

        /// <summary>绑定按钮点击事件。</summary>
        private void WireButtons()
        {
            if (_enterButton != null)
            {
                _enterButton.onClick.AddListener(() =>
                {
                    var coord = SessionGameplayCoordinator.Instance;
                    if (coord == null) return;
                    if (coord.State.phase == GameplayPhase.MiniGame) return;
                    AudioManager.Play(SfxId.UiButtonClick);
                    coord.Request(new GameplayIntent(GameplayIntentType.StartBaguaMiniGame, "bagua_old_photo"));
                });
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(() =>
                {
                    AudioManager.Play(SfxId.UiButtonClick);
                    SceneLoader.Load(SceneNames.Kitchen);
                });
            }
        }

        // ==============================
        // 代码构建 UI（旧场景兼容，Prefab 导出后可移除）
        // ==============================

        private void CreateUICodeFallback()
        {
            var canvasGo = new GameObject("CourtyardEavesdropCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _enterButton = CreateButton(canvasGo.transform, "EnterEavesdropBtn", "进入偷听",
                new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Color(0.5f, 0.35f, 0.15f, 0.95f));

            _exitButton = CreateButton(canvasGo.transform, "ExitEavesdropBtn", "结束偷听",
                new Vector2(0.5f, 0f), new Vector2(0, 80), new Color(0.5f, 0.35f, 0.15f, 0.95f));
        }

        private Button CreateButton(Transform parent, string objName, string label,
            Vector2 anchor, Vector2 position, Color color)
        {
            var go = new GameObject(objName, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360, 100);
            var img = go.GetComponent<Image>();
            img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 38;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.raycastTarget = false;

            go.AddComponent<ButtonHoverEffect>();
            return btn;
        }

        private void Start()
        {
            var coord = SessionGameplayCoordinator.Instance;
            if (coord == null)
            {
                coord = FindObjectOfType<SessionGameplayCoordinator>();
            }
            if (coord == null)
            {
                Debug.LogWarning("[CourtyardEavesdrop] 未找到 Coordinator，自动创建调试管理器…");
                EnsureDebugManagers();
                coord = SessionGameplayCoordinator.Instance;
            }
            if (coord != null)
            {
                coord.StateChanged += OnStateChanged;
                OnStateChanged(coord.State);
            }
            else
            {
                Debug.LogError("[CourtyardEavesdrop] 仍无法创建 Coordinator，请从 LivingRoom 进入。");
                if (_enterButton != null) _enterButton.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            var coord = SessionGameplayCoordinator.Instance;
            if (coord != null) coord.StateChanged -= OnStateChanged;
        }

        /// <summary>直接打开 Courtyard 场景时，自动创建调试用管理器。</summary>
        private void EnsureDebugManagers()
        {
#if UNITY_EDITOR
            // LocalNetworkBootstrap
            if (NetworkSessionManager.Service == null)
            {
                NetworkSessionManager.Register(new LocalDebugService());
            }

            // BaguaStoryConfig
            var baguaConfig = UnityEngine.Resources.Load<BaguaStoryConfig>("BaguaStoryConfig");
            if (baguaConfig == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:BaguaStoryConfig");
                if (guids.Length > 0)
                    baguaConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<BaguaStoryConfig>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // AlbumConfig
            var albumConfig = UnityEngine.Resources.Load<AlbumConfig>("AlbumConfig");
            if (albumConfig == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:AlbumConfig");
                if (guids.Length > 0)
                    albumConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<AlbumConfig>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // RecipeConfig
            RecipeConfig tomatoEgg = null, cucumberSalad = null;
            var recipeGuids = UnityEditor.AssetDatabase.FindAssets("t:RecipeConfig");
            if (recipeGuids.Length > 0)
                tomatoEgg = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeConfig>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(recipeGuids[0]));
            if (recipeGuids.Length > 1)
                cucumberSalad = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeConfig>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(recipeGuids[1]));

            // Coordinator
            var coordObj = new GameObject("SessionGameplayCoordinator");
            var coord = coordObj.AddComponent<SessionGameplayCoordinator>();
            var so = new UnityEditor.SerializedObject(coord);
            if (tomatoEgg != null && cucumberSalad != null)
            {
                so.FindProperty("recipes").arraySize = 2;
                so.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = tomatoEgg;
                so.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = cucumberSalad;
            }
            if (baguaConfig != null)
            {
                so.FindProperty("baguaConfigs").arraySize = 1;
                so.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
            }
            if (albumConfig != null)
            {
                so.FindProperty("albumConfigs").arraySize = 1;
                so.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;
            }
            so.FindProperty("debugSingleProcess").boolValue = true;
            so.ApplyModifiedProperties();

#if !FUSION_PRESENT
            // LocalGameplayBridge
            coordObj.AddComponent<LocalGameplayBridge>();
#else
            coordObj.AddComponent<Fusion.NetworkObject>();
            coordObj.AddComponent<DoNotForgetMe.Network.Fusion.FusionGameplayBridge>();
#endif

            // MiniGameManager
            var mgmObj = new GameObject("MiniGameManager");
            var mgm = mgmObj.AddComponent<MiniGameManager>();
            var mgmSo = new UnityEditor.SerializedObject(mgm);
            if (tomatoEgg != null && cucumberSalad != null)
            {
                mgmSo.FindProperty("recipes").arraySize = 2;
                mgmSo.FindProperty("recipes").GetArrayElementAtIndex(0).objectReferenceValue = tomatoEgg;
                mgmSo.FindProperty("recipes").GetArrayElementAtIndex(1).objectReferenceValue = cucumberSalad;
            }
            if (baguaConfig != null)
            {
                mgmSo.FindProperty("baguaConfigs").arraySize = 1;
                mgmSo.FindProperty("baguaConfigs").GetArrayElementAtIndex(0).objectReferenceValue = baguaConfig;
            }
            if (albumConfig != null)
            {
                mgmSo.FindProperty("albumConfigs").arraySize = 1;
                mgmSo.FindProperty("albumConfigs").GetArrayElementAtIndex(0).objectReferenceValue = albumConfig;
            }
            mgmSo.ApplyModifiedProperties();

            Debug.Log("[CourtyardEavesdrop] 调试管理器已创建，可正常测试八卦小游戏。");
#else
            Debug.LogWarning("[CourtyardEavesdrop] 正式版需要从 LivingRoom 进入。");
#endif
        }

        private void OnStateChanged(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;

            // 小游戏进行中或对白播放中 → 隐藏按钮
            if (snapshot.phase == GameplayPhase.MiniGame ||
                snapshot.phase == GameplayPhase.Dialogue)
            {
                if (_enterButton != null) _enterButton.gameObject.SetActive(false);
                if (_exitButton != null) _exitButton.gameObject.SetActive(false);
            }
            else if (snapshot.phase == GameplayPhase.Exploration)
            {
                // 检查八卦照片是否已收集 → 标记八卦完成
                if (snapshot.collectedPhotoIds != null && snapshot.collectedPhotoIds.Contains("bagua_old_family_photo"))
                {
                    _baguaCompleted = true;
                }
                // 八卦已完成 → 隐藏"进入偷听"，显示"结束偷听"
                if (_enterButton != null) _enterButton.gameObject.SetActive(!_baguaCompleted);
                if (_exitButton != null) _exitButton.gameObject.SetActive(_baguaCompleted);
            }
        }

    }
}
