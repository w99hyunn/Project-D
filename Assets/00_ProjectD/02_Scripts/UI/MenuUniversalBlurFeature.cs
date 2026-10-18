using Unified.UniversalBlur.Runtime;
using UnityEngine.Rendering.Universal;
namespace DoHwa.UI
{
    public sealed class MenuUniversalBlurFeature : UniversalBlurFeature
    {
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(!UiBackdropBlur.IsVisible || !renderingData.cameraData.camera.CompareTag("MainCamera"))return;
            base.AddRenderPasses(renderer,ref renderingData);
        }
    }
}
