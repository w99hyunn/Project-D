using TMPro;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class WardrobeItemView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private EquipmentSlotFeedback slotFeedback;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text state;
        public void Bind(string label, Sprite sprite, bool selected, bool equipped, System.Action choose)
        {
            title.text = label;
            icon.sprite = sprite;
            icon.enabled = sprite;
            state.text = selected ? "선택됨" : equipped ? "착용 중" : "미리 입기";
            slotFeedback.SetState(selected, sprite);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => choose());
        }
    }
}
