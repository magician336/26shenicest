using UnityEngine;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;

namespace DoNotForgetMe.Dialogue
{
    /// <summary>
    /// 世界空间对白触发器：玩家走入区域或交互指定物体时，
    /// 向 Coordinator 发送 StartDialogue Intent。
    /// 与 MiniGameTrigger 平行。
    /// </summary>
    public class DialogueTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string dialogueSequenceId;
        [Tooltip("是否仅 Host 可触发")]
        [SerializeField] private bool requiresHost = true;
        [Tooltip("是否只触发一次")]
        [SerializeField] private bool triggerOnce = true;

        private bool _triggered;

        public void TriggerInteract()
        {
            if (requiresHost && NetworkSessionManager.Service != null &&
                NetworkSessionManager.Service.Role != SessionRole.Host) return;

            if (triggerOnce && _triggered) return;
            if (string.IsNullOrEmpty(dialogueSequenceId)) return;

            _triggered = true;

            var intent = new GameplayIntent(GameplayIntentType.StartDialogue)
            {
                dialogueSequenceId = dialogueSequenceId
            };
            SessionGameplayCoordinator.Instance?.Request(intent);
        }
    }
}
