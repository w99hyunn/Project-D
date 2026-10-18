using TMPro;
using UnityEngine;
namespace DoHwa.UI
{
    public sealed class ButtonActionIcon : MonoBehaviour
    {
        [SerializeField] private UiModuleButton module;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Sprite confirm, cancel, back, sort, equip, use, exit, bag, craft;
        private string previous;
        public void Configure(UiModuleButton target, TMP_Text text, Sprite[] sprites) { module=target;label=text;confirm=sprites[0];cancel=sprites[1];back=sprites[2];sort=sprites[3];equip=sprites[4];use=sprites[5];exit=sprites[6];bag=sprites[7];craft=sprites[8];Refresh(); }
        private void LateUpdate() { if(previous!=label.text)Refresh(); }
        private void Refresh()
        {
            previous=label.text;
            var sprite=previous.Contains("정렬")?sort:previous.Contains("취소")?cancel:previous.Contains("돌아")?back:previous.Contains("종료")?exit:previous.Contains("장착")?equip:previous.Contains("사용")?use:previous.Contains("배낭")?bag:previous.Contains("제작")||previous.Contains("재료")?craft:confirm;
            module.SetIcon(sprite);
        }
    }
}
