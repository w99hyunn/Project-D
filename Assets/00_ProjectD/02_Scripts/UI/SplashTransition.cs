using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa.UI
{
    public sealed class SplashTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logo;
        [SerializeField] private CanvasGroup ratingIcons;
        [SerializeField] private CanvasGroup ratingNotice;
        [SerializeField] private float logoDuration = 2f;
        [SerializeField] private float noticeDuration = 3f;
        [SerializeField] private float fadeDuration = 0.45f;

        private async void Start()
        {
            var cancellationToken = destroyCancellationToken;
            try
            {
                var logoGroups = new[] { logo };
                var ratingGroups = new[] { ratingIcons, ratingNotice };

                await HoldAsync(1f, cancellationToken);
                await FadeAsync(logoGroups, 0f, 1f, cancellationToken);
                await HoldAsync(logoDuration, cancellationToken);
                await FadeAsync(logoGroups, 1f, 0f, cancellationToken);

                await HoldAsync(1f, cancellationToken);
                ratingIcons.gameObject.SetActive(true);
                ratingNotice.gameObject.SetActive(true);
                await FadeAsync(ratingGroups, 0f, 1f, cancellationToken);
                await HoldAsync(noticeDuration, cancellationToken);
                await FadeAsync(ratingGroups, 1f, 0f, cancellationToken);
                await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync("Login"), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Leaving the scene cancels the pending transition.
            }
        }

        private static async Awaitable HoldAsync(float duration, System.Threading.CancellationToken cancellationToken)
        {
            var startedAt = Time.unscaledTime;
            while (Time.unscaledTime - startedAt < duration)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        private async Awaitable FadeAsync(CanvasGroup[] groups, float from, float to, System.Threading.CancellationToken cancellationToken)
        {
            var startedAt = Time.unscaledTime;
            while (Time.unscaledTime - startedAt < fadeDuration)
            {
                var alpha = Mathf.Lerp(from, to, Mathf.Clamp01((Time.unscaledTime - startedAt) / fadeDuration));
                foreach (var group in groups)
                    group.alpha = alpha;
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            foreach (var group in groups)
                group.alpha = to;
        }
    }
}
