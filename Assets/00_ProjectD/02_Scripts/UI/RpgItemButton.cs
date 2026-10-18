using TMPro;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class RpgItemButton : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        [SerializeField] private RpgUiController owner;
        [SerializeField] private bool quick;
        [SerializeField] private int index;
        [SerializeField] private UnityEngine.UI.Image art, border;
        [SerializeField] private TMP_Text amount, equipped;
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private Sprite normalFrame, selectedFrame;
        [SerializeField] private bool unframed;
        [SerializeField] private UnityEngine.UI.Image selectionMarker;
        private bool hovered;
        private float resultPulse;
        public void PulseItem(int id) { if(id>=0 && index==id) resultPulse=1; }
        private bool chosen, pressed;
        private Vector2 artPosition;
        [SerializeField] private UnityEngine.UI.Image cardFace, quantityBand, equippedBadge;
        public void ConfigureCard(UnityEngine.UI.Image face, UnityEngine.UI.Image band, UnityEngine.UI.Image badge) { cardFace=face; quantityBand=band; equippedBadge=badge; }

        private void Awake() => artPosition=art.rectTransform.anchoredPosition;
        private Color SurfaceColor() => chosen ? new Color(.68f,.82f,.94f) : hovered ? new Color(.89f,.94f,.98f) : Color.white;
        private void Update()
        {
            float blend=1-Mathf.Exp(-24*Time.unscaledDeltaTime);
            resultPulse=Mathf.MoveTowards(resultPulse,0,Time.unscaledDeltaTime*1.6f);
            Color target=Color.Lerp(SurfaceColor(),Color.white,Mathf.Sin(resultPulse*Mathf.PI)*.8f);
            if(pressed && button.IsInteractable()) target*=new Color(.90f,.93f,.97f,1);
            border.color=Color.Lerp(border.color,target,blend);
            if(!quick) {
                Color face=chosen?new Color(.59f,.76f,.90f):hovered?new Color(.86f,.90f,.95f):new Color(.82f,.86f,.92f);
                face=Color.Lerp(face,Color.white,Mathf.Sin(resultPulse*Mathf.PI)*.7f);
                cardFace.color=Color.Lerp(cardFace.color,face,blend);
                quantityBand.color=Color.Lerp(quantityBand.color,chosen?new Color(.90f,.95f,.99f):new Color(.98f,.98f,.97f),blend);
            }
            var offset=pressed && button.IsInteractable()?Vector2.down*2:Vector2.zero;
            art.rectTransform.anchoredPosition=Vector2.Lerp(art.rectTransform.anchoredPosition,artPosition+offset,blend);
        }

        public void ConfigureSkin(Sprite normal, Sprite selected) { normalFrame=normal; selectedFrame=selected; }
        public void ConfigureUnframed(bool value) => unframed=value;
        public void ConfigureSelectionMarker(UnityEngine.UI.Image value) => selectionMarker=value;

        public void Configure(RpgUiController controller,bool quickSlot,UnityEngine.UI.Image icon,UnityEngine.UI.Image outline,TMP_Text quantity,TMP_Text status,UnityEngine.UI.Button target)
        { owner=controller; quick=quickSlot; art=icon; border=outline; amount=quantity; equipped=status; button=target; }

        public void Show(int id,Sprite sprite,int quantity,bool selected,bool worn,bool showEmpty=false)
        {
            index=id; art.sprite=sprite; art.enabled=sprite; amount.text=id>=0 && (!quick || quantity>1)?quantity.ToString():"";
            border.enabled=!unframed || selected;
            chosen=selected;
            selectionMarker.enabled=false;
            if(normalFrame) { border.sprite=normalFrame; }
            else border.color=selected?new Color(.97f,.96f,.89f):new Color(.75f,.82f,.86f,.10f);
            if(quick) {
                transform.Find("Key").TryGetComponent<TMP_Text>(out var key);
                key.color=amount.color=new Color(.22f,.25f,.28f);
            }
            if(!quick) {
                gameObject.SetActive(id>=0 || showEmpty);
                cardFace.enabled=quantityBand.enabled=id>=0;
                equippedBadge.enabled=worn;
            }
            equipped.gameObject.SetActive(quick && worn);
            equipped.text=worn?"장착":""; button.interactable=id>=0 && (!quick || quantity>0);
            if(quick && hovered) owner.ShowQuickTooltip(index);
        }
        public void Click() { if(quick) owner.UseQuick(index); else owner.Select(index); }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) { hovered=true; if(quick) owner.ShowQuickTooltip(index); }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) { hovered=pressed=false; if(quick) owner.HideQuickTooltip(); }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData data) { if(data.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left) pressed=true; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData data) => pressed=false;
        private void OnDisable() { hovered=pressed=false; art.rectTransform.anchoredPosition=artPosition; }
    }
}



