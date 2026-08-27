using UnityEngine;

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
    }

    void Start()
    {
        stateMachine.Initialize(idleState);
    }

    void Update()
    {
        CaptureFallbackInput();

        if (stateMachine?.CurrentState == null)
            return;

        stateMachine.CurrentState.HandleInput();
        stateMachine.CurrentState.LogicUpdate();
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
}
