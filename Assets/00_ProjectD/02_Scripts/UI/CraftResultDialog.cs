using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class CraftResultDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image art;
        [SerializeField] private TMP_Text itemName, quantity;
        [SerializeField] private Button confirm;
        [SerializeField] private Animator animator;
        private GameObject returnFocus;
        private bool closing;
        public bool IsOpen => gameObject.activeSelf;
        private void Awake() => confirm.onClick.AddListener(Close);
        public void Show(Sprite sprite, string title, int amount)
        {
            returnFocus=EventSystem.current.currentSelectedGameObject;
            art.sprite=sprite; art.enabled=true; itemName.text=title; quantity.text=amount.ToString();
            closing=false; group.interactable=false; group.blocksRaycasts=true;
            gameObject.SetActive(true); transform.SetAsLastSibling();
            animator.Play("Open",0,0); animator.Update(0);
            EventSystem.current.SetSelectedGameObject(null);
        }
        public void OnOpened() { if(!closing) group.interactable=true; }
        public void Close()
        {
            if(closing || !IsOpen) return;
            closing=true; group.interactable=false;
            EventSystem.current.SetSelectedGameObject(null);
            animator.CrossFadeInFixedTime("Close",.06f);
        }
        public void OnClosed()
        {
            gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(returnFocus);
        }
    }
}
