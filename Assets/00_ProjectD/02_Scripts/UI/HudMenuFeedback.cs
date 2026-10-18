using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class HudMenuFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image border, keySurface, highlight;
        [SerializeField] private RectTransform glyph;
        [SerializeField] private TMP_Text keyLabel;
        private Button button;
        private Image icon;
        
        private Vector2 glyphRest, keyRest;
        private bool hovered, pressed, keyboardFocus;
        public void Configure(Image frame, Image key, RectTransform art) { border=frame; keySurface=key; glyph=art; }
        public void ConfigureHighlight(Image fill, TMP_Text label) { highlight=fill; keyLabel=label; }
        private void Awake()
        {
            TryGetComponent<Button>(out button);
            glyph.TryGetComponent<Image>(out icon);
            
            glyphRest=glyph.anchoredPosition;
            keyRest=keySurface.rectTransform.anchoredPosition;
        }
        private void OnEnable() => ResetVisuals();
        private void Update()
        {
            bool enabled=button.IsInteractable();
            bool down=enabled && pressed && hovered;
            bool over=enabled && (hovered || keyboardFocus);
            float blend=1-Mathf.Exp(-(down?42:24)*Time.unscaledDeltaTime);
            highlight.color=Color.Lerp(highlight.color,new Color(1,1,1,down?.16f:over?.48f:0),blend);
            icon.color=Color.Lerp(icon.color,!enabled?new Color(1,1,1,.4f):down?new Color(.78f,.82f,.87f):Color.white,blend);
            glyph.localScale=Vector3.Lerp(glyph.localScale,Vector3.one*(down?.92f:over?1.045f:1),blend);
            glyph.anchoredPosition=Vector2.Lerp(glyph.anchoredPosition,glyphRest+Vector2.up*(down?-1:over?2:0),blend);
            highlight.rectTransform.localScale=Vector3.Lerp(highlight.rectTransform.localScale,Vector3.one*(down?.94f:over?1.06f:1),blend);
            keySurface.color=Color.Lerp(keySurface.color,down?new Color(.77f,.83f,.91f):over?Color.white:new Color(.96f,.96f,.94f,.96f),blend);
        }
        public void OnPointerEnter(PointerEventData data) => hovered=true;
        public void OnPointerExit(PointerEventData data) { hovered=false; pressed=false; keyboardFocus=false; }
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left || !button.IsInteractable())return;
            hovered=true; pressed=true; keyboardFocus=false;
        }
        public void OnPointerUp(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left)pressed=false; }
        public void OnSelect(BaseEventData data) => keyboardFocus=!(data is PointerEventData);
        public void OnDeselect(BaseEventData data) => keyboardFocus=false;
        private void ResetVisuals()
        {
            hovered=pressed=keyboardFocus=false;
            border.color=highlight.color=Color.clear;
            icon.color=Color.white;
            
            glyph.localScale=Vector3.one;
            highlight.rectTransform.localScale=Vector3.one;
            keySurface.color=new Color(.96f,.96f,.94f,.96f);
            keyLabel.color=new Color(.22f,.24f,.28f);
            glyph.anchoredPosition=glyphRest;
            keySurface.rectTransform.anchoredPosition=keyRest;
        }
        private void OnDisable() => ResetVisuals();
    }
}


