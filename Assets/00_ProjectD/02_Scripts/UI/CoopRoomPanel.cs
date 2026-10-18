using DoHwa.World;
using TMPro;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class CoopRoomPanel : MonoBehaviour
    {
        [SerializeField] private CoopSession session;
        [SerializeField] private TMP_Text status;
        [SerializeField] private UnityEngine.UI.Button host;
        [SerializeField] private UnityEngine.UI.Button invite;
        [SerializeField] private UnityEngine.UI.Button friends;
        [SerializeField] private UnityEngine.UI.Button leave;
        [SerializeField] private GameObject settingsPanel;
        public void ShowRoom() { settingsPanel.SetActive(false); gameObject.SetActive(true); }
        public void ShowSettings() { gameObject.SetActive(false); settingsPanel.SetActive(true); }
        private void OnEnable() { session.Changed += Refresh; Refresh(); }
        private void OnDisable() { session.Changed -= Refresh; }
        private void Refresh()
        {
            status.text = session.Status;
            host.gameObject.SetActive(false);
            invite.interactable = !session.Busy && session.InLobby;
            friends.interactable = SteamRuntime.Ready;
            leave.interactable = !session.Busy && session.InLobby;
        }
        public async void Host() { await session.Host(); }
        public void InviteFriends() => session.InviteFriends();
        public void OpenFriends() => session.OpenFriends();
        public void Leave() => session.Leave();
    }
}
