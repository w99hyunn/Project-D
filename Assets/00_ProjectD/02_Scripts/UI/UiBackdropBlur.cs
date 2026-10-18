using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI
{
    public sealed class UiBackdropBlur : MonoBehaviour
    {
        [SerializeField] private RawImage backdrop;
        public static bool IsVisible { get; private set; }
        private bool shown;
        private float opacity;
        public async Awaitable CaptureAsync()
        {
            IsVisible=true;
            shown=true;
            backdrop.texture=Texture2D.whiteTexture;
            backdrop.enabled=true;
            await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
        private void Update()
        {
            opacity=Mathf.MoveTowards(opacity,shown?1:0,Time.unscaledDeltaTime/(shown?.18f:.12f));
            var color=backdrop.color;color.a=opacity;backdrop.color=color;
            if(!shown && opacity<=0){IsVisible=false;backdrop.enabled=false;}
        }
        public void Hide() => shown=false;
        private void OnDisable(){shown=false;opacity=0;IsVisible=false;backdrop.enabled=false;}
    }
}
