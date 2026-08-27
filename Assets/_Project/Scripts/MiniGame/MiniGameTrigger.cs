using UnityEngine;
using DoNotForgetMe.Network;

public class MiniGameTrigger : MonoBehaviour, IInteractable
{
    [Header("小游戏配置")]
    [SerializeField] private string miniGameId = "SampleGame";
    [SerializeField] private MiniGameSettings settings;

    public void TriggerInteract()
    {
        if (NetworkSessionManager.Service.Role != SessionRole.Host)
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
}
