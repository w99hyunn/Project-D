using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class JourneyStepView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text status;
        [SerializeField] private GameObject selection;
        [SerializeField] private GameObject completion;
        [SerializeField] private CanvasGroup content;
        public void Show(Sprite art, string label, bool done, bool current, bool selected)
        {
            icon.sprite = art;
            title.text = label;
            status.text = done ? "완료" : current ? "진행 중" : "다음 여정";
            selection.SetActive(selected);
            completion.SetActive(done);
            content.alpha = done || current || selected ? 1f : .72f;
        }
    }
}
