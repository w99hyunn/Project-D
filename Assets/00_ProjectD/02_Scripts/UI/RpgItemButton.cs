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
        [SerializeField] private Animator animator;
        private bool pendingPulse;
        public void PulseItem(int id) { if(id>=0 && index==id) { if(isActiveAndEnabled)animator.SetTrigger("ItemChanged");else pendingPulse=true; } }
        private bool chosen, pressed;
        private bool hasContent, displayedWorn, displayedEmpty;
        private int displayedQuantity;

        [SerializeField] private UnityEngine.UI.Image cardFace, quantityBand, equippedBadge;
        public void ConfigureCard(UnityEngine.UI.Image face, UnityEngine.UI.Image band, UnityEngine.UI.Image badge) { cardFace=face; quantityBand=band; equippedBadge=badge; }


        private void OnEnable()
        {
            SyncAnimation();
            if(pendingPulse){animator.SetTrigger("ItemChanged");pendingPulse=false;}
        }
        private void SyncAnimation()
        {
            if(!isActiveAndEnabled)return;
            animator.SetBool("ItemSelected",chosen);
            animator.SetBool("ItemHovered",hovered && button.IsInteractable());
            animator.SetBool("ItemPressed",pressed && button.IsInteractable());
        }

        public void ConfigureSkin(Sprite normal, Sprite selected) { normalFrame=normal; selectedFrame=selected; }
        public void ConfigureUnframed(bool value) => unframed=value;
        public void ConfigureSelectionMarker(UnityEngine.UI.Image value) => selectionMarker=value;

        public void Configure(RpgUiController controller,bool quickSlot,UnityEngine.UI.Image icon,UnityEngine.UI.Image outline,TMP_Text quantity,TMP_Text status,UnityEngine.UI.Button target)
        { owner=controller; quick=quickSlot; art=icon; border=outline; amount=quantity; equipped=status; button=target; }

        public void Show(int id,Sprite sprite,int quantity,bool selected,bool worn,bool showEmpty=false)
        {
            if(hasContent && index==id && art.sprite==sprite && displayedQuantity==quantity &&
               chosen==selected && displayedWorn==worn && displayedEmpty==showEmpty)return;
            hasContent=true;displayedQuantity=quantity;displayedWorn=worn;displayedEmpty=showEmpty;
            if(index!=id)pendingPulse=false;
            if(index!=id && animator.isActiveAndEnabled)
            {
                animator.ResetTrigger("ItemChanged");
                animator.Play("Idle",animator.GetLayerIndex("Result emphasis"),0);
            }
            index=id; art.sprite=sprite; art.enabled=sprite; amount.text=id>=0 && (!quick || quantity>1)?quantity.ToString():"";
            border.enabled=!unframed || selected;
            chosen=selected;
            selectionMarker.enabled=false;
            if(!quick) {
                gameObject.SetActive(id>=0 || showEmpty);
                cardFace.enabled=quantityBand.enabled=id>=0;
                equippedBadge.enabled=worn;
            }
            equipped.gameObject.SetActive(quick && worn);
            equipped.text=worn?"장착":""; button.interactable=id>=0 && (!quick || quantity>0);
            SyncAnimation();
            if(quick && hovered) owner.ShowQuickTooltip(index);
        }
        public void Click() { if(quick) owner.UseQuick(index); else owner.Select(index); }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) { hovered=true; SyncAnimation(); if(quick) owner.ShowQuickTooltip(index); }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) { hovered=pressed=false; SyncAnimation(); if(quick) owner.HideQuickTooltip(); }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData data) { if(data.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left) pressed=true; SyncAnimation(); }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData data) { pressed=false; SyncAnimation(); }
        private void OnDisable() { hovered=pressed=false; }
    }
}



