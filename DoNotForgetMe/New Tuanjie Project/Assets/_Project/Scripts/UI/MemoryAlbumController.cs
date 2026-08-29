using System.Collections.Generic;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.UI
{
    /// <summary>探索阶段常驻的家庭记忆相册，以及待收集照片与预览界面。</summary>
    public class MemoryAlbumController : MonoBehaviour
    {
        private const int MaxPhotos = 6;

        private readonly Dictionary<string, string> _photoTitles = new()
        {
            { "photo_hongqiang", "红墙前的合影" },
            { "photo_hongfang", "刘洪芳的照片" },
            { "bagua_old_family_photo", "旧家庭照片" }
        };

        [Header("照片 Sprite（按收集顺序绑定）")]
        [SerializeField] private Sprite photo_hongqiang;
        [SerializeField] private Sprite photo_hongfang;
        [SerializeField] private Sprite bagua_old_family_photo;

        private SessionGameplayCoordinator _coordinator;
        private Canvas _canvas;
        private RectTransform _album;
        private GameObject _pickup;
        private GameObject _preview;
        private string _localPreviewPhotoId;

        private void Awake()
        {
            CreateCanvas();
            if (photo_hongqiang == null) photo_hongqiang = Resources.Load<Sprite>("photo_hongqiang");
            if (photo_hongfang == null) photo_hongfang = Resources.Load<Sprite>("photo_hongfang");
            if (bagua_old_family_photo == null) bagua_old_family_photo = Resources.Load<Sprite>("bagua_old_family_photo");
        }

        private void Start()
        {
            _coordinator = SessionGameplayCoordinator.Instance;
            if (_coordinator == null)
            {
                Debug.LogWarning("[MemoryAlbum] 缺少 SessionGameplayCoordinator");
                return;
            }

            _coordinator.StateChanged += Render;
            Render(_coordinator.State);
        }

        private void OnDestroy()
        {
            if (_coordinator != null) _coordinator.StateChanged -= Render;
        }

        private void Render(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;
            var isExploring = snapshot.phase == GameplayPhase.Exploration;
            _album.gameObject.SetActive(isExploring);
            ClearChildren(_album);
            if (isExploring) RenderAlbum(snapshot.collectedPhotoIds);

            if (!isExploring || string.IsNullOrEmpty(snapshot.pendingPhotoId)) DestroyPickup();
            else
            {
                Debug.Log($"[MemoryAlbum] ShowPickup pendingPhotoId={snapshot.pendingPhotoId} role={NetworkSessionManager.Service.Role} phase={snapshot.phase}");
                ShowPickup(snapshot.pendingPhotoId);
            }

            if (!string.IsNullOrEmpty(snapshot.previewPhotoId)) ShowPreview(snapshot.previewPhotoId, true);
            else if (_localPreviewPhotoId == null) DestroyPreview();
        }

        private void RenderAlbum(List<string> photoIds)
        {
            var count = Mathf.Min(photoIds?.Count ?? 0, MaxPhotos);

            // 无照片时不显示
            if (count == 0)
            {
                _album.sizeDelta = Vector2.zero;
                return;
            }

            // 面板宽度 = 左边距 + 照片数 × 槽位间距 + 右边距
            _album.sizeDelta = new Vector2(40 + count * 120 + 40, 150);

            CreateText(_album, "相册", new Vector2(40, -14), new Vector2(80, 30), 20, TextAnchor.MiddleLeft);
            var paperBg = Resources.Load<Sprite>("paper_bg");
            for (var index = 0; index < count; index++)
            {
                var slotCenter = new Vector2(40 + index * 120 + 50, -80);

                // 稿纸底图（与照片槽位同位置同锚点，稍大）
                if (paperBg != null)
                {
                    var paperGo = new GameObject("PaperBg_" + index, typeof(Image));
                    paperGo.transform.SetParent(_album, false);
                    var paperRect = paperGo.GetComponent<RectTransform>();
                    paperRect.anchorMin = paperRect.anchorMax = new Vector2(0.5f, 0.5f);
                    paperRect.anchoredPosition = slotCenter;
                    paperRect.sizeDelta = new Vector2(125, 125);
                    paperGo.GetComponent<Image>().sprite = paperBg;
                    paperGo.GetComponent<Image>().color = Color.white;
                    paperGo.GetComponent<Image>().preserveAspect = false;
                    paperGo.GetComponent<Image>().raycastTarget = false;
                    paperGo.transform.SetAsFirstSibling();
                }

                var slot = CreateButton(_album, "PhotoSlot_" + index, slotCenter, new Vector2(85, 85));
                var image = slot.GetComponent<Image>();
                var photoId = photoIds[index];
                var photoSprite = GetPhotoSprite(photoId);
                if (photoSprite != null)
                {
                    image.sprite = photoSprite;
                    image.color = Color.white;
                    image.preserveAspect = false;
                }
                else
                {
                    image.color = new Color(0.73f, 0.59f, 0.38f, 1f);
                    CreateText(slot.GetComponent<RectTransform>(), "相片", Vector2.zero, new Vector2(58, 30), 13, TextAnchor.MiddleCenter);
                }
                slot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _localPreviewPhotoId = photoId;
                    ShowPreview(photoId, false);
                });
            }
        }

        private void ShowPickup(string photoId)
        {
            var isHost = NetworkSessionManager.Service.Role == SessionRole.Host;
            if (_pickup != null && _pickup.name == "Pending_" + photoId)
            {
                // 角色可能已变化，更新按钮可交互状态
                _pickup.GetComponent<Button>().interactable = isHost;
                return;
            }
            DestroyPickup();
            _pickup = CreateButton(_canvas.transform as RectTransform, "Pending_" + photoId,
                new Vector2(-130, 130), new Vector2(180, 150));
            var rect = _pickup.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            _pickup.GetComponent<Image>().color = new Color(1f, 0.78f, 0.28f, 0.96f);
            CreateText(rect, "新照片\n点击收集", Vector2.zero, new Vector2(170, 120), 22, TextAnchor.MiddleCenter);
            var button = _pickup.GetComponent<Button>();
            button.interactable = isHost;
            if (isHost)
            {
                button.onClick.AddListener(() => _coordinator.Request(
                    new GameplayIntent(GameplayIntentType.CollectMemoryPhoto, null, photoId)));
            }
            else
            {
                button.interactable = false;
            }
        }

        private void ShowPreview(string photoId, bool isRewardPreview)
        {
            DestroyPreview();
            _preview = new GameObject("PhotoPreview", typeof(Image));
            _preview.transform.SetParent(_canvas.transform, false);
            var background = _preview.GetComponent<Image>();
            background.color = new Color(0.04f, 0.03f, 0.02f, 0.9f);
            var rect = _preview.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // 稿纸底图（比照片大）
            var paperBg = Resources.Load<Sprite>("paper_bg");
            if (paperBg != null)
            {
                var paperGo = new GameObject("PaperBg", typeof(Image));
                paperGo.transform.SetParent(rect, false);
                var paperRect = paperGo.GetComponent<RectTransform>();
                paperRect.anchorMin = paperRect.anchorMax = new Vector2(0.5f, 0.5f);
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.sizeDelta = new Vector2(960, 720);
                paperGo.GetComponent<Image>().sprite = paperBg;
                paperGo.GetComponent<Image>().color = Color.white;
                paperGo.GetComponent<Image>().preserveAspect = false;
                paperGo.GetComponent<Image>().raycastTarget = false;
            }

            var card = new GameObject("Photo", typeof(Image)).GetComponent<RectTransform>();
            card.SetParent(rect, false);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(760, 520);
            var cardImage = card.GetComponent<Image>();
            var previewSprite = GetPhotoSprite(photoId);
            if (previewSprite != null)
            {
                cardImage.sprite = previewSprite;
                cardImage.color = Color.white;
                cardImage.preserveAspect = true;
            }
            else
            {
                cardImage.color = new Color(0.62f, 0.48f, 0.3f);
            }
            CreateText(card, GetTitle(photoId), new Vector2(0, 0), new Vector2(680, 100), 40, TextAnchor.MiddleCenter);

            if (isRewardPreview && NetworkSessionManager.Service.Role != SessionRole.Host)
            {
                CreateText(rect, "等待 Host 关闭照片预览", new Vector2(0, -340), new Vector2(600, 60), 24, TextAnchor.MiddleCenter);
                return;
            }

            var close = CreateButton(rect, "Close", new Vector2(700, 390), new Vector2(70, 70));
            close.GetComponent<Image>().color = new Color(0.55f, 0.18f, 0.15f, 1f);
            CreateText(close.GetComponent<RectTransform>(), "×", Vector2.zero, new Vector2(60, 60), 42, TextAnchor.MiddleCenter);
            close.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (isRewardPreview) _coordinator.Request(new GameplayIntent(GameplayIntentType.CloseMemoryPhotoPreview));
                else
                {
                    _localPreviewPhotoId = null;
                    DestroyPreview();
                }
            });
        }

        private string GetTitle(string photoId) => _photoTitles.TryGetValue(photoId, out var title) ? title : photoId;

        private Sprite GetPhotoSprite(string photoId)
        {
            return photoId switch
            {
                "photo_hongqiang" => photo_hongqiang,
                "photo_hongfang" => photo_hongfang,
                "bagua_old_family_photo" => bagua_old_family_photo,
                _ => null
            };
        }

        private void DestroyPickup()
        {
            if (_pickup != null) Destroy(_pickup);
            _pickup = null;
        }

        private void DestroyPreview()
        {
            if (_preview != null) Destroy(_preview);
            _preview = null;
        }

        private void CreateCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            _album = new GameObject("MemoryAlbum", typeof(RectTransform)).GetComponent<RectTransform>();
            _album.SetParent(transform, false);
            _album.anchorMin = _album.anchorMax = new Vector2(0, 1);
            _album.pivot = new Vector2(0, 1);
            _album.anchoredPosition = new Vector2(24, -24);
            _album.sizeDelta = Vector2.zero;
        }

        private static GameObject CreateButton(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Button>().targetGraphic = go.GetComponent<Image>();
            return go;
        }

        private static Text CreateText(RectTransform parent, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject("Label", typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private static void ClearChildren(Transform transform)
        {
            for (var i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        }
    }
}
