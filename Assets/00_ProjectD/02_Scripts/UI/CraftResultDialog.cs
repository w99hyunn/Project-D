using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class CraftResultDialog : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform content;
        private Image art;
        private TMP_Text itemName, quantity;
        private Button confirm;
        private GameObject returnFocus;
        private float elapsed;
        public bool IsOpen => gameObject.activeSelf;

        public void Initialize(UiModuleButton template, TMP_Text typography)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var dim = gameObject.AddComponent<Image>(); dim.color = new Color(.07f, .09f, .14f, .82f);
            group = gameObject.AddComponent<CanvasGroup>();
            content = new GameObject("Result", typeof(RectTransform)).transform as RectTransform;
            content.SetParent(rect, false); content.sizeDelta = new Vector2(640, 480);
            TMP_Text Text(string name, string value, Vector2 pos, Vector2 size, float fontSize)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(content, false);
                go.TryGetComponent<TextMeshProUGUI>(out var text); text.font = typography.font; text.text = value;
                text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.color = new Color(.96f,.95f,.91f); text.raycastTarget = false;
                text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = pos; return text;
            }
            Text("Title", "제작 완료", new Vector2(0,180), new Vector2(600,60), 32);
            var card = new GameObject("Item", typeof(RectTransform), typeof(Image)); card.transform.SetParent(content, false);
            var cr = (RectTransform)card.transform; cr.sizeDelta = new Vector2(144,164); cr.anchoredPosition = new Vector2(0,35);
            card.TryGetComponent<Image>(out var surface); surface.sprite = template.Theme.surface; surface.type = Image.Type.Sliced;
            surface.pixelsPerUnitMultiplier = surface.sprite.border.x / 10; surface.raycastTarget = false;
            var icon = new GameObject("Art", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(cr,false);
            icon.TryGetComponent<Image>(out art); art.enabled=false; art.preserveAspect=true; art.raycastTarget=false;
            art.rectTransform.sizeDelta=new Vector2(100,100); art.rectTransform.anchoredPosition=new Vector2(0,12);
            quantity=Text("Quantity","",new Vector2(0,-24),new Vector2(120,32),22); quantity.color=template.Theme.ink;
            itemName=Text("ItemName","",new Vector2(0,-82),new Vector2(580,40),24);
            var button = Instantiate(template.gameObject,content,false); button.name="Confirm";
            var br=(RectTransform)button.transform; br.anchorMin=br.anchorMax=br.pivot=new Vector2(.5f,.5f);
            br.sizeDelta=new Vector2(280,56);br.anchoredPosition=new Vector2(0,-188);
            button.TryGetComponent<Button>(out confirm); confirm.onClick=new Button.ButtonClickedEvent();confirm.onClick.AddListener(Close);
            button.TryGetComponent<UiModuleButton>(out var module); module.SetLabel("확인");module.SetIcon(template.Theme.confirmIcon);
            confirm.interactable=true; gameObject.SetActive(false);
        }
        public void Show(Sprite sprite,string title,int amount)
        {
            returnFocus=EventSystem.current.currentSelectedGameObject;
            art.sprite=sprite;art.enabled=true;itemName.text=title;quantity.text=amount.ToString();
            elapsed=0;group.alpha=0;group.interactable=false;
            gameObject.SetActive(true);transform.SetAsLastSibling();EventSystem.current.SetSelectedGameObject(confirm.gameObject);
        }
        private void Update()
        {
            elapsed+=Time.unscaledDeltaTime;float t=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.24f),3);
            group.alpha=t;content.anchoredPosition=Vector2.down*(1-t)*16;group.interactable=elapsed>=.24f;
        }
        public void Close(){gameObject.SetActive(false);EventSystem.current.SetSelectedGameObject(returnFocus);}
    }
}
