using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    public bool IsActive => _activeMiniGame != null;

    public event Action<string> OnMiniGameStart;
    public event Action<string, bool> OnMiniGameComplete;

    private readonly Dictionary<string, MiniGameBase> _templates = new();
    private MiniGameBase _activeMiniGame;

    private Canvas _canvas;
    private GameObject _panel;
    private Image _panelBg;

    private bool _playerWasEnabled;
    private RigidbodyType2D _savedBodyType;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateCanvas();
        RegisterTemplates();
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

        _panelBg = _panel.AddComponent<Image>();
        _panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        canvasObj.SetActive(false);
    }

    private void RegisterTemplates()
    {
        _templates.Clear();
        foreach (var game in GetComponentsInChildren<MiniGameBase>(true))
        {
            _templates[game.GameId] = game;
            game.gameObject.SetActive(false);
        }
    }

    public void StartMiniGame(string gameId, MiniGameSettings settings = null)
    {
        if (_activeMiniGame != null)
        {
            Debug.LogWarning("[MiniGameManager] 已有小游戏正在进行中");
            return;
        }

        if (!_templates.TryGetValue(gameId, out var template))
        {
            Debug.LogError($"[MiniGameManager] 未找到小游戏模板: {gameId}");
            return;
        }

        FreezePlayer();

        _canvas.gameObject.SetActive(true);

        _activeMiniGame = Instantiate(template, _panel.transform);
        _activeMiniGame.gameObject.SetActive(true);
        _activeMiniGame.Initialize(settings, _panel.GetComponent<RectTransform>());
        _activeMiniGame.StartGame();

        OnMiniGameStart?.Invoke(gameId);
        Debug.Log($"[MiniGameManager] 小游戏开始: {gameId}");
    }

    private void Update()
    {
        if (_activeMiniGame == null) return;

        _activeMiniGame.UpdateGame();

        if (_activeMiniGame.IsComplete)
        {
            var gameId = _activeMiniGame.GameId;
            var success = _activeMiniGame.IsSuccess;

            _activeMiniGame.EndGame();
            Destroy(_activeMiniGame.gameObject);
            _activeMiniGame = null;

            _canvas.gameObject.SetActive(false);
            UnfreezePlayer();

            OnMiniGameComplete?.Invoke(gameId, success);
            Debug.Log($"[MiniGameManager] 小游戏结束: {gameId}, 成功: {success}");
        }
    }

    private void FreezePlayer()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;

        _playerWasEnabled = player.enabled;
        player.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            _savedBodyType = rb.bodyType;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void UnfreezePlayer()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = _savedBodyType;
        }

        player.enabled = _playerWasEnabled;
    }
}
