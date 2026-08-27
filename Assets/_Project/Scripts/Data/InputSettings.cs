using UnityEngine;

[CreateAssetMenu(menuName = "Data/Player/Input Settings", fileName = "PlayerInputSettings")]
public class InputSettings : ScriptableObject
{
    [Header("Axis Settings")]
    [SerializeField] private string horizontalAxis = "Horizontal";

    [Header("Action Keys")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    public string HorizontalAxis => horizontalAxis;
    public KeyCode InteractKey => interactKey;
    public KeyCode ExitKey => exitKey;

    [ContextMenu("Reset To Defaults")]
    public void ResetToDefaults()
    {
        horizontalAxis = "Horizontal";
        interactKey = KeyCode.F;
        exitKey = KeyCode.Escape;
    }
}
