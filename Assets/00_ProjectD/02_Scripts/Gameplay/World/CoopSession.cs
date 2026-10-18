using System;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace DoHwa.World
{
    public sealed class CoopSession : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [SerializeField] private SteamSocketTransport transport;
        [SerializeField] private WorldRuntime world;
        public CSteamID Lobby { get; private set; }
        public bool InLobby => Lobby.IsValid();
        public bool Busy { get; private set; }
        public string Status { get; private set; } = "접속 준비 중";
        public event Action Changed;
        private Callback<LobbyChatUpdate_t> members;
        private bool leaving;
        private bool joined;
        private float connectionDeadline;
        private async void Start()
        {
            transport.AcceptPeer = IsMember;
            SteamRuntime.JoinRequested += InviteAccepted;
            network.OnClientDisconnectCallback += Disconnected;
            network.OnClientConnectedCallback += Connected;
            if (!SteamRuntime.Ready) { SetStatus("Steam을 실행하고 로그인해 주세요"); return; }
            members = Callback<LobbyChatUpdate_t>.Create(MembersChanged);
            if (WorldSelection.Lobby != 0) { AdoptMenuLobby(); return; }
            if (SteamRuntime.PendingLobby.IsValid()) InviteAccepted(SteamRuntime.PendingLobby);
            else await Host();
        }
        private void AdoptMenuLobby()
        {
            Lobby = new CSteamID(WorldSelection.Lobby);
            bool host = WorldSelection.HostLobby;
            WorldSelection.Lobby = 0;
            network.NetworkConfig.NetworkTransport = transport;
            if (host)
            {
                if (!network.StartHost()) { Close(); SetStatus("리슨 서버 시작 실패"); return; }
                SteamMatchmaking.SetLobbyData(Lobby, "ready", "1");
                SteamMatchmaking.SetLobbyJoinable(Lobby, true);
            }
            else
            {
                if (!ulong.TryParse(SteamMatchmaking.GetLobbyData(Lobby, "host"), out var hostId) || SteamMatchmaking.GetLobbyOwner(Lobby).m_SteamID != hostId)
                { Close(); SetStatus("방장이 방을 종료했습니다"); return; }
                transport.HostSteamId = hostId;
                connectionDeadline = Time.realtimeSinceStartup + 20;
                if (!network.StartClient()) { Close(); SetStatus("Steam 서버 연결 실패"); }
            }
        }
        public bool IsMember(ulong steamId)
        {
            if (!InLobby || !SteamRuntime.Ready) return false;
            for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(Lobby); i++)
                if (SteamMatchmaking.GetLobbyMemberByIndex(Lobby, i).m_SteamID == steamId) return true;
            return false;
        }
        private void SetStatus(string value) { Status = value; Changed?.Invoke(); }
        private bool CanConnect()
        {
            if (!SteamRuntime.Ready) { SetStatus("Steam을 실행하고 로그인해 주세요"); return false; }
            return !Busy && !InLobby && !network.IsListening && !network.ShutdownInProgress;
        }
        public async Awaitable Host()
        {
            if (!CanConnect()) return;
            Busy = true; SetStatus("Steam 방을 여는 중");
            try
            {
                bool done = false; bool failed = false; LobbyCreated_t result = default;
                using var call = CallResult<LobbyCreated_t>.Create((value, io) => { result = value; failed = io; done = true; });
                call.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4));
                float deadline = Time.realtimeSinceStartup + 20;
                while (!done && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync(destroyCancellationToken);
                if (!done || failed || result.m_eResult != EResult.k_EResultOK) throw new InvalidOperationException("Steam 로비 생성 실패");
                Lobby = new CSteamID(result.m_ulSteamIDLobby);
                SteamMatchmaking.SetLobbyData(Lobby, "game", SteamRuntime.Settings.lobbyNamespace);
                SteamMatchmaking.SetLobbyData(Lobby, "protocol", SteamRuntime.Settings.protocol);
                SteamMatchmaking.SetLobbyData(Lobby, "host", SteamUser.GetSteamID().ToString());
                SteamMatchmaking.SetLobbyData(Lobby, "name", SteamFriends.GetPersonaName()+"의 도화정");
                network.NetworkConfig.NetworkTransport = transport;
                if (!network.StartHost()) throw new InvalidOperationException("리슨 서버 시작 실패");
                SteamMatchmaking.SetLobbyData(Lobby, "ready", "1");
                SteamMatchmaking.SetLobbyJoinable(Lobby, true);
                SteamFriends.SetRichPresence("connect", "+connect_lobby " + Lobby.m_SteamID);
                SetStatus("친구를 초대할 수 있습니다 · 최대 4명");
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Close(); SetStatus(error.Message); }
            finally { Busy = false; Changed?.Invoke(); }
        }
        private async void InviteAccepted(CSteamID lobby)
        {
            try
            {
                SteamRuntime.ClearPending();
                if (Busy || Lobby == lobby) return;
                if (InLobby || network.IsListening)
                {
                    Close();
                    while (network.ShutdownInProgress) await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                SteamRuntime.SetPending(lobby);
                await DoHwa.UI.UiSceneTransition.LoadAsync("Main");
            }
            catch (OperationCanceledException) { }
        }
        public void InviteFriends()
        {
            if (InLobby && SteamRuntime.Ready) SteamFriends.ActivateGameOverlayInviteDialog(Lobby);
        }
        public void OpenFriends()
        {
            if (SteamRuntime.Ready) SteamFriends.ActivateGameOverlay("Friends");
            else SetStatus("Steam을 실행하고 로그인해 주세요");
        }
        public async void Leave()
        {
            if (Busy) return;
            Busy = true;
            SetStatus("메인 메뉴로 돌아가는 중");
            try
            {
                Close();
                while (network.ShutdownInProgress) await Awaitable.NextFrameAsync(destroyCancellationToken);
                await DoHwa.UI.UiSceneTransition.LoadAsync("Main");
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { SetStatus(error.Message); }
            finally { Busy = false; }
        }
        private void Close()
        {
            leaving = true;
            if (network.IsHost) world.Save();
            if (InLobby && SteamRuntime.Ready)
            {
                if (network.IsServer) { SteamMatchmaking.SetLobbyData(Lobby, "ready", "0"); SteamMatchmaking.SetLobbyJoinable(Lobby, false); }
                SteamMatchmaking.LeaveLobby(Lobby); SteamFriends.ClearRichPresence();
            }
            Lobby = default; joined = false;
            if (network.IsListening) network.Shutdown();
            leaving = false;
        }
        private void Connected(ulong id)
        {
            if (id != network.LocalClientId) return;
            joined = true; SetStatus(network.IsHost ? "친구를 초대할 수 있습니다 · 최대 4명" : "친구와 함께 탐험 중");
        }
        private void Disconnected(ulong id)
        {
            if (leaving || !InLobby || network.IsServer || id != network.LocalClientId) return;
            Close(); SetStatus("방장과 연결이 끊어졌습니다");
        }
        private void MembersChanged(LobbyChatUpdate_t value)
        {
            if (value.m_ulSteamIDLobby != Lobby.m_SteamID) return;
            if (network.IsServer && !IsMember(value.m_ulSteamIDUserChanged))
            {
                ulong client = network.GetClientIdFromTransportId(value.m_ulSteamIDUserChanged);
                if (client != ulong.MaxValue && client != network.LocalClientId) network.DisconnectClient(client);
            }
            if (!network.IsServer && !IsMember(transport.HostSteamId)) { Close(); SetStatus("방장이 방을 종료했습니다"); }
            else Changed?.Invoke();
        }
        private void Update()
        {
            if (InLobby && !network.IsServer && !joined && Time.realtimeSinceStartup > connectionDeadline)
            { Close(); SetStatus("방장 연결 시간이 초과되었습니다"); }
        }
        private void OnDestroy()
        {
            SteamRuntime.JoinRequested -= InviteAccepted;
            network.OnClientDisconnectCallback -= Disconnected;
            network.OnClientConnectedCallback -= Connected;
            members?.Dispose(); Close();
        }
    }
}

