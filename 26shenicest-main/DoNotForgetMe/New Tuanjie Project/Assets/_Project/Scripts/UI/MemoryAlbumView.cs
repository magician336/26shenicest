using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.UI
{
    /// <summary>
    /// 家庭记忆相册（物品栏）的纯视图绑定脚本。挂在 prefab 根节点上。
    /// 不含任何游戏逻辑——MemoryAlbumController 通过此组件访问 UI 元素。
    /// </summary>
    public class MemoryAlbumView : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;

        [Header("Album Bar")]
        [SerializeField] private RectTransform _albumPanel;
        [SerializeField] private Text _albumTitleText;
        [SerializeField] private AlbumPhotoSlotView[] _photoSlots;

        [Header("Overlay")]
        [SerializeField] private GameObject _inputBlocker;

        [Header("Photo Preview")]
        [SerializeField] private GameObject _previewRoot;
        [SerializeField] private Image _previewBackground;
        [SerializeField] private Image _previewPaperBg;
        [SerializeField] private Image _previewPhotoImage;
        [SerializeField] private Text _previewTitleText;
        [SerializeField] private GameObject _previewCloseButtonRoot;
        [SerializeField] private Button _previewCloseButton;
        [SerializeField] private Image _previewCloseButtonImage;
        [SerializeField] private Text _previewCloseButtonLabel;
        [SerializeField] private GameObject _previewWaitingText;

        public Canvas Canvas => _canvas;
        public RectTransform AlbumPanel => _albumPanel;
        public Text AlbumTitleText => _albumTitleText;
        public AlbumPhotoSlotView[] PhotoSlots => _photoSlots;
        public GameObject InputBlocker => _inputBlocker;

        public GameObject PreviewRoot => _previewRoot;
        public Image PreviewBackground => _previewBackground;
        public Image PreviewPaperBg => _previewPaperBg;
        public Image PreviewPhotoImage => _previewPhotoImage;
        public Text PreviewTitleText => _previewTitleText;
        public GameObject PreviewCloseButtonRoot => _previewCloseButtonRoot;
        public Button PreviewCloseButton => _previewCloseButton;
        public Image PreviewCloseButtonImage => _previewCloseButtonImage;
        public Text PreviewCloseButtonLabel => _previewCloseButtonLabel;
        public GameObject PreviewWaitingText => _previewWaitingText;
    }

    [Serializable]
    public struct AlbumPhotoSlotView
    {
        public GameObject paperRoot;
        public Image paperImage;
        public GameObject slotRoot;
        public Image photoImage;
        public Button photoButton;
        public Text placeholderText;
    }
}
