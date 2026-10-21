using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class SettingsCategories : MonoBehaviour
    {
        [Serializable] private sealed class Category
        {
            public Button button;
            public Toggle selection;
            public GameObject[] content;
            public string title;
            public UiVisualState visual;
        }
        [SerializeField] private Category[] categories;
        [SerializeField] private UiPanelReveal reveal;
        [SerializeField] private TMPro.TMP_Text heading;
        private int current;
        private void Awake()
        {
            for(int i=0;i<categories.Length;i++)
            {
                int index=i;
                categories[i].button.onClick.AddListener(()=>Select(index));
            }
        }
        private void OnEnable()=>Show(current);
        private void OnDisable()
        {
            foreach(var category in categories)category.selection.SetIsOnWithoutNotify(false);
        }
        private void Select(int index)
        {
            TryGetComponent<DisplaySettingsPanel>(out var settings);
            settings.CancelPreview();
            bool changed=current!=index;
            current=index;
            Show(index);
            if(changed)reveal.RefreshContent(0);
        }
        private void Show(int index)
        {
            heading.text=categories[index].title;
            for(int i=0;i<categories.Length;i++)
            {
                categories[i].selection.SetIsOnWithoutNotify(i==index);
                categories[i].visual.SetActiveState(i==index);
                foreach(var item in categories[i].content)item.SetActive(i==index);
            }
        }
    }
}
