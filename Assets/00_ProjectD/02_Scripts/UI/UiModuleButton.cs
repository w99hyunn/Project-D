using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UiModuleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private UiImageTheme theme;
        [SerializeField] private Image surface, frame, icon;
        [SerializeField] private Image iconMedallion;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool primary;
        private Button button;
        private bool hovered, pressed;
        private Material sheen;

        private Image sheenImage;
        private float hoverAmount, pressAmount, sweep=1;
        public UiImageTheme Theme => theme;

        public void Configure(UiImageTheme style, Image background, Image outline, Image symbol, TMP_Text text, bool primaryAction)
        {
            theme = style; surface = background; frame = outline; icon = symbol; label = text; primary = primaryAction;
            ApplyTheme();
        }

        public void SetIcon(Sprite sprite) { icon.sprite = sprite; icon.enabled = sprite; ApplyLayout(); }
        public void SetLabel(string value) => label.text = value;
        public void SetTheme(UiImageTheme style) { theme = style; ApplyTheme(); }

        [ContextMenu("Apply Theme")]
        public void ApplyTheme()
        {
            surface.sprite = primary ? theme.primarySurface : theme.secondarySurface;
            frame.enabled = false;
            surface.type = frame.type = Image.Type.Sliced;
            surface.fillCenter = frame.fillCenter = true;
            frame.transform.SetAsFirstSibling();
            surface.transform.SetSiblingIndex(1);
            surface.rectTransform.offsetMin = Vector2.one * 1.5f;
            surface.rectTransform.offsetMax = Vector2.one * -1.5f;
            surface.pixelsPerUnitMultiplier = surface.sprite.rect.height / surface.rectTransform.rect.height;
            surface.color = Color.white;
            icon.preserveAspect = true;
            label.color = primary ? theme.ink : theme.primaryInk;
            icon.color = new Color(.96f,.95f,.91f);
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            float diameter = Mathf.Min(((RectTransform)transform).rect.height * .58f, 32f);
            float sideInset = (((RectTransform)transform).rect.height - diameter) * .5f;
            var rt = icon.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = new Vector2(sideInset + diameter * .5f, 0);
            rt.sizeDelta = Vector2.one * (diameter * .65f);
            var disk = iconMedallion.rectTransform;
            disk.anchorMin = disk.anchorMax = new Vector2(0,.5f); disk.pivot = new Vector2(.5f,.5f);
            disk.anchoredPosition = rt.anchoredPosition; disk.sizeDelta = Vector2.one * diameter;
            var text = label.rectTransform;
            text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one;
            float inset = sideInset + (icon.enabled ? diameter + 10 : 0);
            text.offsetMin = new Vector2(inset, 0); text.offsetMax = new Vector2(-theme.iconInset, 0);
            label.alignment = TextAlignmentOptions.Center;
        }

        private void Awake()
        {
            TryGetComponent<Button>(out button); ApplyTheme();
            var overlay=new GameObject("InteractionSheen",typeof(RectTransform),typeof(Image));
            overlay.transform.SetParent(surface.transform,false);
            var rect=(RectTransform)overlay.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            overlay.TryGetComponent<Image>(out sheenImage);sheenImage.sprite=surface.sprite;sheenImage.type=surface.type;sheenImage.pixelsPerUnitMultiplier=surface.pixelsPerUnitMultiplier;sheenImage.raycastTarget=false;
            overlay.AddComponent<UiSurfaceCoordinates>();
            sheen=new Material(Resources.Load<Material>("UI/ButtonSheen"));sheenImage.material=sheen;
            sheen.SetColor("_Tint",primary?new Color(.94f,.98f,1):new Color(.45f,.52f,.61f));
            sheen.SetFloat("_Edge",primary?.13f:0);

        }
        private void Update()
        {
            bool enabledState = button.IsInteractable();
            bool focused = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
            bool active = hovered || focused;
            float blend = 1 - Mathf.Exp(-theme.transitionSpeed * Time.unscaledDeltaTime);
            hoverAmount=Mathf.Lerp(hoverAmount,enabledState && active?1:0,blend);
            pressAmount=Mathf.Lerp(pressAmount,enabledState && pressed?1:0,1-Mathf.Exp(-35*Time.unscaledDeltaTime));
            sweep=Mathf.Min(1,sweep+Time.unscaledDeltaTime/ .42f);
            sheen.SetFloat("_Phase",sweep);sheen.SetFloat("_Strength",hoverAmount*(1-pressAmount*.6f));
            iconMedallion.rectTransform.localScale=Vector3.one*(1+hoverAmount*.06f-pressAmount*.10f);
            Color face = !enabledState ? new Color(.84f,.85f,.87f,.65f) : pressed ? new Color(.80f,.83f,.87f) : active ? new Color(.94f,.97f,1f) : Color.white;
            Color edge = theme.accent; edge.a = !enabledState ? .12f : pressed ? .75f : active ? .55f : .3f;
            Color text = primary ? theme.ink : theme.primaryInk; text.a = enabledState ? 1 : .6f;
            Color symbol = new Color(.96f,.95f,.91f,enabledState ? 1 : .6f);
            surface.color = Color.Lerp(surface.color, face, blend);
            frame.color = Color.Lerp(frame.color, edge, blend);
            label.color = Color.Lerp(label.color, text, blend);
            icon.color = Color.Lerp(icon.color, symbol, blend);
        }
        public void OnPointerEnter(PointerEventData data) {hovered = true;sweep=0;}
        public void OnPointerExit(PointerEventData data) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData data) { if(data.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData data) => pressed = false;
        private void OnDisable() { hovered = false; pressed = false;hoverAmount=pressAmount=0;iconMedallion.rectTransform.localScale=Vector3.one; }
        private void OnDestroy(){Destroy(sheen);}
    }
}



