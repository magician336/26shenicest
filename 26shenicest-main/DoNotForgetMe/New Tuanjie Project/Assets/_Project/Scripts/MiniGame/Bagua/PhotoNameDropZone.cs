using UnityEngine;

namespace DoNotForgetMe.MiniGame.Bagua
{
    /// <summary>
    /// 老照片上的透明姓名投放区。检测 DraggableItem 是否落在区域内。
    /// zoneId 由 Inspector 或运行时代码配置，不写死坐标。
    /// </summary>
    public class PhotoNameDropZone : MonoBehaviour
    {
        [SerializeField] private string zoneId;

        public string ZoneId
        {
            get => zoneId;
            set => zoneId = value;
        }

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>屏幕坐标是否落在本投放区内。</summary>
        public bool Contains(Vector2 screenPosition)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPosition, null);
        }
    }
}
