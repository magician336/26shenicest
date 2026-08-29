using UnityEngine;
using DoNotForgetMe.Cutscene;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    public string horizontalAxis = "Horizontal";
    [SerializeField] private KeyCode fallbackInteractKey = KeyCode.F;
    [SerializeField] private InputSettings inputSettings;

    private PlayerController playerController;

    public KeyCode interactKey { set { fallbackInteractKey = value; } }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void ApplyInputSettings(InputSettings settings)
    {
        inputSettings = settings;
    }

    private KeyCode GetInteractKey()
    {
        return inputSettings != null ? inputSettings.InteractKey : fallbackInteractKey;
    }

    void Update()
    {
        if (playerController == null || NetworkSessionManager.Service.Role != SessionRole.Host ||
            (MiniGameManager.Instance != null && MiniGameManager.Instance.IsActive))
        {
            return;
        }

        // 书桌视角锁定移动
        if (DeskViewController.IsActive)
        {
            playerController.SetMovementInput(0f);
            return;
        }

        var coordinator = SessionGameplayCoordinator.Instance;

        // 对白阶段锁定移动
        if (coordinator != null && coordinator.State.phase == GameplayPhase.Dialogue)
        {
            playerController.SetMovementInput(0f);
            return;
        }

        if (coordinator != null && (!string.IsNullOrEmpty(coordinator.State.pendingPhotoId) ||
                                    !string.IsNullOrEmpty(coordinator.State.previewPhotoId)))
        {
            playerController.SetMovementInput(0f);
            if (!string.IsNullOrEmpty(coordinator.State.pendingPhotoId))
                Debug.Log($"[PlayerInput] 移动被锁定：pendingPhotoId={coordinator.State.pendingPhotoId}（请收集照片）");
            return;
        }

        string hAxis = (inputSettings != null && !string.IsNullOrEmpty(inputSettings.HorizontalAxis))
             ? inputSettings.HorizontalAxis : horizontalAxis;

        playerController.SetMovementInput(Input.GetAxisRaw(hAxis));

        if (Input.GetKeyDown(GetInteractKey()))
        {
            playerController.QueueInteractInput();
        }
    }
}
