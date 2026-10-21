using DoHwa.World;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class CoopRoomPanel : MonoBehaviour
    {
        [SerializeField] private CoopSession session;
        [SerializeField] private Button invite;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject[] profiles;
        [SerializeField] private TMP_Text[] names, roles, initials;
        [SerializeField] private RawImage[] avatars;
        private readonly ulong[] displayed = new ulong[4];
        private readonly Texture2D[] textures = new Texture2D[4];
        private Callback<AvatarImageLoaded_t> avatarLoaded;
        private Callback<PersonaStateChange_t> personaChanged;
        public void ShowRoom() { settingsPanel.SetActive(false); gameObject.SetActive(true); }
        public void ShowSettings() { gameObject.SetActive(false); settingsPanel.SetActive(true); }
        private void OnEnable()
        {
            session.Changed += Refresh;
            if(SteamRuntime.Ready)
            {
                avatarLoaded=Callback<AvatarImageLoaded_t>.Create(e=>RefreshAvatar(e.m_steamID));
                personaChanged=Callback<PersonaStateChange_t>.Create(e=>Refresh());
            }
            Refresh();
        }
        private void OnDisable()
        {
            session.Changed -= Refresh;
            avatarLoaded?.Dispose(); personaChanged?.Dispose();
            avatarLoaded=null;personaChanged=null;
        }
        private void Refresh()
        {
            bool connected=SteamRuntime.Ready && session.InLobby;
            int total=connected?Mathf.Min(4,SteamMatchmaking.GetNumLobbyMembers(session.Lobby)):1;
            var owner=connected?SteamMatchmaking.GetLobbyOwner(session.Lobby):default;
            for(int i=0;i<profiles.Length;i++)
            {
                bool occupied=i<total;profiles[i].SetActive(occupied);
                if(!occupied)continue;
                var id=connected?SteamMatchmaking.GetLobbyMemberByIndex(session.Lobby,i):SteamRuntime.Ready?SteamUser.GetSteamID():default;
                string name=SteamRuntime.Ready?SteamFriends.GetFriendPersonaName(id):"나";
                names[i].text=name;roles[i].text=connected?(id==owner?"방장":"참가자"):"나";
                initials[i].text=string.IsNullOrEmpty(name)?"?":System.Globalization.StringInfo.GetNextTextElement(name);
                if(displayed[i]!=id.m_SteamID || !textures[i]){displayed[i]=id.m_SteamID;LoadAvatar(i,id);}
            }
            invite.interactable=connected&&!session.Busy&&total<4;
        }
        private void RefreshAvatar(CSteamID id)
        {
            for(int i=0;i<displayed.Length;i++)if(displayed[i]==id.m_SteamID)LoadAvatar(i,id);
        }
        private void LoadAvatar(int index,CSteamID id)
        {
            if(textures[index])Destroy(textures[index]);textures[index]=null;avatars[index].texture=null;avatars[index].enabled=false;
            if(!SteamRuntime.Ready||!id.IsValid())return;
            int handle=SteamFriends.GetMediumFriendAvatar(id);
            if(handle<=0||!SteamUtils.GetImageSize(handle,out uint width,out uint height))return;
            var pixels=new byte[width*height*4];
            if(!SteamUtils.GetImageRGBA(handle,pixels,pixels.Length))return;
            var texture=new Texture2D((int)width,(int)height,TextureFormat.RGBA32,false);
            texture.LoadRawTextureData(pixels);texture.Apply(false,true);textures[index]=texture;
            avatars[index].texture=texture;avatars[index].uvRect=new Rect(0,1,1,-1);avatars[index].enabled=true;
        }
        private void OnDestroy(){foreach(var texture in textures)if(texture)Destroy(texture);}
        public void InviteFriends()=>session.InviteFriends();
    }
}
