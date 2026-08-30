using System.Collections;
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

        [Header("照片标题（放大预览时显示）")]
        [SerializeField] private string _titleHongqiang = "红墙前的合影";
        [SerializeField] private string _titleHongfang = "刘洪芳的照片";
        [SerializeField] private string _titleOldFamilyPhoto = "旧家庭照片";

        [Header("照片 Sprite（按收集顺序绑定）")]
        [SerializeField] private Sprite photo_hongqiang;
        [SerializeField] private Sprite photo_hongfang;
        [SerializeField] private Sprite bagua_old_family_photo;

        [Header("UI 素材")]
        [Tooltip("照片预览的关闭按钮 Sprite；留空则使用纯色背景")]
        [SerializeField] private Sprite _closeButtonSprite;

        private SessionGameplayCoordinator _coordinator;
        private Canvas _canvas;
        private RectTransform _album;
        private GameObject _pickup;
        private GameObject _preview;
        private string _localPreviewPhotoId;
        private string _activePreviewPhotoId;
        private GameObject _inputBlocker;
        private MemoryAlbumView _view;
        private bool _usePrefab;
        private AlbumPhotoSlotView[] _prefabSlots;
        private Sprite _paperBgSprite;
        private List<string> _lastCollectedPhotoIds = new();

        /// <summary>飞行动画进行中标记，供 PlayerInputHandler 等外部组件查询。</summary>
        public static bool IsPhotoFlying { get; private set; }

        private void Awake()
        {
            _view = GetComponent<MemoryAlbumView>();
            if (_view != null && _view.Canvas != null)
            {
                _usePrefab = true;
                _canvas = _view.Canvas;
                _album = _view.AlbumPanel;
                _inputBlocker = _view.InputBlocker;
                _prefabSlots = _view.PhotoSlots;
                _paperBgSprite = Resources.Load<Sprite>("paper_bg");
                HideAllPrefabSlots();
                HidePreviewRoot();
            }
            else
            {
                CreateCanvas();
            }

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
            IsPhotoFlying = false;
        }

        private void Render(GameplaySnapshot snapshot)
        {
            if (snapshot == null) return;

            // 检测新增照片
            string newPhotoId = null;
            foreach (var id in snapshot.collectedPhotoIds)
            {
                if (!_lastCollectedPhotoIds.Contains(id))
                {
                    newPhotoId = id;
                    break;
                }
            }
            _lastCollectedPhotoIds = new List<string>(snapshot.collectedPhotoIds);

            // 相册在 Exploration 和 MiniGame 阶段显示
            var showAlbum = snapshot.phase == GameplayPhase.Exploration
                          || snapshot.phase == GameplayPhase.MiniGame;
            _album.gameObject.SetActive(showAlbum);
            if (!_usePrefab) ClearChildren(_album);
            if (showAlbum) RenderAlbum(snapshot.collectedPhotoIds);

            // 小游戏内检测到新照片 → 启动飞回动画
            if (newPhotoId != null && snapshot.phase == GameplayPhase.MiniGame && !IsPhotoFlying)
            {
                var targetIndex = snapshot.collectedPhotoIds.IndexOf(newPhotoId);
                StartCoroutine(FlyPhotoToAlbum(newPhotoId, targetIndex));
                StartCoroutine(FlyPhotoTimeout(3f));
            }

            // Pickup / Preview 逻辑（仅 Exploration）
            var isExploring = snapshot.phase == GameplayPhase.Exploration;

            if (!isExploring || string.IsNullOrEmpty(snapshot.pendingPhotoId)) DestroyPickup();
            else
            {
                Debug.Log($"[MemoryAlbum] ShowPickup pendingPhotoId={snapshot.pendingPhotoId} role={NetworkSessionManager.Service.Role} phase={snapshot.phase}");
                ShowPickup(snapshot.pendingPhotoId);
            }

            // Preview 逻辑（仅 Exploration 阶段处理 reward preview）
            if (isExploring && !string.IsNullOrEmpty(snapshot.previewPhotoId))
            {
                if (_activePreviewPhotoId != snapshot.previewPhotoId)
                    ShowPreview(snapshot.previewPhotoId, true);
            }
            else if (_localPreviewPhotoId == null)
            {
                DestroyPreview();
            }
        }

        private void RenderAlbum(List<string> photoIds)
        {
            var count = Mathf.Min(photoIds?.Count ?? 0, MaxPhotos);

            // 无照片时不显示
            if (count == 0)
            {
                _album.sizeDelta = Vector2.zero;
                if (_usePrefab) HideAllPrefabSlots();
                return;
            }

            // 面板宽度 = 左边距 + 照片数 × 槽位间距 + 右边距
            _album.sizeDelta = new Vector2(40 + count * 120 + 40, 150);

            if (_usePrefab)
                RenderAlbumFromPrefab(photoIds, count);
            else
                RenderAlbumFromCode(photoIds, count);
        }

        private void RenderAlbumFromPrefab(List<string> photoIds, int count)
        {
            if (_view != null && _view.AlbumTitleText != null)
                _view.AlbumTitleText.gameObject.SetActive(true);

            for (var i = 0; i < _prefabSlots.Length; i++)
            {
                var slot = _prefabSlots[i];
                var visible = i < count;
                if (slot.paperRoot != null) slot.paperRoot.SetActive(visible);
                if (slot.slotRoot != null) slot.slotRoot.SetActive(visible);
                if (!visible) continue;

                // 稿纸底图
                if (_paperBgSprite != null && slot.paperImage != null)
                {
                    slot.paperImage.sprite = _paperBgSprite;
                    slot.paperImage.color = Color.white;
                }

                var photoId = photoIds[i];
                var photoSprite = GetPhotoSprite(photoId);

                if (photoSprite != null)
                {
                    slot.photoImage.sprite = photoSprite;
                    slot.photoImage.color = Color.white;
                    slot.photoImage.preserveAspect = false;
                    if (slot.placeholderText != null) slot.placeholderText.gameObject.SetActive(false);
                }
                else
                {
                    slot.photoImage.color = new Color(0.73f, 0.59f, 0.38f, 1f);
                    if (slot.placeholderText != null) slot.placeholderText.gameObject.SetActive(true);
                }

                // 点击预览
                if (slot.photoButton != null)
                {
                    slot.photoButton.onClick.RemoveAllListeners();
                    var id = photoId;
                    slot.photoButton.onClick.AddListener(() =>
                    {
                        _localPreviewPhotoId = id;
                        ShowPreview(id, false);
                    });
                }
            }
        }

        private void RenderAlbumFromCode(List<string> photoIds, int count)
        {
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

        private void HideAllPrefabSlots()
        {
            if (_prefabSlots == null) return;
            foreach (var slot in _prefabSlots)
            {
                if (slot.paperRoot != null) slot.paperRoot.SetActive(false);
                if (slot.slotRoot != null) slot.slotRoot.SetActive(false);
            }
            if (_view != null && _view.AlbumTitleText != null)
                _view.AlbumTitleText.gameObject.SetActive(false);
        }

        /// <summary>隐藏预览面板（优先用 View 字段，回退到按名称查找）。</summary>
        private void HidePreviewRoot()
        {
            if (_view != null && _view.PreviewRoot != null)
            {
                _view.PreviewRoot.SetActive(false);
                return;
            }
            // 回退：按名称查找
            var t = transform.Find("PhotoPreview");
            if (t != null) t.gameObject.SetActive(false);
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
            _activePreviewPhotoId = photoId;

            // 优先使用 prefab 预览
            if (_usePrefab && _view?.PreviewRoot != null)
            {
                _preview = _view.PreviewRoot;
                _preview.SetActive(true);

                var prefabPreviewSprite = GetPhotoSprite(photoId);
                if (_view.PreviewPhotoImage != null)
                {
                    _view.PreviewPhotoImage.enabled = true;
                    if (prefabPreviewSprite != null)
                    {
                        _view.PreviewPhotoImage.sprite = prefabPreviewSprite;
                        _view.PreviewPhotoImage.color = Color.white;
                        _view.PreviewPhotoImage.preserveAspect = true;
                    }
                    else
                    {
                        _view.PreviewPhotoImage.color = new Color(0.62f, 0.48f, 0.3f);
                    }
                }

                if (_view.PreviewTitleText != null)
                    _view.PreviewTitleText.text = GetTitle(photoId);

                var isNotHost = isRewardPreview && NetworkSessionManager.Service?.Role != SessionRole.Host;
                if (_view.PreviewWaitingText != null)
                    _view.PreviewWaitingText.SetActive(isNotHost);

                if (_view.PreviewCloseButtonRoot != null)
                    _view.PreviewCloseButtonRoot.SetActive(!isNotHost);

                if (_view.PreviewCloseButton != null)
                {
                    _view.PreviewCloseButton.onClick.RemoveAllListeners();
                    var capturedId = photoId;
                    _view.PreviewCloseButton.onClick.AddListener(() =>
                    {
                        if (isRewardPreview)
                        {
                            _coordinator.Request(new GameplayIntent(GameplayIntentType.CloseMemoryPhotoPreview));
                        }
                        else
                        {
                            _localPreviewPhotoId = null;
                        }
                        DestroyPreview();
                    });
                }
                return;
            }

            // 代码生成回退
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
            var closeImage = close.GetComponent<Image>();
            if (_closeButtonSprite != null)
            {
                closeImage.sprite = _closeButtonSprite;
                closeImage.color = Color.white;
                closeImage.preserveAspect = true;
            }
            else
            {
                closeImage.color = new Color(0.55f, 0.18f, 0.15f, 1f);
            }
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

        private string GetTitle(string photoId) => photoId switch
        {
            "photo_hongqiang" => _titleHongqiang,
            "photo_hongfang" => _titleHongfang,
            "bagua_old_family_photo" => _titleOldFamilyPhoto,
            _ => photoId
        };

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
            if (_preview != null)
            {
                if (_usePrefab && _view != null && _preview == _view.PreviewRoot)
                    _preview.SetActive(false);
                else
                    Destroy(_preview);
            }
            _preview = null;
            _activePreviewPhotoId = null;
            // 确保 prefab 预览面板也被隐藏
            HidePreviewRoot();
        }

        private void CreateCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;
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

            // 全屏输入拦截层（飞行期间激活）
            _inputBlocker = new GameObject("InputBlocker", typeof(Image));
            _inputBlocker.transform.SetParent(transform, false);
            var blockerRect = _inputBlocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            var blockerImage = _inputBlocker.GetComponent<Image>();
            blockerImage.color = new Color(0, 0, 0, 0);
            blockerImage.raycastTarget = true;
            _inputBlocker.SetActive(false);
        }

        /// <summary>照片从屏幕中央飞回相册的弧线动画。</summary>
        private IEnumerator FlyPhotoToAlbum(string photoId, int targetIndex)
        {
            IsPhotoFlying = true;
            if (_inputBlocker != null) _inputBlocker.SetActive(true);

            var sprite = GetPhotoSprite(photoId);
            if (sprite == null)
            {
                Debug.Log($"[MemoryAlbum] 照片 sprite 为空 ({photoId})，跳过飞行动画");
                FinishFlight();
                yield break;
            }

            // 在相册 Canvas 上创建克隆照片
            var clone = new GameObject("FlyClone", typeof(Image));
            clone.transform.SetParent(_canvas.transform, false);
            var rt = clone.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(300, 300);
            rt.localScale = Vector3.one;
            var img = clone.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 计算目标格子位置（在 canvas 根的本地坐标系中）
            Vector2 targetAnchored;
            var slotTransform = _album.Find("PhotoSlot_" + targetIndex);
            if (slotTransform != null)
            {
                var slotRect = (RectTransform)slotTransform;
                Vector3[] corners = new Vector3[4];
                slotRect.GetWorldCorners(corners);
                var slotWorldCenter = (corners[0] + corners[2]) * 0.5f;
                var canvasLocal = _canvas.transform.InverseTransformPoint(slotWorldCenter);
                targetAnchored = new Vector2(canvasLocal.x, canvasLocal.y);
            }
            else
            {
                targetAnchored = new Vector2(-846 + targetIndex * 120, 436);
            }

            var startPos = Vector2.zero;
            var arcHeight = 200f;
            var duration = 0.7f;
            var elapsed = 0f;

            // 弧线飞行 + 旋转 + 缩小
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = UiTween.EaseOutCubic(t);

                var pos = Vector2.LerpUnclamped(startPos, targetAnchored, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                rt.anchoredPosition = pos;

                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-15f, 0f, eased));

                var s = Mathf.Lerp(1f, 0.3f, eased);
                rt.localScale = new Vector3(s, s, 1f);

                yield return null;
            }

            // 销毁克隆照片
            Destroy(clone);

            // 格子弹跳动画
            if (slotTransform != null)
            {
                var slotRect = (RectTransform)slotTransform;
                yield return UiTween.Scale(slotRect,
                    new Vector3(0.8f, 0.8f, 1f),
                    new Vector3(1f, 1f, 1f),
                    0.3f, UiTween.EaseOutBack);
            }

            FinishFlight();
        }

        /// <summary>飞行动画超时保护。</summary>
        private IEnumerator FlyPhotoTimeout(float timeout)
        {
            yield return new WaitForSecondsRealtime(timeout);
            if (IsPhotoFlying)
            {
                Debug.LogWarning($"[MemoryAlbum] 飞行动画超时 ({timeout}s)，强制完成");
                FinishFlight();
            }
        }

        private void FinishFlight()
        {
            IsPhotoFlying = false;
            if (_inputBlocker != null) _inputBlocker.SetActive(false);

            if (_coordinator == null)
            {
                Debug.LogWarning("[MemoryAlbum] FinishFlight: _coordinator 为空");
                return;
            }

            _coordinator.Request(new GameplayIntent(GameplayIntentType.FinishMiniGame));
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
