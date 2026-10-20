using System;
using System.Collections.Generic;
using DoHwa.World;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Transform slots;
        [SerializeField] private WorldSaveRow rowPrefab;
        [SerializeField] private TMP_Text selectedTitle;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text members;
        [SerializeField] private RoomRoster roster;
        [SerializeField] private TMP_Text empty;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text createStatus;
        [SerializeField] private TMP_InputField worldName;
        [SerializeField] private TMP_InputField seed;
        [SerializeField] private GameObject createPanel;
        [SerializeField] private Button start;
        [SerializeField] private Button host;
        [SerializeField] private Button invite;
        [SerializeField] private Button leave;
        [SerializeField] private GameObject contextMenu;
        [SerializeField] private RectTransform contextPanel;
        [SerializeField] private GameObject deletePanel;
        [SerializeField] private TMP_Text deleteMessage;
        [SerializeField] private TMP_Text editorTitle;
        [SerializeField] private TMP_Text editorSubmit;
        [SerializeField] private GameObject seedLabel;
        private WorldSlot contextSlot;
        private string editingSlot;
        [SerializeField] private GameObject roomProgress;
        [SerializeField] private TMP_Text roomProgressTitle;
        [SerializeField] private TMP_Text roomProgressMessage;
        [SerializeField] private GameObject roomProgressConfirm;
        [SerializeField] private UiPanelReveal roomProgressReveal;
        public bool IsBusy=>busy;
        public void DismissRoomProgress(){if(!busy)roomProgress.SetActive(false);}
        private readonly List<WorldSaveRow> rows=new();
        private CSteamID lobby;
        private ulong hostId;
        private bool ownLobby;
        private bool busy;
        private bool handoff;
        public void BeginSelected(){if(!InLobby)HostRoom();else StartGame();}
        private float nextPoll;
        private WorldSlot selected;
        public bool InLobby=>lobby.IsValid();
        private void Start()
        {
            RefreshSaves();SteamRuntime.JoinRequested+=AcceptInvite;
            if(SteamRuntime.PendingLobby.IsValid())AcceptInvite(SteamRuntime.PendingLobby);
            else UpdateState();
        }
        private void RefreshSaves()
        {
            foreach(var row in rows)Destroy(row.gameObject);rows.Clear();selected=null;
            var saves=WorldSelection.List();
            foreach(var slot in saves){var row=Instantiate(rowPrefab,slots);row.gameObject.SetActive(true);row.Bind(slot,Select,ShowContext);rows.Add(row);if(slot.valid&&(selected==null||slot.id==WorldSelection.Slot))selected=slot;}
            empty.gameObject.SetActive(saves.Count==0);
            if(selected!=null)Select(selected);else {selectedTitle.text="새로운 여정을 시작하세요";UpdateState();}
        }
        private void Select(WorldSlot slot)
        {
            if(busy || (InLobby&&!ownLobby))return;
            selected=slot;WorldSelection.Slot=slot.id;selectedTitle.text=slot.title;
            if(!InLobby)status.text="마지막 저장  "+DateTimeOffset.FromUnixTimeSeconds(slot.savedAt).ToLocalTime().ToString("yyyy.MM.dd  HH:mm")+"\n시드  "+slot.seed;
            foreach(var row in rows)row.Select(row.Slot==slot.id);
            if(InLobby)SteamMatchmaking.SetLobbyData(lobby,"world",slot.title);
            UpdateState();
        }
        public void ShowCreate()
        {
            if(busy||InLobby)return;
            editingSlot=null;editorTitle.text="새로운 세계";editorSubmit.text="세계 만들기";
            worldName.text="";seed.text="";seed.gameObject.SetActive(true);seedLabel.SetActive(true);
            createStatus.text="";createPanel.SetActive(true);
        }
        private void ShowContext(WorldSlot slot,Vector2 position)
        {
            if(busy||InLobby)return;
            contextSlot=slot;contextMenu.SetActive(true);contextMenu.transform.SetAsLastSibling();
            var parent=(RectTransform)contextPanel.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,position,null,out var point);
            var bounds=parent.rect;point.x=Mathf.Clamp(point.x,bounds.xMin,bounds.xMax-contextPanel.rect.width);
            point.y=Mathf.Clamp(point.y,bounds.yMin+contextPanel.rect.height,bounds.yMax);
            contextPanel.anchoredPosition=point;
        }
        public void CloseContext()=>contextMenu.SetActive(false);
        public bool CloseSavePopup()
        {
            if(roomProgress.activeSelf){DismissRoomProgress();return true;}
            if(contextMenu.activeSelf){CloseContext();return true;}
            if(deletePanel.activeSelf){CancelDelete();return true;}
            return false;
        }
        public void EditContextWorld()
        {
            CloseContext();if(!contextSlot.valid)return;
            editingSlot=contextSlot.id;editorTitle.text="월드 편집";editorSubmit.text="저장";
            worldName.text=contextSlot.title;seed.gameObject.SetActive(false);seedLabel.SetActive(false);
            createStatus.text="";createPanel.SetActive(true);
        }
        public void AskDeleteWorld()
        {
            CloseContext();deleteMessage.text="〈"+contextSlot.title+"〉 월드를 삭제할까요?\n삭제한 저장 파일은 복구할 수 없습니다.";
            deletePanel.SetActive(true);
        }
        public void CancelDelete()=>deletePanel.SetActive(false);
        public void DeleteContextWorld()
        {
            if(busy||InLobby)return;
            try{WorldSelection.Delete(contextSlot.id);deletePanel.SetActive(false);RefreshSaves();}
            catch(Exception e){deleteMessage.text=e.Message;}
        }
        public bool IsCreateOpen=>createPanel.activeSelf;
        public void CancelCreate()=>createPanel.SetActive(false);
        public void CreateWorld()
        {
            try {if(editingSlot==null)WorldSelection.Create(worldName.text,seed.text);else WorldSelection.Rename(editingSlot,worldName.text);createPanel.SetActive(false);RefreshSaves();status.text="";}
            catch(Exception e){createStatus.text=e.Message;}
        }
        private void UpdateState()
        {
            start.interactable=!busy&&selected!=null&&(!InLobby||ownLobby)&&SteamRuntime.Ready;
            start.TryGetComponent<UiVisualState>(out var startState);
            startState.SetActiveState(InLobby);
            host.gameObject.SetActive(false);
            members.gameObject.SetActive(false);
            invite.interactable=!busy&&InLobby&&SteamRuntime.Ready;
            leave.interactable=!busy&&InLobby;
            startLabel.text=busy?"준비 중":InLobby?(ownLobby?"게임 시작":"방장의 시작을 기다리는 중"):"방 생성";
            foreach(var row in rows){row.TryGetComponent<Button>(out var b);b.interactable=!busy&&(!InLobby||ownLobby)&&row.Valid;}
        }
        public async void HostRoom()
        {
            if(busy||InLobby||selected==null||!SteamRuntime.Ready)return;
            var cancellation=destroyCancellationToken;
            busy=true;UpdateState();
            roomProgressTitle.text="방 생성 중";roomProgressMessage.text="Steam에 방을 만들고 있습니다.";
            roomProgressConfirm.SetActive(false);roomProgress.SetActive(true);roomProgress.transform.SetAsLastSibling();
            try
            {
                bool done=false,failed=false;LobbyCreated_t result=default;
                using var call=CallResult<LobbyCreated_t>.Create((value,io)=>{result=value;failed=io;done=true;});
                call.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly,4));
                float deadline=Time.realtimeSinceStartup+20;
                while(!done&&Time.realtimeSinceStartup<deadline)await Awaitable.NextFrameAsync(cancellation);
                if(!done||failed||result.m_eResult!=EResult.k_EResultOK)throw new InvalidOperationException("Steam 방을 열지 못했습니다");
                lobby=new CSteamID(result.m_ulSteamIDLobby);ownLobby=true;hostId=SteamUser.GetSteamID().m_SteamID;
                SteamMatchmaking.SetLobbyData(lobby,"game",SteamRuntime.Settings.lobbyNamespace);SteamMatchmaking.SetLobbyData(lobby,"protocol",SteamRuntime.Settings.protocol);
                SteamMatchmaking.SetLobbyData(lobby,"host",hostId.ToString());SteamMatchmaking.SetLobbyData(lobby,"ready","0");SteamMatchmaking.SetLobbyData(lobby,"world",selected.title);
                SteamFriends.SetRichPresence("connect","+connect_lobby "+lobby.m_SteamID);status.text="친구를 초대하거나 바로 시작하세요";
                roomProgressMessage.text="방이 준비되었습니다.";
                await roomProgressReveal.HideAsync();
                roomProgress.SetActive(false);
            }
            catch(OperationCanceledException){}
            catch(Exception e){LeaveLobby();roomProgressTitle.text="방 생성 실패";roomProgressMessage.text=e.Message;roomProgressConfirm.SetActive(true);}
            finally{busy=false;if(!cancellation.IsCancellationRequested)UpdateState();}
        }
        private async void AcceptInvite(CSteamID target)
        {
            SteamRuntime.ClearPending();if(busy||lobby==target)return;
            LeaveLobby();busy=true;UpdateState();status.text="친구의 방에 참가하는 중";
            try
            {
                bool done=false,failed=false;LobbyEnter_t result=default;
                using var call=CallResult<LobbyEnter_t>.Create((value,io)=>{result=value;failed=io;done=true;});call.Set(SteamMatchmaking.JoinLobby(target));
                float deadline=Time.realtimeSinceStartup+20;
                while(!done&&Time.realtimeSinceStartup<deadline)await Awaitable.NextFrameAsync(destroyCancellationToken);
                if(!done||failed||result.m_EChatRoomEnterResponse!=(uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)throw new InvalidOperationException("참가할 수 없는 방입니다");
                lobby=target;
                if(SteamMatchmaking.GetLobbyData(lobby,"game")!=SteamRuntime.Settings.lobbyNamespace||SteamMatchmaking.GetLobbyData(lobby,"protocol")!=SteamRuntime.Settings.protocol||!ulong.TryParse(SteamMatchmaking.GetLobbyData(lobby,"host"),out hostId))throw new InvalidOperationException("호환되는 도화정 방이 아닙니다");
                ownLobby=false;selectedTitle.text=SteamMatchmaking.GetLobbyData(lobby,"world");status.text="친구의 월드에 연결하는 중";
            }
            catch(OperationCanceledException){}
            catch(Exception e){LeaveLobby();status.text=e.Message;}
            finally{busy=false;UpdateState();}
        }
        private void Update()
        {
            if(!InLobby||busy||Time.unscaledTime<nextPoll)return;nextPoll=Time.unscaledTime+.5f;
            if(SteamMatchmaking.GetLobbyOwner(lobby).m_SteamID!=hostId){LeaveLobby();status.text="방장이 방을 종료했습니다";return;}
            roster.Refresh(lobby,hostId);
            if(!ownLobby&&SteamMatchmaking.GetLobbyData(lobby,"ready")=="1")StartGame();
        }
        public void InviteFriends(){if(InLobby)SteamFriends.ActivateGameOverlayInviteDialog(lobby);}
        public void Friends(){if(SteamRuntime.Ready)SteamFriends.ActivateGameOverlay("Friends");else status.text="Steam에 로그인해 주세요";}
        public void LeaveLobby()
        {
            if(InLobby&&SteamRuntime.Ready){if(ownLobby)SteamMatchmaking.SetLobbyJoinable(lobby,false);SteamMatchmaking.LeaveLobby(lobby);SteamFriends.ClearRichPresence();}
            lobby=default;ownLobby=false;members.text="";if(selected!=null)Select(selected);UpdateState();
        }
        public async void StartGame()
        {
            if(busy)return;
            if(InLobby&&!ownLobby&&SteamMatchmaking.GetLobbyData(lobby,"ready")!="1")return;
            if(!InLobby){HostRoom();return;}
            busy=true;UpdateState();status.text="월드를 불러오는 중";
            try {if(selected!=null)WorldSelection.Slot=selected.id;WorldSelection.Lobby=lobby.m_SteamID;WorldSelection.HostLobby=ownLobby;handoff=true;await UiSceneTransition.LoadAsync("Ingame");}
            catch(OperationCanceledException){}
            catch(Exception e){handoff=false;busy=false;status.text=e.Message;UpdateState();}
        }
        public void Quit()=>Application.Quit();
        private void OnDestroy(){SteamRuntime.JoinRequested-=AcceptInvite;if(!handoff&&InLobby&&SteamRuntime.Ready){SteamMatchmaking.LeaveLobby(lobby);SteamFriends.ClearRichPresence();}}
    }
}



