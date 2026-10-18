using UnityEngine;
using UnityEngine.InputSystem;
namespace DoHwa.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuBackgroundParallax : MonoBehaviour
    {
        [SerializeField] private float distance=12f;
        private RectTransform rect;
        private Vector2 origin,offset;
        private Vector3 originalScale;
        private void Awake(){TryGetComponent<RectTransform>(out rect);origin=rect.anchoredPosition;originalScale=rect.localScale;rect.localScale=originalScale*1.035f;}
        private void LateUpdate()
        {
            var target=Vector2.zero;
            if(Application.isFocused&&Mouse.current!=null){var p=Mouse.current.position.ReadValue();target=new Vector2(Mathf.Clamp(p.x/Screen.width*2-1,-1,1),Mathf.Clamp(p.y/Screen.height*2-1,-1,1))*distance;}
            offset=Vector2.Lerp(offset,target,1-Mathf.Exp(-5f*Time.unscaledDeltaTime));rect.anchoredPosition=origin+offset;
        }
        private void OnDisable(){rect.anchoredPosition=origin;rect.localScale=originalScale;}
    }
}
