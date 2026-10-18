using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class InventorySortMenu : MonoBehaviour
    {
        private RpgUiController owner;
        private RectTransform anchor, panel;
        private GameObject overlay;
        private CanvasGroup group;
        private Button[] options;
        private TMP_Text[] labels;
        private float elapsed;
        private int selected;
        public bool IsOpen => overlay.activeSelf;
        private readonly string[] titles = { "기본순", "종류순", "이름순", "수량순" };

        public void Initialize(RpgUiController controller, RectTransform trigger)
        {
            owner = controller; anchor = trigger;
            trigger.TryGetComponent<UiModuleButton>(out var module);
            var theme = module.Theme;
            trigger.Find("Label").TryGetComponent<TMP_Text>(out var source);
            overlay = new GameObject("SortDropdown", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(transform, false);
            var full = (RectTransform)overlay.transform;
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            overlay.TryGetComponent<Image>(out var blocker); blocker.color = Color.clear;
            overlay.TryGetComponent<Button>(out var dismiss); dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(Hide);
            var body = new GameObject("Options", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            body.transform.SetParent(full, false); panel = (RectTransform)body.transform;
            panel.anchorMin = panel.anchorMax = Vector2.zero; panel.pivot = new Vector2(0, 0);
            panel.sizeDelta = new Vector2(trigger.rect.width, 208);
            body.TryGetComponent<Image>(out var background); background.sprite = theme.surface;
            background.type = Image.Type.Sliced; background.color = new Color(.97f, .97f, .98f);
            background.pixelsPerUnitMultiplier = theme.surface.border.x * background.canvas.referencePixelsPerUnit
                / (theme.surface.pixelsPerUnit * theme.cornerSize);
            body.TryGetComponent<CanvasGroup>(out group);
            options = new Button[titles.Length]; labels = new TMP_Text[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                int choice = i;
                var row = new GameObject(titles[i], typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(panel, false);
                var rect = (RectTransform)row.transform; rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(-16, 48); rect.anchoredPosition = new Vector2(0, -8 - i * 48);
                row.TryGetComponent<Button>(out options[i]); row.TryGetComponent<Image>(out var face);
                options[i].targetGraphic = face;
                var colors = options[i].colors; colors.normalColor = Color.clear;
                colors.highlightedColor = new Color(.26f, .32f, .40f, .12f);
                colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.26f, .32f, .40f, .23f);
                colors.fadeDuration = .1f; options[i].colors = colors;
                options[i].onClick.AddListener(() => Choose(choice));
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(rect, false); labelObject.TryGetComponent<TextMeshProUGUI>(out var text);
                text.font = source.font; text.fontSharedMaterial = source.fontSharedMaterial;
                text.fontSize = 21; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(18, 0); text.rectTransform.offsetMax = new Vector2(-18, 0);
                labels[i] = text;
            }
            overlay.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen) { Hide(); return; }
            overlay.SetActive(true); overlay.transform.SetAsLastSibling();
            var corners = new Vector3[4]; anchor.GetWorldCorners(corners);
            var point = ((RectTransform)overlay.transform).InverseTransformPoint(corners[1]);
            var bounds = ((RectTransform)overlay.transform).rect;
            panel.anchoredPosition = new Vector2(point.x - bounds.xMin, point.y - bounds.yMin + 10);
            elapsed = 0; group.alpha = 0;
            for (int i = 0; i < options.Length; i++)
            {
                labels[i].text = titles[i]; labels[i].color = i == selected ? Color.white : new Color(.20f, .24f, .29f);
                var colors = options[i].colors;
                colors.normalColor = i == selected ? new Color(.29f, .34f, .41f) : Color.clear;
                colors.highlightedColor = colors.selectedColor = i == selected ? new Color(.36f, .42f, .50f) : new Color(.26f, .32f, .40f, .12f);
                options[i].colors = colors;
            }
            EventSystem.current.SetSelectedGameObject(options[selected].gameObject);
        }
        private void Update()
        {
            if (!IsOpen) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / .16f);
            group.alpha = 1 - Mathf.Pow(1 - t, 3);
            panel.localScale = Vector3.one * Mathf.Lerp(.97f, 1, group.alpha);
        }
        private void Choose(int index) { selected = index; owner.SetSort(index); Hide(); }
        public void Hide() { overlay.SetActive(false); EventSystem.current.SetSelectedGameObject(anchor.gameObject); }
        private void OnDisable() { if (overlay) overlay.SetActive(false); }
    }
}
