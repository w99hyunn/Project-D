using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DoHwa.UI
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public sealed class EquipmentSlotFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ICanvasRaycastFilter
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float focus, press, selection, resultPulse;
        private Material material;
        private float phase;
        private bool selected,occupied,pendingPulse;
        public void Pulse(){if(isActiveAndEnabled)animator.SetTrigger("ItemChanged");else pendingPulse=true;}
        private void Awake() => Initialize();
        private void OnEnable(){animator.Update(0);SyncState();if(pendingPulse){animator.SetTrigger("ItemChanged");pendingPulse=false;}}
        private void Initialize()
        {
            if(material) return;
            TryGetComponent<Image>(out var image);
            material=new Material(image.material); image.material=material;
            phase=transform.GetSiblingIndex()*.73f;
        }
        public void SetState(bool selected,bool occupied)
        {
            Initialize();
            this.selected=selected;this.occupied=occupied;
            material.SetFloat("_Occupied",occupied?1:0);
            if(isActiveAndEnabled)SyncState();
        }
        private void SyncState()
        {
            animator.SetBool("EquippedSelected",selected);
            animator.SetBool("Occupied",occupied);
        }
        // Animator owns all envelopes. This bridge supplies per-instance shader values and an unscaled clock.
        private void LateUpdate()
        {
            material.SetFloat("_Focus",Mathf.Max(focus,resultPulse));
            material.SetFloat("_Press",press);
            material.SetFloat("_Selected",selection);
            material.SetFloat("_EffectTime",Time.unscaledTime+phase);
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screenPoint,eventCamera,out var point))return false;
            return (point-rect.rect.center).sqrMagnitude<=Mathf.Pow(Mathf.Min(rect.rect.width,rect.rect.height)*.42f,2);
        }
        public void OnPointerEnter(PointerEventData e)=>animator.SetBool("SlotHovered",true);
        public void OnPointerExit(PointerEventData e){animator.SetBool("SlotHovered",false);animator.SetBool("SlotPressed",false);}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)animator.SetBool("SlotPressed",true);}
        public void OnPointerUp(PointerEventData e)=>animator.SetBool("SlotPressed",false);
        private void OnDisable(){focus=press=selection=resultPulse=0;}
        private void OnDestroy()=>Destroy(material);
    }
}
