using System;
using UnityEngine;

namespace DoNotForgetMe.MiniGame.Album
{
    /// <summary>
    /// 全家福相册中一个人物的完整配置：贴纸素材、姓名tag、线索信息、轮廓/名牌坐标。
    /// hasSticker=false 的人物（刘洪梅/小岩）没有贴纸和姓名tag，轮廓始终空缺。
    /// </summary>
    [Serializable]
    public class AlbumCharacterEntry
    {
        public string characterId;
        public string displayName;
        public Sprite stickerSprite;
        public Sprite photoSprite;
        [TextArea] public string clueText;
        public Vector2 stickerZonePosition;
        public Vector2 stickerZoneSize = new Vector2(150, 200);
        public Vector2 nameTagZonePosition;
        public Vector2 nameTagZoneSize = new Vector2(200, 60);
        public bool hasSticker = true;
    }

    /// <summary>
    /// 全家福相册小游戏的 ScriptableObject 配置。
    /// 6个人物轮廓（5个有贴纸，1个空缺）、写实风全家福、前置照片要求。
    /// </summary>
    [CreateAssetMenu(menuName = "Data/MiniGame/Album Config", fileName = "AlbumConfig")]
    public class AlbumConfig : ScriptableObject
    {
        [SerializeField] private string miniGameId = "album_family_portrait";
        [SerializeField] private string displayName = "全家福认人";
        [SerializeField] private AlbumCharacterEntry[] entries;
        [SerializeField] private Sprite realisticFamilyPortrait;
        [SerializeField] private string[] requiredPhotoIds = { "photo_hongqiang", "photo_hongfang", "bagua_old_family_photo" };

        public string MiniGameId => miniGameId;
        public string DisplayName => displayName;
        public AlbumCharacterEntry[] Entries => entries;
        public Sprite RealisticFamilyPortrait => realisticFamilyPortrait;
        public string[] RequiredPhotoIds => requiredPhotoIds;

        /// <summary>查找指定人物的配置条目。</summary>
        public AlbumCharacterEntry FindEntry(string characterId)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry != null && entry.characterId == characterId) return entry;
            }
            return null;
        }

        /// <summary>获取所有有贴纸的人物条目（排除 hasSticker=false）。</summary>
        public AlbumCharacterEntry[] GetStickerEntries()
        {
            var result = new System.Collections.Generic.List<AlbumCharacterEntry>();
            if (entries == null) return result.ToArray();
            foreach (var entry in entries)
            {
                if (entry != null && entry.hasSticker) result.Add(entry);
            }
            return result.ToArray();
        }
    }
}
