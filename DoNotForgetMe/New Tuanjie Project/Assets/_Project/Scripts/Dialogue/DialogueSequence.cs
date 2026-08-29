using System;
using UnityEngine;

namespace DoNotForgetMe.Dialogue
{
    /// <summary>对白序列中的单条台词。</summary>
    [Serializable]
    public class DialogueEntry
    {
        public string speaker;
        [TextArea] public string text;
        public bool isInternal;
        public AudioClip audioClip;
    }

    /// <summary>
    /// 对白序列资产：一段以电影字幕形式呈现的角色台词序列。
    /// 可通过 nextMiniGameId / nextDialogueId 链接到下一个流程节点，
    /// 与小游戏资产互相引用、模块化拼接。
    /// </summary>
    [CreateAssetMenu(menuName = "Data/Dialogue/Dialogue Sequence", fileName = "DialogueSequence")]
    public class DialogueSequence : ScriptableObject
    {
        [SerializeField] private string sequenceId;
        [SerializeField] private DialogueEntry[] entries;
        [Tooltip("true = 整场黑框常驻（电影模式）；false = 逐条黑底条淡入淡出")]
        [SerializeField] private bool cinematicMode = true;
        [Tooltip("播完后自动启动的小游戏 ID")]
        [SerializeField] private string nextMiniGameId;
        [Tooltip("播完后自动启动的下一段对白 ID")]
        [SerializeField] private string nextDialogueId;
        [Tooltip("播完后自动转场到的目标场景名（使用 SceneNames 常量）")]
        [SerializeField] private string nextSceneName;

        public string SequenceId => sequenceId;
        public DialogueEntry[] Entries => entries;
        public bool CinematicMode => cinematicMode;
        public string NextMiniGameId => nextMiniGameId;
        public string NextDialogueId => nextDialogueId;
        public string NextSceneName => nextSceneName;

        public DialogueEntry GetEntry(int index)
        {
            if (entries == null || index < 0 || index >= entries.Length) return null;
            return entries[index];
        }

        public int EntryCount => entries != null ? entries.Length : 0;
    }
}
