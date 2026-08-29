using UnityEngine;
using DoNotForgetMe.Network;

public class MiniGameTrigger : MonoBehaviour, IInteractable
{
    [Header("小游戏配置")]
    [SerializeField] private string miniGameId = "SampleGame";
    [SerializeField] private MiniGameSettings settings;

    [Header("前置条件")]
    [Tooltip("需要先完成的菜谱 ID；留空表示无前置")]
    [SerializeField] private string requiresCompletedRecipeId;

    public bool IsCompleted { get; private set; }
    public bool IsLocked => !string.IsNullOrEmpty(requiresCompletedRecipeId) && !_prerequisiteCompleted;

    private bool _prerequisiteCompleted;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TriggerInteract()
    {
        if (NetworkSessionManager.Service.Role != SessionRole.Host)
        {
            return;
        }

        if (IsCompleted || IsLocked)
        {
            return;
        }

        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("[MiniGameTrigger] 场景中未找到 MiniGameManager");
            return;
        }

        MiniGameManager.Instance.StartMiniGame(miniGameId, settings);
    }

    private void OnEnable()
    {
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete += OnMiniGameComplete;
        }
    }

    private void Start()
    {
        // 补偿 OnEnable 时 MiniGameManager 尚未初始化的情况
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete -= OnMiniGameComplete;
            MiniGameManager.Instance.OnMiniGameComplete += OnMiniGameComplete;
        }
        UpdateVisual();
    }

    private void OnDisable()
    {
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete -= OnMiniGameComplete;
        }
    }

    private void OnMiniGameComplete(string gameId, bool success)
    {
        if (success && gameId == miniGameId)
        {
            IsCompleted = true;
        }

        if (success && gameId == requiresCompletedRecipeId)
        {
            _prerequisiteCompleted = true;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null) return;
        if (IsLocked)
        {
            _spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        else if (IsCompleted)
        {
            _spriteRenderer.color = new Color(0.3f, 0.6f, 0.3f, 0.6f);
        }
        else
        {
            // 恢复默认颜色由场景设置决定，这里不做覆盖
        }
    }
}
