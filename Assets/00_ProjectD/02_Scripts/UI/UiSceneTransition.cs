using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa.UI
{
    public sealed class UiSceneTransition : MonoBehaviour
    {
        public static async Awaitable LoadAsync(string scene)
        {
            var curtain = new GameObject("SceneTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(UnityEngine.UI.GraphicRaycaster), typeof(UiSceneTransition));
            DontDestroyOnLoad(curtain);
            curtain.TryGetComponent<Canvas>(out var canvas);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            curtain.TryGetComponent<CanvasGroup>(out var group);
            group.alpha = 0;
            var surface = new GameObject("Surface", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            surface.transform.SetParent(curtain.transform, false);
            var rect = (RectTransform)surface.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            surface.TryGetComponent<UnityEngine.UI.Image>(out var image);
            image.color = new Color(.035f, .045f, .065f, 1);
            curtain.TryGetComponent<UiSceneTransition>(out var lifetime);
            var token = lifetime.destroyCancellationToken;
            async Awaitable Fade(float from, float to, float duration)
            {
                float began = Time.unscaledTime;
                while (Time.unscaledTime - began < duration)
                {
                    float p = Mathf.Clamp01((Time.unscaledTime - began) / duration);
                    group.alpha = Mathf.Lerp(from, to, p * p * (3 - 2 * p));
                    await Awaitable.NextFrameAsync(token);
                }
                group.alpha = to;
            }
            try
            {
                await Fade(0, 1, .22f);
                await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(scene), token);
                await Awaitable.NextFrameAsync(token);
                await Fade(1, 0, .28f);
            }
            finally { Destroy(curtain); }
        }
    }
}
