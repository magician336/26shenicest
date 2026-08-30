using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.UI
{
    /// <summary>
    /// 纯代码 UI 动效系统。使用 Image 模拟高质量粒子，
    /// 配合 UiTween 缓动函数实现丝滑过渡。
    /// </summary>
    public static class UiFx
    {
        // ==============================
        // Sprite 缓存
        // ==============================

        private static Sprite _softCircle;
        private static Sprite _starRay;

        /// <summary>柔和渐变圆（中心实→边缘透明），用于蒸汽和发光。</summary>
        public static Sprite GetSoftCircleSprite()
        {
            if (_softCircle != null) return _softCircle;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var center = new Vector2(size / 2f, size / 2f);
            var radius = size / 2f;
            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                    // 柔和衰减：1 - dist^2，避免硬边
                    var alpha = Mathf.Clamp01(1f - dist * dist);
                    alpha = alpha * alpha; // 二次衰减更自然
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            _softCircle = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            return _softCircle;
        }

        /// <summary>星形光芒（细长菱形），用于光线爆发。</summary>
        public static Sprite GetStarRaySprite()
        {
            if (_starRay != null) return _starRay;
            const int w = 16;
            const int h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var cx = w / 2f;
            var cy = h / 2f;
            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var dx = Mathf.Abs(x - cx) / cx;
                    var dy = Mathf.Abs(y - cy) / cy;
                    // 菱形：dx + dy < 1
                    var diamond = Mathf.Clamp01(1f - (dx + dy));
                    diamond = Mathf.Pow(diamond, 1.5f);
                    tex.SetPixel(x, y, new Color(1, 0.95f, 0.7f, diamond));
                }
            }
            tex.Apply();
            _starRay = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), h);
            return _starRay;
        }

        // ==============================
        // 缩小消失 + 旋转
        // ==============================

        public static IEnumerator ShrinkOut(RectTransform target, float duration,
            System.Action onComplete = null)
        {
            if (target == null) yield break;
            var startScale = target.localScale;
            var startRot = target.localRotation;
            var img = target.GetComponent<Image>();
            var startAlpha = img != null ? img.color.a : 1f;
            var elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = UiTween.EaseInCubic(t);
                target.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, eased);
                target.localRotation = startRot * Quaternion.Euler(0, 0, 360f * t);
                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Lerp(startAlpha, 0f, t);
                    img.color = c;
                }
                yield return null;
            }
            onComplete?.Invoke();
        }

        // ==============================
        // 蒸汽粒子（柔和上飘 + 左右摇摆 + 渐变淡出）
        // ==============================

        public static IEnumerator SteamBurst(RectTransform parent,
            Vector2 anchoredPosition, int count = 10)
        {
            if (parent == null) yield break;
            var sprite = GetSoftCircleSprite();
            var particles = new List<RectTransform>();
            var datas = new List<SteamData>();

            for (var i = 0; i < count; i++)
            {
                var go = new GameObject("Steam", typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                var offsetX = Random.Range(-50f, 50f);
                var offsetY = Random.Range(-15f, 25f);
                rt.anchoredPosition = anchoredPosition + new Vector2(offsetX, offsetY);
                var startSize = Random.Range(40f, 70f);
                rt.sizeDelta = new Vector2(startSize, startSize);
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.color = new Color(0.92f, 0.92f, 0.98f, 0.55f);
                img.raycastTarget = false;

                particles.Add(rt);
                datas.Add(new SteamData
                {
                    startX = offsetX,
                    startY = offsetY,
                    swayAmp = Random.Range(20f, 45f),
                    swayFreq = Random.Range(1.5f, 3f),
                    riseSpeed = Random.Range(100f, 180f),
                    startSize = startSize,
                    endSize = startSize * Random.Range(1.6f, 2.2f),
                    startAlpha = Random.Range(0.4f, 0.6f),
                    delay = Random.Range(0f, 0.15f),
                    lifetime = Random.Range(0.9f, 1.3f)
                });
            }

            var maxLife = 1.5f;
            var elapsed = 0f;
            while (elapsed < maxLife)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = particles.Count - 1; i >= 0; i--)
                {
                    if (particles[i] == null) { particles.RemoveAt(i); continue; }
                    var d = datas[i];
                    var localT = elapsed - d.delay;
                    if (localT < 0) continue;
                    var lifeT = Mathf.Clamp01(localT / d.lifetime);
                    if (lifeT >= 1f)
                    {
                        Object.Destroy(particles[i].gameObject);
                        particles.RemoveAt(i);
                        continue;
                    }

                    // 上升 + 左右摇摆
                    var y = anchoredPosition.y + d.startY + d.riseSpeed * localT;
                    var x = anchoredPosition.x + d.startX + Mathf.Sin(localT * d.swayFreq) * d.swayAmp;
                    particles[i].anchoredPosition = new Vector2(x, y);

                    // 放大
                    var s = Mathf.Lerp(d.startSize, d.endSize, lifeT);
                    particles[i].sizeDelta = new Vector2(s, s);

                    // 淡出（前30%保持，后70%渐隐）
                    var alphaT = lifeT < 0.3f
                        ? lifeT / 0.3f
                        : 1f - (lifeT - 0.3f) / 0.7f;
                    var img = particles[i].GetComponent<Image>();
                    if (img != null)
                    {
                        var c = img.color;
                        c.a = d.startAlpha * alphaT;
                        img.color = c;
                    }
                }
                yield return null;
            }

            for (var i = particles.Count - 1; i >= 0; i--)
                if (particles[i] != null) Object.Destroy(particles[i].gameObject);
        }

        private struct SteamData
        {
            public float startX, startY;
            public float swayAmp, swayFreq;
            public float riseSpeed;
            public float startSize, endSize;
            public float startAlpha;
            public float delay, lifetime;
        }

        // ==============================
        // 光芒爆发（中心闪光 + 径向星形射线 + 旋转）
        // ==============================

        public static IEnumerator LightBurst(RectTransform parent,
            Vector2 anchoredPosition, int rayCount = 12)
        {
            if (parent == null) yield break;
            var softSprite = GetSoftCircleSprite();
            var raySprite = GetStarRaySprite();
            var elements = new List<(RectTransform rt, float angle, float speed, float delay)>();

            // 中心闪光
            var flashGo = new GameObject("CenterFlash", typeof(Image));
            flashGo.transform.SetParent(parent, false);
            var flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = flashRt.anchorMax = new Vector2(0.5f, 0.5f);
            flashRt.anchoredPosition = anchoredPosition;
            flashRt.sizeDelta = new Vector2(120, 120);
            var flashImg = flashGo.GetComponent<Image>();
            flashImg.sprite = softSprite;
            flashImg.color = new Color(1f, 0.95f, 0.7f, 0.9f);
            flashImg.raycastTarget = false;

            // 径向射线
            for (var i = 0; i < rayCount; i++)
            {
                var angle = (360f / rayCount) * i + Random.Range(-8f, 8f);
                var speed = Random.Range(220f, 380f);
                var go = new GameObject("Ray", typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = new Vector2(24, 80);
                rt.localRotation = Quaternion.Euler(0, 0, angle - 90);
                var img = go.GetComponent<Image>();
                img.sprite = raySprite;
                img.color = new Color(1f, 0.88f, 0.5f, 0.85f);
                img.raycastTarget = false;
                elements.Add((rt, angle, speed, Random.Range(0f, 0.08f)));
            }

            var duration = 0.8f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                // 中心闪光：先放大后缩小 + 淡出
                var flashT = Mathf.Clamp01(elapsed / 0.35f);
                var flashScale = Mathf.Lerp(0.3f, 1.6f, UiTween.EaseOutBack(flashT));
                if (elapsed > 0.35f)
                {
                    var fadeT = (elapsed - 0.35f) / 0.45f;
                    flashScale = Mathf.Lerp(1.6f, 0.8f, fadeT);
                    var fc = flashImg.color;
                    fc.a = Mathf.Lerp(0.9f, 0f, fadeT);
                    flashImg.color = fc;
                }
                else
                {
                    var fc = flashImg.color;
                    fc.a = Mathf.Lerp(0.3f, 0.9f, flashT);
                    flashImg.color = fc;
                }
                flashRt.localScale = Vector3.one * flashScale;

                // 射线：飞散 + 旋转 + 淡出
                foreach (var (rt, angle, speed, delay) in elements)
                {
                    if (rt == null) continue;
                    var localT = Mathf.Max(0, elapsed - delay);
                    var dir = new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad));
                    rt.anchoredPosition = anchoredPosition + dir * speed * localT;
                    rt.localRotation = Quaternion.Euler(0, 0, angle - 90 + localT * 120f);

                    var s = Mathf.Lerp(80f, 30f, localT / duration);
                    rt.sizeDelta = new Vector2(24, s);

                    var img = rt.GetComponent<Image>();
                    if (img != null)
                    {
                        var c = img.color;
                        c.a = Mathf.Lerp(0.85f, 0f, localT / (duration - delay));
                        img.color = c;
                    }
                }

                yield return null;
            }

            if (flashGo != null) Object.Destroy(flashGo);
            foreach (var (rt, _, _, _) in elements)
                if (rt != null) Object.Destroy(rt.gameObject);
        }

        // ==============================
        // 卡片翻转（Y轴 scale 1→0→1，中间切换内容可见性）
        // ==============================

        public static IEnumerator CardFlip(RectTransform target, float duration = 0.6f,
            System.Action onMidpoint = null)
        {
            if (target == null) yield break;
            var originalScale = target.localScale.x;
            var halfDur = duration * 0.5f;

            // 前半段：scaleX 1→0，平滑加速
            var elapsed = 0f;
            while (elapsed < halfDur)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / halfDur);
                var eased = UiTween.EaseInCubic(t);
                target.localScale = new Vector3(
                    originalScale * (1f - eased),
                    target.localScale.y,
                    target.localScale.z);
                yield return null;
            }

            // 中点回调
            onMidpoint?.Invoke();
            target.localScale = new Vector3(0f, target.localScale.y, target.localScale.z);

            // 后半段：scaleX 0→1
            elapsed = 0f;
            while (elapsed < halfDur)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / halfDur);
                var eased = UiTween.EaseOutBack(t);
                target.localScale = new Vector3(
                    originalScale * eased,
                    target.localScale.y,
                    target.localScale.z);
                yield return null;
            }
            target.localScale = new Vector3(originalScale, target.localScale.y, target.localScale.z);
        }

        // ==============================
        // 脉冲呼吸光（持续 Alpha + Scale 脉动，加色叠加）
        // ==============================

        public static IEnumerator PulseGlow(Image target)
        {
            if (target == null) yield break;
            var baseColor = target.color;
            var phase = 0f;
            while (target != null && target.gameObject != null)
            {
                phase += Time.unscaledDeltaTime;
                var pulse = (Mathf.Sin(phase * 2.5f) + 1f) * 0.5f;

                var c = baseColor;
                c.a = Mathf.Lerp(baseColor.a * 0.3f, baseColor.a, pulse);
                target.color = c;

                var scale = 1f + pulse * 0.06f;
                target.rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        // ==============================
        // 弹出出现（从0放大到1，带回弹）
        // ==============================

        public static IEnumerator PopIn(RectTransform target, float duration = 0.35f)
        {
            if (target == null) yield break;
            var originalScale = target.localScale;
            target.localScale = Vector3.zero;
            yield return UiTween.Scale(target, Vector3.zero, originalScale, duration, UiTween.EaseOutBack);
        }
    }
}
