using UnityEngine;

public class MiniGameTrigger : MonoBehaviour, IInteractable
{
    [Header("小游戏配置")]
    [SerializeField] private string miniGameId = "SampleGame";
    [SerializeField] private MiniGameSettings settings;

    public void TriggerInteract()
    {
        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("[MiniGameTrigger] 场景中未找到 MiniGameManager");
            return;
        }

        MiniGameManager.Instance.StartMiniGame(miniGameId, settings);
    }
}
