using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Album
{
    /// <summary>
    /// 全家福相册小游戏的纯视图绑定脚本。挂在 prefab 根节点上。
    /// 不含任何游戏逻辑——AlbumMiniGameView 通过此组件访问 UI 元素并更新数据。
    /// </summary>
    public class AlbumView : MonoBehaviour
    {
        // ==============================
        // 共享
        // ==============================

        [Header("Shared")]
        [SerializeField] private Image _albumBaseImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _instructionText;

        // ==============================
        // 轮廓区域
        // ==============================

        [Header("Zones")]
        [SerializeField] private StickerZoneView[] _stickerZones;
        [SerializeField] private NameTagZoneView[] _nameTagZones;

        // ==============================
        // 可拖拽候选
        // ==============================

        [Header("Draggables")]
        [SerializeField] private StickerDraggableView[] _stickerDraggables;
        [SerializeField] private NameTagDraggableView[] _nameTagDraggables;

        // ==============================
        // 功能按钮
        // ==============================

        [Header("Buttons")]
        [SerializeField] private GameObject _clueButtonRoot;
        [SerializeField] private Button _clueButton;
        [SerializeField] private Text _clueButtonLabel;
        [SerializeField] private GameObject _completeButtonRoot;
        [SerializeField] private Button _completeButton;
        [SerializeField] private Text _completeButtonLabel;

        // ==============================
        // 线索面板
        // ==============================

        [Header("Clue Panel")]
        [SerializeField] private GameObject _cluePanelRoot;
        [SerializeField] private Image _cluePanelImage;
        [SerializeField] private Text _cluePanelTitle;
        [SerializeField] private CluePhotoView[] _cluePhotos;
        [SerializeField] private GameObject _closeClueButtonRoot;
        [SerializeField] private Button _closeClueButton;
        [SerializeField] private Text _closeClueButtonLabel;

        // ==============================
        // 完成动画
        // ==============================

        [Header("Complete Animation")]
        [SerializeField] private Image _familyPortraitImage;
        [SerializeField] private Image _blackScreenImage;

        // ==============================
        // 公开属性
        // ==============================

        public Image AlbumBaseImage => _albumBaseImage;
        public Text TitleText => _titleText;
        public Text InstructionText => _instructionText;

        public StickerZoneView[] StickerZones => _stickerZones;
        public NameTagZoneView[] NameTagZones => _nameTagZones;

        public StickerDraggableView[] StickerDraggables => _stickerDraggables;
        public NameTagDraggableView[] NameTagDraggables => _nameTagDraggables;

        public GameObject ClueButtonRoot => _clueButtonRoot;
        public Button ClueButton => _clueButton;
        public Text ClueButtonLabel => _clueButtonLabel;
        public GameObject CompleteButtonRoot => _completeButtonRoot;
        public Button CompleteButton => _completeButton;
        public Text CompleteButtonLabel => _completeButtonLabel;

        public GameObject CluePanelRoot => _cluePanelRoot;
        public Image CluePanelImage => _cluePanelImage;
        public Text CluePanelTitle => _cluePanelTitle;
        public CluePhotoView[] CluePhotos => _cluePhotos;
        public GameObject CloseClueButtonRoot => _closeClueButtonRoot;
        public Button CloseClueButton => _closeClueButton;
        public Text CloseClueButtonLabel => _closeClueButtonLabel;

        public Image FamilyPortraitImage => _familyPortraitImage;
        public Image BlackScreenImage => _blackScreenImage;
    }

    [Serializable]
    public struct StickerZoneView
    {
        public GameObject root;
        public Image image;
    }

    [Serializable]
    public struct NameTagZoneView
    {
        public GameObject root;
        public Image image;
        public Text labelText;
    }

    [Serializable]
    public struct StickerDraggableView
    {
        public GameObject root;
        public Image image;
        public DraggableItem draggable;
    }

    [Serializable]
    public struct NameTagDraggableView
    {
        public GameObject root;
        public Image image;
        public DraggableItem draggable;
        public Text labelText;
    }

    [Serializable]
    public struct CluePhotoView
    {
        public GameObject root;
        public Image photoImage;
        public GameObject noteRoot;
        public Image noteImage;
        public Text noteText;
    }
}
