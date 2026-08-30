using System;
using System.Collections;
using DoNotForgetMe.MiniGame.Album;
using DoNotForgetMe.MiniGame.Bagua;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>把 Host 权威快照转换为本端的小游戏私有 UI。</summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [SerializeField] private RecipeConfig[] recipes;
    [SerializeField] private BaguaStoryConfig[] baguaConfigs;
    [SerializeField] private AlbumConfig[] albumConfigs;

    [Header("片尾视频")]
    [Tooltip("片尾视频（留空则跳过）")]
    [SerializeField] private VideoClip outroVideoClip;

    public bool IsActive => _activeMiniGame != null;
    public event Action<string> OnMiniGameStart;
    public event Action<string, bool> OnMiniGameComplete;

    private Canvas _canvas;
    private GameObject _panel;
    private MiniGameBase _activeMiniGame;
    private string _activeMiniGameId;
    private SessionGameplayCoordinator _coordinator;
    private Coroutine _finishRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
    }

    private void Start()
    {
        _coordinator = SessionGameplayCoordinator.Instance;
        if (_coordinator == null)
        {
            Debug.LogWarning("[MiniGame] 缺少 SessionGameplayCoordinator");
            return;
        }
        _coordinator.StateChanged += OnStateChanged;
        OnStateChanged(_coordinator.State);
    }

    private void OnDestroy()
    {
        if (_coordinator != null) _coordinator.StateChanged -= OnStateChanged;
    }

    public void StartMiniGame(string gameId, MiniGameSettings settings = null)
    {
        _coordinator?.Request(new GameplayIntent(GameplayIntentType.StartMiniGame, gameId));
    }

    public void StartBaguaMiniGame(string configId)
    {
        _coordinator?.Request(new GameplayIntent(GameplayIntentType.StartBaguaMiniGame, configId));
    }

    private void OnStateChanged(GameplaySnapshot snapshot)
    {
        if (snapshot == null) return;

        // GameEnded：显示黑屏覆盖，保持 canvas 激活
        if (snapshot.phase == GameplayPhase.GameEnded)
        {
            DestroyActiveView();
            ShowGameEndedOverlay();
            return;
        }

        // 对白阶段不渲染小游戏 UI
        if (snapshot.phase == GameplayPhase.Dialogue)
        {
            if (_activeMiniGame != null) CloseMiniGame(false);
            return;
        }

        var shouldShow = snapshot.phase == GameplayPhase.MiniGame && !string.IsNullOrEmpty(snapshot.miniGameId);
        if (!shouldShow)
        {
            CloseMiniGame(snapshot.cooking != null && snapshot.cooking.completed ||
                         snapshot.bagua != null && snapshot.bagua.completed);
            return;
        }

        // 确定小游戏类型
        var miniGameId = snapshot.miniGameId;
        var isBagua = FindBaguaConfig(miniGameId) != null;
        var isAlbum = FindAlbumConfig(miniGameId) != null;

        if (_activeMiniGame == null || _activeMiniGameId != miniGameId)
        {
            DestroyActiveView();
            _activeMiniGameId = miniGameId;

            _canvas.gameObject.SetActive(true);
            var go = new GameObject(isAlbum ? "AlbumMiniGame" : (isBagua ? "BaguaMiniGame" : "CookingMiniGame"));
            go.transform.SetParent(_panel.transform, false);

            if (isAlbum)
            {
                var albumConfig = FindAlbumConfig(miniGameId);
                var view = go.AddComponent<AlbumMiniGameView>();
                view.Initialize(null, _panel.GetComponent<RectTransform>());
                view.Setup(albumConfig, _coordinator);
                view.StartGame();
                _activeMiniGame = view;
            }
            else if (isBagua)
            {
                var baguaConfig = FindBaguaConfig(miniGameId);
                var view = go.AddComponent<BaguaMiniGameView>();
                view.Initialize(null, _panel.GetComponent<RectTransform>());
                view.Setup(baguaConfig, _coordinator);
                view.StartGame();
                _activeMiniGame = view;
            }
            else
            {
                var recipe = FindRecipe(miniGameId);
                if (recipe == null)
                {
                    Debug.LogError("[MiniGame] 未找到菜谱或配置：" + miniGameId);
                    CloseMiniGame(false);
                    return;
                }
                var view = go.AddComponent<CookingMiniGame>();
                view.Initialize(null, _panel.GetComponent<RectTransform>());
                view.Setup(recipe, _coordinator);
                view.StartGame();
                _activeMiniGame = view;
            }

            OnMiniGameStart?.Invoke(miniGameId);
        }

        // 渲染对应视图
        if (isAlbum)
        {
            ((AlbumMiniGameView)_activeMiniGame).Render(snapshot.album);
        }
        else if (isBagua)
        {
            ((BaguaMiniGameView)_activeMiniGame).Render(snapshot.bagua);
        }
        else
        {
            ((CookingMiniGame)_activeMiniGame).Render(snapshot.cooking);
        }

        // 完成后不自动 Finish——等玩家在小游戏界面内收集照片后再 Finish
        if (isAlbum) return;
    }

    private IEnumerator FinishAfterDisplay(string gameId)
    {
        yield return new WaitForSeconds(1.4f);
        _finishRoutine = null;
        _coordinator?.Request(new GameplayIntent(GameplayIntentType.FinishMiniGame));
        OnMiniGameComplete?.Invoke(gameId, true);
    }

    private void CloseMiniGame(bool success)
    {
        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }
        DestroyActiveView();
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    private void DestroyActiveView()
    {
        if (_activeMiniGame == null) return;
        _activeMiniGame.EndGame();
        Destroy(_activeMiniGame.gameObject);
        _activeMiniGame = null;
        _activeMiniGameId = null;
    }

    private RecipeConfig FindRecipe(string recipeId)
    {
        if (recipes == null) return null;
        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.RecipeId == recipeId) return recipe;
        }
        return null;
    }

    private BaguaStoryConfig FindBaguaConfig(string configId)
    {
        if (baguaConfigs == null) return null;
        foreach (var config in baguaConfigs)
        {
            if (config != null && config.MiniGameId == configId) return config;
        }
        return null;
    }

    private AlbumConfig FindAlbumConfig(string configId)
    {
        if (albumConfigs == null) return null;
        foreach (var config in albumConfigs)
        {
            if (config != null && config.MiniGameId == configId) return config;
        }
        return null;
    }

    private void ShowGameEndedOverlay()
    {
        _canvas.gameObject.SetActive(true);
        var go = new GameObject("GameEndedOverlay", typeof(Image));
        go.transform.SetParent(_panel.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = Color.black;

        // 结尾序列控制器
        var endingGo = new GameObject("EndingController");
        var ending = endingGo.AddComponent<DoNotForgetMe.Cutscene.EndingController>();

        // 传递片尾视频
#if UNITY_EDITOR
        if (outroVideoClip != null)
        {
            var endingSo = new UnityEditor.SerializedObject(ending);
            endingSo.FindProperty("outroVideoClip").objectReferenceValue = outroVideoClip;
            endingSo.ApplyModifiedProperties();
        }
        else
        {
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                "Assets/_Project/Video/片尾.mp4");
            if (clip != null)
            {
                var endingSo = new UnityEditor.SerializedObject(ending);
                endingSo.FindProperty("outroVideoClip").objectReferenceValue = clip;
                endingSo.ApplyModifiedProperties();
            }
        }
#else
        if (outroVideoClip != null)
        {
            ending.SetOutroVideo(outroVideoClip);
        }
#endif
    }

    private void CreateCanvas()
    {
        var canvasObj = new GameObject("MiniGameCanvas");
        canvasObj.transform.SetParent(transform, false);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<PassthroughGraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObj.transform, false);
        var background = _panel.AddComponent<Image>();
        background.color = new Color(0.12f, 0.1f, 0.08f, 0.9f);
        background.raycastTarget = false;
        var rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        canvasObj.SetActive(false);
    }
}
