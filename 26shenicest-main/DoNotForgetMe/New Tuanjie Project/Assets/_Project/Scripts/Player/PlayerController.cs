using UnityEngine;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private MovementController movementController;
    [SerializeField] private InteractionController interactionController;
    [SerializeField] private HealthController healthController;
    [SerializeField] private PlayerInputHandler cachedInputHandler;

    [Header("数据")]
    [SerializeField] private InputSettings inputSettings;
    [SerializeField] private PlayerSettings playerSettings;

    private PlayerStateMachine stateMachine;
    private Rigidbody2D body;
    private float baseGravityScale = 1f;
    private GameObject _interactHint;

    private IPlayerState idleState;
    private IPlayerState runState;
    private IPlayerState interactState;
    private IPlayerState deadState;

    private float movementInput;
    private bool interactRequested;

    public float HorizontalInput => movementInput;

    public IPlayerState IdleState => idleState;
    public IPlayerState RunState => runState;
    public IPlayerState InteractState => interactState;
    public IPlayerState DeadState => deadState;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        baseGravityScale = body != null ? body.gravityScale : 1f;

        if (movementController == null)
            movementController = GetComponent<MovementController>();
        if (interactionController == null)
            interactionController = GetComponent<InteractionController>();
        if (healthController == null)
            healthController = GetComponent<HealthController>();
        if (cachedInputHandler == null)
            cachedInputHandler = GetComponent<PlayerInputHandler>();

        if (healthController != null)
        {
            healthController.OnDie += OnDie_Handler;
        }

        ApplySettings();

        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this);
        runState = new PlayerRunState(this);
        interactState = new PlayerInteractState(this);
        deadState = new PlayerDeadState(this);

        Debug.Log($"[PlayerController] Interact: {GetInteractKey()}");

        CreateInteractHint();
    }

    void Start()
    {
        stateMachine.Initialize(idleState);
    }

    void Update()
    {
        if (!CanControlExploration())
        {
            SetMovementInput(0f);
            Move(0f);
            UpdateInteractHint(null);
            return;
        }

        CaptureFallbackInput();
        UpdateInteractHint(FindNearbyInteractable());

        if (stateMachine?.CurrentState == null)
            return;

        stateMachine.CurrentState.HandleInput();
        stateMachine.CurrentState.LogicUpdate();
    }

    private static bool CanControlExploration()
    {
        var coordinator = SessionGameplayCoordinator.Instance;
        if (coordinator != null && coordinator.State.phase != GameplayPhase.Exploration)
        {
            return false;
        }

        // 探索阶段仅女儿（Host）拥有输入权；Client 始终观战。
        return NetworkSessionManager.Service.Role == SessionRole.Host;
    }

    private void ApplySettings()
    {
        if (playerSettings != null)
        {
            if (movementController != null)
                movementController.ApplySettings(playerSettings.MoveSpeed);
            if (interactionController != null)
                interactionController.ApplyPlayerSettings(playerSettings);
            if (healthController != null)
                healthController.ApplyPlayerSettings(playerSettings);
        }

        if (interactionController != null)
            interactionController.ApplyInputSettings(inputSettings);

        if (cachedInputHandler != null)
        {
            cachedInputHandler.ApplyInputSettings(inputSettings);
            cachedInputHandler.interactKey = GetInteractKey();
        }
    }

    private void OnDie_Handler()
    {
        ChangeState(deadState);
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        if (body != null)
        {
            body.velocity = Vector2.zero;
        }
    }

    public void Revive()
    {
        SetGravityScale(baseGravityScale);
        ChangeState(idleState);
        Debug.Log("Player Revived!");
    }

    private void CaptureFallbackInput()
    {
        if (cachedInputHandler != null && cachedInputHandler.isActiveAndEnabled)
        {
            return;
        }

        SetMovementInput(Input.GetAxisRaw("Horizontal"));

        if (Input.GetKeyDown(GetInteractKey()))
        {
            QueueInteractInput();
        }
    }

    public void SetMovementInput(float value)
    {
        movementInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void QueueInteractInput()
    {
        interactRequested = true;
    }

    public bool ConsumeInteractInput()
    {
        if (!interactRequested)
            return false;
        interactRequested = false;
        return true;
    }

    public void Move(float normalizedInput)
    {
        movementController?.Move(normalizedInput);
    }

    public bool PerformInteraction()
    {
        return interactionController != null && interactionController.TryInteract();
    }

    public void ChangeState(IPlayerState newState)
    {
        stateMachine?.ChangeState(newState);
    }

    public void SetGravityScale(float scale)
    {
        if (body != null)
            body.gravityScale = scale;
    }

    private KeyCode GetInteractKey()
    {
        return inputSettings != null ? inputSettings.InteractKey : KeyCode.F;
    }

    // ==============================
    // 交互提示图标
    // ==============================

    private void CreateInteractHint()
    {
        _interactHint = new GameObject("InteractHint");
        _interactHint.transform.SetParent(transform, false);
        _interactHint.SetActive(false);

        var sr = _interactHint.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // 用 Resources.Load 加载放大镜图标，无图时用 "F" 文字提示
        var hintSprite = Resources.Load<Sprite>("interact_hint");
        if (hintSprite != null)
        {
            sr.sprite = hintSprite;
            sr.color = Color.white;
        }
        else
        {
            // 无图标时用纯色圆点提示
            sr.sprite = CreateHintSprite();
            sr.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        }
        sr.flipX = false;

        _interactHint.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
        _interactHint.transform.localRotation = Quaternion.identity;
    }

    private static Sprite CreateHintSprite()
    {
        var size = 32;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        var center = new Vector2(13, 19);
        float ringRadius = 8f;
        float ringWidth = 2.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center);
                // 圆环部分
                bool inRing = dist >= ringRadius - ringWidth && dist <= ringRadius + ringWidth;
                // 手柄部分（从圆环右下延伸到角落）
                bool onHandle = false;
                if (x >= 17 && y <= 15)
                {
                    var handleDist = Mathf.Abs((x - 13) - (19 - y)) / 1.4142f;
                    onHandle = handleDist <= 1.5f && (x + (size - y)) >= 22 && (x + (size - y)) <= 38;
                }

                pixels[y * size + x] = (inRing || onHandle) ? Color.white : new Color(0, 0, 0, 0);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.4f, 0.6f), 32f);
    }

    private Collider2D FindNearbyInteractable()
    {
        if (interactionController == null) return null;
        return Physics2D.OverlapCircle(transform.position, interactionController.interactRange, interactionController.interactLayer);
    }

    private void UpdateInteractHint(Collider2D nearby)
    {
        if (_interactHint == null) return;

        if (nearby != null)
        {
            _interactHint.SetActive(true);
            // 图标显示在交互物上方
            _interactHint.transform.position = nearby.transform.position + new Vector3(0, 1.2f, 0);
        }
        else
        {
            _interactHint.SetActive(false);
        }
    }
}
