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
        [SerializeField] private float logoDuration=2f;
        [SerializeField] private float noticeDuration=3f;
        [SerializeField] private float fadeDuration=.45f;
        [SerializeField] private Animator animator;
        private bool loading;
        public async void OnSplashFinished()
        {
            if(loading)return;
            loading=true;
            try {await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync("Main"),destroyCancellationToken);}
            catch(OperationCanceledException){}
        }
    }
}

