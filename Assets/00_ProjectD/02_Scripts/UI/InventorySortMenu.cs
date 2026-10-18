using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class InventorySortMenu : MonoBehaviour
    {
        [SerializeField] private RpgUiController owner;
        [SerializeField] private RectTransform anchor, panel;
        [SerializeField] private GameObject overlay;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button[] options;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Animator animator;
        private int selected;
        public bool IsOpen => overlay.activeSelf;
        private void Awake()
        {
            overlay.TryGetComponent<Button>(out var dismiss); dismiss.onClick.AddListener(Hide);
            for(int i=0;i<options.Length;i++) { int choice=i; options[i].onClick.AddListener(()=>Choose(choice)); }
        }
        public void Toggle()
        {
            if(IsOpen) { Hide(); return; }
            overlay.SetActive(true); overlay.transform.SetAsLastSibling();
            var corners=new Vector3[4]; anchor.GetWorldCorners(corners);
            var point=((RectTransform)overlay.transform).InverseTransformPoint(corners[1]);
            var bounds=((RectTransform)overlay.transform).rect;
            panel.anchoredPosition=new Vector2(point.x-bounds.xMin,point.y-bounds.yMin+10);
            for(int i=0;i<options.Length;i++)
            {
                labels[i].color=i==selected?Color.white:new Color(.20f,.24f,.29f);
                var colors=options[i].colors;
                colors.normalColor=i==selected?new Color(.29f,.34f,.41f):Color.clear;
                colors.highlightedColor=colors.selectedColor=i==selected?new Color(.36f,.42f,.50f):new Color(.26f,.32f,.40f,.12f);
                options[i].colors=colors;
            }
            animator.Play("Open",0,0); animator.Update(0);
            EventSystem.current.SetSelectedGameObject(options[selected].gameObject);
        }
        private void Choose(int index) { selected=index; owner.SetSort(index); Hide(); }
        public void Hide() { overlay.SetActive(false); EventSystem.current.SetSelectedGameObject(anchor.gameObject); }
        private void OnDisable() => overlay.SetActive(false);
    }
}
