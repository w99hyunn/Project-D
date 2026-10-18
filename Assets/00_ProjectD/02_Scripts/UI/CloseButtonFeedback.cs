using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DoHwa.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class CloseButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image circle;
        private Button button;
        private bool hovered, pressed, focused;
        public void Configure(Image image) => circle = image;
        private void Awake() => TryGetComponent<Button>(out button);
        private void Update()
        {
            float target = !button.IsInteractable() ? .35f : 1;
            float alpha = Mathf.Lerp(circle.color.a, target, 1 - Mathf.Exp(-(pressed ? 38 : 24) * Time.unscaledDeltaTime));
            var face = pressed && hovered ? new Color(.70f,.73f,.77f,alpha) : hovered || focused ? new Color(1,1,1,alpha) : new Color(.90f,.89f,.85f,alpha);
            circle.color = Color.Lerp(circle.color, face, 1 - Mathf.Exp(-26 * Time.unscaledDeltaTime));
            float scale=pressed&&hovered?.90f:hovered||focused?1.09f:1;
            circle.rectTransform.localScale=Vector3.Lerp(circle.rectTransform.localScale,Vector3.one*scale,1-Mathf.Exp(-24*Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { if(e.button == PointerEventData.InputButton.Left && button.IsInteractable()) pressed = true; }
        public void OnPointerUp(PointerEventData e) { if(e.button == PointerEventData.InputButton.Left) pressed = false; }
        public void OnSelect(BaseEventData e) => focused = !(e is PointerEventData);
        public void OnDeselect(BaseEventData e) => focused = false;
        private void OnDisable() { hovered = pressed = focused = false; circle.color = new Color(.90f,.89f,.85f,1);circle.rectTransform.localScale=Vector3.one; }
    }
}

