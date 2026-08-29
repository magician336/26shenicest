using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotForgetMe.UI
{
    /// <summary>
    /// 轻量级 UI 缓动库。不依赖 DOTween，使用协程驱动。
    /// 支持常见缓动函数 + 链式序列。
    /// </summary>
    public static class UiTween
    {
        // ==============================
        // 缓动函数
        // ==============================

        public delegate float EaseFn(float t);

        public static readonly EaseFn Linear = t => t;

        public static readonly EaseFn EaseOutQuad = t => 1f - (1f - t) * (1f - t);

        public static readonly EaseFn EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

        public static readonly EaseFn EaseInCubic = t => t * t * t;

        public static readonly EaseFn EaseOutQuart = t => 1f - Mathf.Pow(1f - t, 4f);

        public static readonly EaseFn EaseInOutCubic = t =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        /// <summary>轻微回弹，适合按钮出现、卡片入场。</summary>
        public static readonly EaseFn EaseOutBack = t =>
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        };

        /// <summary>弹性回弹，适合放大效果。</summary>
        public static readonly EaseFn EaseOutElastic = t =>
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        };

        /// <summary>弹跳落地，适合物品掉落。</summary>
        public static readonly EaseFn EaseOutBounce = t =>
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        };

        public static readonly EaseFn EaseInBack = t =>
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        };

        // ==============================
        // 基础 Tween 协程
        // ==============================

        /// <summary>通用缓动：在 duration 内用 ease 函数从 from 到 to 插值，每帧调用 setter。</summary>
        public static IEnumerator Animate(float duration, EaseFn ease,
            System.Func<float> getter, System.Action<float> setter)
        {
            var from = getter();
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                setter(Mathf.LerpUnclamped(from, 1f, ease(t)));
                yield return null;
            }
            setter(from + (1f - from) * 0f + (1f - 0f) * ease(1f));
        }

        /// <summary>Scale 缓动。</summary>
        public static IEnumerator Scale(RectTransform target, Vector3 from, Vector3 to,
            float duration, EaseFn ease = null)
        {
            ease ??= EaseOutBack;
            var elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, ease(t));
                yield return null;
            }
            if (target != null) target.localScale = to;
        }

        /// <summary>AnchoredPosition 缓动。</summary>
        public static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to,
            float duration, EaseFn ease = null)
        {
            ease ??= EaseOutCubic;
            var elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t));
                yield return null;
            }
            if (target != null) target.anchoredPosition = to;
        }

        /// <summary>Graphic Alpha 缓动（支持 Image 和 Text）。</summary>
        public static IEnumerator FadeAlpha(Graphic graphic, float from, float to,
            float duration, EaseFn ease = null)
        {
            ease ??= Linear;
            var elapsed = 0f;
            while (elapsed < duration && graphic != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var c = graphic.color;
                c.a = Mathf.LerpUnclamped(from, to, ease(t));
                graphic.color = c;
                yield return null;
            }
            if (graphic != null)
            {
                var c = graphic.color;
                c.a = to;
                graphic.color = c;
            }
        }

        /// <summary>Image 颜色缓动。</summary>
        public static IEnumerator FadeColor(Image img, Color from, Color to,
            float duration, EaseFn ease = null)
        {
            ease ??= Linear;
            var elapsed = 0f;
            while (elapsed < duration && img != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                img.color = Color.LerpUnclamped(from, to, ease(t));
                yield return null;
            }
            if (img != null) img.color = to;
        }

        /// <summary>序列执行多个协程。</summary>
        public static IEnumerator Sequence(params IEnumerator[] coroutines)
        {
            foreach (var c in coroutines)
                yield return c;
        }

        /// <summary>并行执行多个协程。</summary>
        public static IEnumerator Parallel(MonoBehaviour host, params IEnumerator[] coroutines)
        {
            var routines = new Coroutine[coroutines.Length];
            for (var i = 0; i < coroutines.Length; i++)
                routines[i] = host.StartCoroutine(coroutines[i]);
            foreach (var r in routines)
                yield return r;
        }

        /// <summary>等待秒数（不受 Time.timeScale 影响）。</summary>
        public static IEnumerator Wait(float seconds)
        {
            yield return new UnityEngine.WaitForSecondsRealtime(seconds);
        }
    }
}
