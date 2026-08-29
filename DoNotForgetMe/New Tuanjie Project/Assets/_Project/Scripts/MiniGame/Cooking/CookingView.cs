using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.MiniGame.Cooking
{
    /// <summary>
    /// 做饭小游戏的纯视图绑定脚本。挂在 prefab 根节点上，
    /// 通过 [SerializeField] 暴露所有 UI 元素引用。
    /// 不含任何游戏逻辑——CookingMiniGame 通过此组件访问 UI 元素并更新数据。
    /// </summary>
    public class CookingView : MonoBehaviour
    {
        // ==============================
        // 共享元素
        // ==============================

        [Header("Shared")]
        [SerializeField] private Image _background;
        [SerializeField] private Text _waitingText; // "等待联机角色…"

        // ==============================
        // 母亲端 (Client)
        // ==============================

        [Header("Mother Panel (Client)")]
        [SerializeField] private GameObject _motherPanel;
        [SerializeField] private Text _motherRoleText;
        [SerializeField] private Text _motherInstructionText;
        [SerializeField] private RectTransform _motherContainerZone;
        [SerializeField] private Text _motherDroppedNamesText;
        [SerializeField] private IngredientSlotView[] _motherIngredientSlots;
        [SerializeField] private Text _motherWaitingText;
        [SerializeField] private Text _motherRecipeNoteText;
        [SerializeField] private Image _motherHintImage;
        [SerializeField] private Text _motherCompleteText;
        [SerializeField] private Image _motherDishPhoto;

        // ==============================
        // 女儿端 (Host)
        // ==============================

        [Header("Daughter Panel (Host)")]
        [SerializeField] private GameObject _daughterPanel;
        [SerializeField] private Text _daughterRoleText;
        [SerializeField] private Text _daughterWaitingText;
        [SerializeField] private Text _daughterInstructionText;
        [SerializeField] private RectTransform _daughterDishZone;
        [SerializeField] private Image _daughterDishPhoto;
        [SerializeField] private IngredientSlotView[] _daughterSeasoningSlots;
        [SerializeField] private Text _daughterCompleteText;
        [SerializeField] private Image _rewardPhotoImage;
        [SerializeField] private Text _photoLabelText;
        [SerializeField] private GameObject _collectButtonRoot;
        [SerializeField] private Button _collectButton;
        [SerializeField] private Image _collectButtonImage;
        [SerializeField] private Text _collectButtonLabel;
        [SerializeField] private Image _collectGlowImage;
        [SerializeField] private Text _collectedText;
        [SerializeField] private GameObject _interruptButtonRoot;
        [SerializeField] private Button _interruptButton;
        [SerializeField] private Text _interruptButtonLabel;

        // ==============================
        // 公开属性 (只读)
        // ==============================

        public Image Background => _background;
        public Text WaitingText => _waitingText;

        public GameObject MotherPanel => _motherPanel;
        public Text MotherRoleText => _motherRoleText;
        public Text MotherInstructionText => _motherInstructionText;
        public RectTransform MotherContainerZone => _motherContainerZone;
        public Text MotherDroppedNamesText => _motherDroppedNamesText;
        public IngredientSlotView[] MotherIngredientSlots => _motherIngredientSlots;
        public Text MotherWaitingText => _motherWaitingText;
        public Text MotherRecipeNoteText => _motherRecipeNoteText;
        public Image MotherHintImage => _motherHintImage;
        public Text MotherCompleteText => _motherCompleteText;
        public Image MotherDishPhoto => _motherDishPhoto;

        public GameObject DaughterPanel => _daughterPanel;
        public Text DaughterRoleText => _daughterRoleText;
        public Text DaughterWaitingText => _daughterWaitingText;
        public Text DaughterInstructionText => _daughterInstructionText;
        public RectTransform DaughterDishZone => _daughterDishZone;
        public Image DaughterDishPhoto => _daughterDishPhoto;
        public IngredientSlotView[] DaughterSeasoningSlots => _daughterSeasoningSlots;
        public Text DaughterCompleteText => _daughterCompleteText;
        public Image RewardPhotoImage => _rewardPhotoImage;
        public Text PhotoLabelText => _photoLabelText;
        public GameObject CollectButtonRoot => _collectButtonRoot;
        public Button CollectButton => _collectButton;
        public Image CollectButtonImage => _collectButtonImage;
        public Text CollectButtonLabel => _collectButtonLabel;
        public Image CollectGlowImage => _collectGlowImage;
        public Text CollectedText => _collectedText;
        public GameObject InterruptButtonRoot => _interruptButtonRoot;
        public Button InterruptButton => _interruptButton;
        public Text InterruptButtonLabel => _interruptButtonLabel;
    }

    /// <summary>
    /// 食材/调料的可拖拽槽位。每个槽位包含根节点、图片和拖拽组件。
    /// 用于母亲端食材和女儿端调料的重复结构。
    /// </summary>
    [Serializable]
    public struct IngredientSlotView
    {
        public GameObject root;
        public Image image;
        public DraggableItem draggable;
    }
}
