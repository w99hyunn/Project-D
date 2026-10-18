using System;
using DoHwa.World;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class WorldSaveRow : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private Image selected;
        [SerializeField] private Button button;
        private WorldSlot data;
        private Action<WorldSlot,Vector2> context;
        public string Slot { get; private set; }
        public bool Valid { get; private set; }
        public void Bind(WorldSlot slot, Action<WorldSlot> choose, Action<WorldSlot,Vector2> showContext)
        {
            data=slot;context=showContext;Slot=slot.id;Valid=slot.valid;title.text=slot.title;detail.text=slot.valid?DateTimeOffset.FromUnixTimeSeconds(slot.savedAt).ToLocalTime().ToString("yyyy.MM.dd  HH:mm"):"저장 파일을 읽을 수 없습니다";
            button.interactable=slot.valid;button.onClick.AddListener(()=>choose(slot));
        }
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Right)context(data,e.position);
        }
        public void Select(bool value) => selected.enabled=value;
    }
}

