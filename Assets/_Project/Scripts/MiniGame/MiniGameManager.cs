using System;
using System.Collections;
using DoNotForgetMe.MiniGame.Cooking;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>把 Host 权威快照转换为本端的小游戏私有 UI。</summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [SerializeField] private RecipeConfig[] recipes;

    public bool IsActive => _activeMiniGame != null;
    public event Action<string> OnMiniGameStart;
    public event Action<string, bool> OnMiniGameComplete;

    private Canvas _canvas;
    private GameObject _panel;
    private CookingMiniGame _activeMiniGame;
    private SessionGameplayCoordinator _coordinator;
    private Coroutine _finishRoutine;
    private GameObject _interruptedOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    private void OnStateChanged(CookingGameState state)
    {
        if (state != null && state.phase == GameplayPhase.MiniGameInterrupted)
        {
            DestroyActiveView();
            ShowInterruptedOverlay();
            return;
        }

        if (_interruptedOverlay != null)
        {
            Destroy(_interruptedOverlay);
            _interruptedOverlay = null;
        }

        var shouldShow = state != null && state.phase == GameplayPhase.MiniGame && !string.IsNullOrEmpty(state.recipeId);
        if (!shouldShow)
        {
            CloseMiniGame(state != null && state.completed);
            return;
        }

        var recipe = FindRecipe(state.recipeId);
        if (recipe == null)
        {
            Debug.LogError("[MiniGame] 未找到菜谱：" + state.recipeId);
            return;
        }

        if (_activeMiniGame == null)
        {
            _canvas.gameObject.SetActive(true);
            var go = new GameObject("CookingMiniGame");
            go.transform.SetParent(_panel.transform, false);
            _activeMiniGame = go.AddComponent<CookingMiniGame>();
            _activeMiniGame.Initialize(null, _panel.GetComponent<RectTransform>());
            _activeMiniGame.Setup(recipe, _coordinator);
            _activeMiniGame.StartGame();
            OnMiniGameStart?.Invoke(state.recipeId);
        }

        _activeMiniGame.Render(state);
        if (state.completed && _finishRoutine == null)
        {
            _finishRoutine = StartCoroutine(FinishAfterDisplay(state.recipeId));
        }
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
        Destroy(_activeMiniGame.gameObject);
        _activeMiniGame = null;
    }

    private void ShowInterruptedOverlay()
    {
        if (_interruptedOverlay != null) return;

        _canvas.gameObject.SetActive(true);
        _interruptedOverlay = new GameObject("InterruptedOverlay", typeof(Image));
        _interruptedOverlay.transform.SetParent(_panel.transform, false);
        var rect = _interruptedOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _interruptedOverlay.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.98f);
        CreateOverlayText("小游戏已中断", new Vector2(0, 100), 44);

        if (_coordinator.IsHostAuthority)
        {
            CreateOverlayButton("继续", new Vector2(-180, -80), GameplayIntentType.ResumeMiniGame);
            CreateOverlayButton("重新开始", new Vector2(180, -80), GameplayIntentType.RestartMiniGame);
        }
        else
        {
            CreateOverlayText("等待女儿选择继续或重新开始。", new Vector2(0, -80), 28);
        }
    }

    private void CreateOverlayText(string content, Vector2 position, int fontSize)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(_interruptedOverlay.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1000, 100);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = content;
    }

    private void CreateOverlayButton(string content, Vector2 position, GameplayIntentType intentType)
    {
        var go = new GameObject(content, typeof(Image), typeof(Button));
        go.transform.SetParent(_interruptedOverlay.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300, 110);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.42f, 0.32f, 0.2f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => _coordinator.Request(new GameplayIntent(intentType)));
        CreateOverlayTextOnButton(go.transform, content);
    }

    private static void CreateOverlayTextOnButton(Transform parent, string content)
    {
        var go = new GameObject("Label", typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;
        text.raycastTarget = false;
    }

    private RecipeConfig FindRecipe(string recipeId)
    {
        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.RecipeId == recipeId) return recipe;
        }
        return null;
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
        canvasObj.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObj.transform, false);
        var background = _panel.AddComponent<Image>();
        background.color = new Color(0.12f, 0.1f, 0.08f, 0.98f);
        var rect = _panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        canvasObj.SetActive(false);
    }
}
