using TMPro;
using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldAreaLabel : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Bounds area;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private string areaTitle;
        [SerializeField] private string areaSubtitle;
        private string originalTitle;
        private string originalSubtitle;
        private bool inside;

        private void Start() { originalTitle = title.text; originalSubtitle = subtitle.text; }
        private void LateUpdate()
        {
            bool next = area.Contains(player.position);
            if (inside == next) return;
            inside = next;
            title.text = inside ? areaTitle : originalTitle;
            subtitle.text = inside ? areaSubtitle : originalSubtitle;
        }
    }
}
