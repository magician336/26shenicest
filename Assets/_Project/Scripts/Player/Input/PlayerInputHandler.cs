using UnityEngine;
using DoNotForgetMe.Network;

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

        string hAxis = (inputSettings != null && !string.IsNullOrEmpty(inputSettings.HorizontalAxis))
             ? inputSettings.HorizontalAxis : horizontalAxis;

        playerController.SetMovementInput(Input.GetAxisRaw(hAxis));

        if (Input.GetKeyDown(GetInteractKey()))
        {
            playerController.QueueInteractInput();
        }
    }
}
