using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>
    /// 八卦小游戏的纯视图绑定脚本。挂在 prefab 根节点上。
    /// 不含任何游戏逻辑——BaguaMiniGameView 通过此组件访问 UI 元素并更新数据。
    /// </summary>
    public class BaguaView : MonoBehaviour
    {
        // ==============================
        // 共享
        // ==============================

        [Header("Shared")]
        [SerializeField] private Image _background;
        [SerializeField] private Text _waitingText;

        // ==============================
        // 母亲端 (Client) — 桌面配对
        // ==============================

        [Header("Client Panel")]
        [SerializeField] private GameObject _clientPanel;
        [SerializeField] private Text _taskBannerText;
        [SerializeField] private Image _desktopTrayImage;
        [SerializeField] private BaguaItemSlotView[] _desktopItemSlots;
        [SerializeField] private CharacterCardView[] _characterCards;
        [SerializeField] private Text _clientWaitingText;

        // ==============================
        // 女儿端 (Host) — 照片认人
        // ==============================

        [Header("Host Panel")]
        [SerializeField] private GameObject _hostPanel;
        [SerializeField] private Text _hostRoleText;
        [SerializeField] private Text _hostWaitingText;
        [SerializeField] private Image _photoBackgroundImage;
        [SerializeField] private Text _photoInstructionText;
        [SerializeField] private PhotoZoneView[] _photoZones;
        [SerializeField] private NameTagSlotView[] _nameTagSlots;

        // ==============================
        // 完成视图
        // ==============================

        [Header("Complete")]
        [SerializeField] private Text _completeText;
        [SerializeField] private Image _rewardPhotoImage;
        [SerializeField] private Text _photoLabelText;
        [SerializeField] private GameObject _collectButtonRoot;
        [SerializeField] private Button _collectButton;
        [SerializeField] private Text _collectButtonLabel;
        [SerializeField] private Text _collectedText;

        // ==============================
        // 字幕条
        // ==============================

        [Header("Subtitle")]
        [SerializeField] private GameObject _subtitleBarRoot;
        [SerializeField] private Text _subtitleText;

        // ==============================
        // 公开属性
        // ==============================

        public Image Background => _background;
        public Text WaitingText => _waitingText;

        public GameObject ClientPanel => _clientPanel;
        public Text TaskBannerText => _taskBannerText;
        public Image DesktopTrayImage => _desktopTrayImage;
        public BaguaItemSlotView[] DesktopItemSlots => _desktopItemSlots;
        public CharacterCardView[] CharacterCards => _characterCards;
        public Text ClientWaitingText => _clientWaitingText;

        public GameObject HostPanel => _hostPanel;
        public Text HostRoleText => _hostRoleText;
        public Text HostWaitingText => _hostWaitingText;
        public Image PhotoBackgroundImage => _photoBackgroundImage;
        public Text PhotoInstructionText => _photoInstructionText;
        public PhotoZoneView[] PhotoZones => _photoZones;
        public NameTagSlotView[] NameTagSlots => _nameTagSlots;

        public Text CompleteText => _completeText;
        public Image RewardPhotoImage => _rewardPhotoImage;
        public Text PhotoLabelText => _photoLabelText;
        public GameObject CollectButtonRoot => _collectButtonRoot;
        public Button CollectButton => _collectButton;
        public Text CollectButtonLabel => _collectButtonLabel;
        public Text CollectedText => _collectedText;

        public GameObject SubtitleBarRoot => _subtitleBarRoot;
        public Text SubtitleText => _subtitleText;
    }

    /// <summary>桌面物件的可拖拽槽位。</summary>
    [Serializable]
    public struct BaguaItemSlotView
    {
        public GameObject root;
        public Image image;
        public DraggableItem draggable;
    }

    /// <summary>人物卡片视图。</summary>
    [Serializable]
    public struct CharacterCardView
    {
        public GameObject root;
        public Image cardImage;
        public Image portraitImage;
        public Text nameText;
        public Text ageTitleText;
        public GameObject audioButtonRoot;
        public Button audioButton;
        public Image audioButtonImage;
        public RectTransform dropSlotRect;
        public GameObject filledItemRoot;
        public Image filledItemImage;
        public Text filledItemNameText;
    }

    /// <summary>照片投放区视图。</summary>
    [Serializable]
    public struct PhotoZoneView
    {
        public GameObject root;
        public Image image;
        public PhotoNameDropZone dropZone;
    }

    /// <summary>姓名标签可拖拽槽位。</summary>
    [Serializable]
    public struct NameTagSlotView
    {
        public GameObject root;
        public Image image;
        public DraggableItem draggable;
        public Text labelText;
    }
}
