using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private InputSettings inputSettings;
    [SerializeField] private PlayerSettings playerSettings;

    public float interactRange = 1.5f;
    public LayerMask interactLayer;

    public KeyCode InteractKey => inputSettings != null ? inputSettings.InteractKey : KeyCode.F;

    public void ApplyInputSettings(InputSettings settings)
    {
        inputSettings = settings;
    }

    public void ApplyPlayerSettings(PlayerSettings settings)
    {
        playerSettings = settings;
        if (playerSettings != null)
        {
            interactRange = playerSettings.InteractRange;
        }
    }

    public bool TryInteract()
    {
        Vector2 checkPos = transform.position;

        Collider2D hit = Physics2D.OverlapCircle(checkPos, interactRange, interactLayer);
        if (hit == null)
        {
            return false;
        }

        IInteractable target = hit.GetComponent<IInteractable>();
        if (target == null)
        {
            target = hit.GetComponentInParent<IInteractable>();
        }

        if (target == null)
        {
            return false;
        }

        target.TriggerInteract();
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
