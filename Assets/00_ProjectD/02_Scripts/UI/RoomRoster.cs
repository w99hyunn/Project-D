using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI
{
    public sealed class RoomRoster : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] names;
        [SerializeField] private TMP_Text[] roles;
        [SerializeField] private Animator[] slotAnimators;
        [SerializeField] private Button[] invites;
        [SerializeField] private TMP_Text count;
        public void Refresh(CSteamID lobby,ulong host)
        {
            int total=SteamMatchmaking.GetNumLobbyMembers(lobby);
            count.text=total+" / 4";
            for(int i=0;i<4;i++)
            {
                bool occupied=i<total;
                invites[i].interactable=!occupied;
                slotAnimators[i].SetBool("Occupied", occupied);
                if(occupied)
                {
                    var id=SteamMatchmaking.GetLobbyMemberByIndex(lobby,i);
                    names[i].text=SteamFriends.GetFriendPersonaName(id);
                    roles[i].text=id.m_SteamID==host?"방장":"참가자";
                }
                else { names[i].text="친구 초대";roles[i].text="빈자리"; }
            }
        }
    }
}
