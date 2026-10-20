using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI
{
    public sealed class UiBackdropBlur : MonoBehaviour
    {
        [SerializeField] private RawImage backdrop;
        [SerializeField] private Animator animator;
        public static bool IsVisible {get;private set;}
        private bool shown;
        public async Awaitable PrepareAsync()
        {
            if(IsVisible)return;
            IsVisible=true;
            try
            {
                // Let the renderer allocate its targets before the first menu opens.
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            finally { IsVisible=shown; }
        }
        public async Awaitable CaptureAsync()
        {
            IsVisible=true;shown=true;
            backdrop.texture=Texture2D.whiteTexture;backdrop.enabled=true;
            animator.CrossFadeInFixedTime("Visible",.18f);
            await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
        public void Hide(){shown=false;animator.CrossFadeInFixedTime("Hidden",.12f);}
        public void OnHidden(){if(shown)return;IsVisible=false;backdrop.enabled=false;}
        private void OnDisable(){shown=false;IsVisible=false;backdrop.enabled=false;}
    }
}
