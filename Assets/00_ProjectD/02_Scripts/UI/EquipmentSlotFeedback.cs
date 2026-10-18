using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DoHwa.UI
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public sealed class EquipmentSlotFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ICanvasRaycastFilter
    {
        private Material material;
        private bool hover, pressed;
        private float focus, press, selection;
        private bool selected;
        private float resultPulse;
        public void Pulse() => resultPulse=1;
        private RectTransform art; private Vector2 rest; private bool occupied; private float phase;
        private void Awake()=>Initialize();
        private void Initialize(){if(material)return;TryGetComponent<Image>(out var image);material=new Material(image.material);image.material=material;art=(RectTransform)transform.Find("Art");rest=art.anchoredPosition;phase=transform.GetSiblingIndex()*.73f;}
        public void SetState(bool selected,bool occupied){Initialize();this.selected=selected;this.occupied=occupied;material.SetFloat("_Occupied",occupied?1:0);}
        private void Update()
        {
            float t=1-Mathf.Exp(-18*Time.unscaledDeltaTime);
            focus=Mathf.Lerp(focus,hover?1:0,t);
            press=Mathf.Lerp(press,pressed?1:0,t);
            selection=Mathf.Lerp(selection,selected?1:0,t);
            resultPulse=Mathf.MoveTowards(resultPulse,0,Time.unscaledDeltaTime*1.7f);
            material.SetFloat("_Focus",Mathf.Max(focus,Mathf.Sin(resultPulse*Mathf.PI)));
            material.SetFloat("_Press",press);
            material.SetFloat("_Selected",selection);
            // Menu animation must keep running while gameplay time is paused.
            material.SetFloat("_EffectTime",Time.unscaledTime+phase);
            art.anchoredPosition=rest+Vector2.up*((occupied?Mathf.Sin(Time.unscaledTime*1.25f+phase)*3:0)-press*2);
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screenPoint,eventCamera,out var point))return false;
            var offset=point-rect.rect.center;
            return offset.sqrMagnitude<=Mathf.Pow(Mathf.Min(rect.rect.width,rect.rect.height)*.42f,2);
        }
        public void OnPointerEnter(PointerEventData e)=>hover=true;
        public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e)=>pressed=false;
        private void OnDisable(){hover=pressed=false;focus=press=selection=0;}
        private void OnDestroy(){Destroy(material);}
    }
}


