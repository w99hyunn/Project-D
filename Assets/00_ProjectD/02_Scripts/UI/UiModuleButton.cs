using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UiModuleButton : MonoBehaviour
    {
        [SerializeField] private UiImageTheme theme;
        [SerializeField] private Image surface, frame, icon;
        [SerializeField] private Image iconMedallion;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool primary;
        [SerializeField] private bool authoredMotion;






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

    }
}

