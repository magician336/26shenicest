using System;
using UnityEngine;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>一段故事及其对应人物的配置。物品信息已移至 ItemPlacement。</summary>
    [Serializable]
    public class BaguaStoryEntry
    {
        public string characterId;
        public string displayName;
        public Sprite portrait;
        public Sprite nameTagSprite;
        public AudioClip storyAudio;
        [Range(0f, 3f)]
        [Tooltip("播放音量倍率，1=原始音量。录音偏小时可调大。")]
        public float audioVolume = 1f;
        [TextArea] public string subtitle;
        public string age;   // 如 "18岁"
        public string title;  // 如 "大姐"
    }

    /// <summary>桌面物件的统一配置：正确配对物和干扰物共用此结构。</summary>
    [Serializable]
    public struct ItemPlacement
    {
        public string itemId;
        public string displayName;       // 正确物品有名字, 干扰物留空
        public Sprite sprite;            // 物件图标
        public Sprite filledSprite;      // 配对成功后的展示图（留空则用 sprite）
        public Vector2 anchoredPosition; // 桌面上固定坐标
        public bool isCorrect;           // 是否正确配对物
        public string characterId;       // isCorrect=true 时关联的人物ID
    }

    /// <summary>
    /// 八卦小游戏的 ScriptableObject 配置：人物、字幕、音频、桌面物件布局、
    /// 老照片、照片投放区坐标，全部在 Inspector 中配置。
    /// </summary>
    [CreateAssetMenu(menuName = "Data/MiniGame/Bagua Story Config", fileName = "BaguaStoryConfig")]
    public class BaguaStoryConfig : ScriptableObject
    {
        [SerializeField] private string miniGameId = "bagua_old_photo";
        [SerializeField] private string displayName = "八卦旧事";
        [SerializeField] private BaguaStoryEntry[] entries;
        [SerializeField] private ItemPlacement[] itemPlacements;
        [SerializeField] private Sprite oldFamilyPhoto;
        [SerializeField] private Sprite deskBackground;
        [SerializeField] private Sprite listenButtonSprite;
        [SerializeField] private Sprite daughterPhotoBackground;
        [SerializeField] private Sprite photoZoneSprite;
        [SerializeField] private string[] rewardIds = { "bagua_old_family_photo" };
        [Tooltip("完成后接入的对白序列 ID（照片收集后触发）")]
        [SerializeField] private string nextDialogueId;

        [Header("照片投放区（Inspector 配置坐标，不在代码中写死）")]
        [SerializeField] private PhotoZoneConfig[] photoZones;

        [Serializable]
        public struct PhotoZoneConfig
        {
            public string zoneId;
            public string correctCharacterId;
            public Vector2 anchoredPosition;
            public Vector2 size;
        }

        public string MiniGameId => miniGameId;
        public string DisplayName => displayName;
        public BaguaStoryEntry[] Entries => entries;
        public ItemPlacement[] ItemPlacements => itemPlacements;
        public Sprite OldFamilyPhoto => oldFamilyPhoto;
        public Sprite DeskBackground => deskBackground;
        public Sprite ListenButtonSprite => listenButtonSprite;
        public Sprite DaughterPhotoBackground => daughterPhotoBackground;
        public Sprite PhotoZoneSprite => photoZoneSprite;
        public string[] RewardIds => rewardIds;
        public string NextDialogueId => nextDialogueId;
        public PhotoZoneConfig[] PhotoZones => photoZones;

        /// <summary>查找指定人物的故事条目。</summary>
        public BaguaStoryEntry FindEntry(string characterId)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry != null && entry.characterId == characterId) return entry;
            }
            return null;
        }

        /// <summary>校验人物—物品配对是否正确。</summary>
        public bool IsCorrectMatch(string characterId, string itemId)
        {
            if (itemPlacements == null) return false;
            foreach (var item in itemPlacements)
            {
                if (item.isCorrect && item.characterId == characterId && item.itemId == itemId)
                    return true;
            }
            return false;
        }

        /// <summary>校验照片区域投放的姓名是否正确。</summary>
        public bool IsCorrectPhotoAssignment(string zoneId, string characterId)
        {
            if (photoZones == null) return false;
            foreach (var zone in photoZones)
            {
                if (zone.zoneId == zoneId) return zone.correctCharacterId == characterId;
            }
            return false;
        }

        /// <summary>获取桌面所需的全部物品 ID（正确 + 干扰物）。</summary>
        public string[] GetAllItemIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            if (itemPlacements != null)
            {
                foreach (var item in itemPlacements)
                {
                    if (!string.IsNullOrEmpty(item.itemId))
                        ids.Add(item.itemId);
                }
            }
            return ids.ToArray();
        }
    }
}
