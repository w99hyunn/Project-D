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
        private int memberCount;
        private bool presencePending;

        private void OnEnable() => presencePending=true;

        private void LateUpdate()
        {
            if(!presencePending)return;
            for(int i=0;i<slotAnimators.Length;i++)
            {
                // Inactive panels have no initialized Animator playback state.
                // Apply the latest roster after Unity initializes the enabled slots.
                if(!slotAnimators[i].isInitialized)return;
                slotAnimators[i].SetBool("Occupied",i<memberCount);
            }
            presencePending=false;
        }
        public void Refresh(CSteamID lobby,ulong host)
        {
            int total=SteamMatchmaking.GetNumLobbyMembers(lobby);
            if(memberCount!=total){memberCount=total;presencePending=true;}
            count.text=total+" / 4";
            for(int i=0;i<4;i++)
            {
                bool occupied=i<total;
                invites[i].interactable=!occupied;
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
