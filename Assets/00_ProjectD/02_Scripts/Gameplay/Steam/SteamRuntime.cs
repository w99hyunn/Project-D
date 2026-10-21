using System;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa.World
{
    [DefaultExecutionOrder(-10000)]
    public sealed class SteamRuntime : MonoBehaviour
    {
        public static bool Ready { get; private set; }
        public static string Error { get; private set; }
        public static SteamSettings Settings { get; private set; }
        public static CSteamID PendingLobby { get; private set; }
        public static event Action<CSteamID> JoinRequested;
        private Callback<GameLobbyJoinRequested_t> invite;
        private Callback<GameRichPresenceJoinRequested_t> friendJoin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Ready = false; Error = ""; PendingLobby = default; JoinRequested = null; }
        private void Awake()
        {
            Settings = Resources.Load<SteamSettings>("SteamSettings");
            try
            {
#if UNITY_EDITOR
                SetDevelopmentAppId();
#else
                // Spacewar 테스트는 Steam에 등록된 다른 실행 파일로 재실행하지 않는다.
                if (Settings.appId == 480) SetDevelopmentAppId();
                else if (SteamAPI.RestartAppIfNecessary(new AppId_t(Settings.appId)))
                {
                    Debug.Log($"Steam 실행으로 전환합니다. App ID: {Settings.appId}");
                    Application.Quit();
                    return;
                }
#endif
                Ready = SteamAPI.InitEx(out var error) == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
                Error = error;
                if (!Ready)
                {
                    Debug.LogError($"Steam 초기화 실패 (App ID: {Settings.appId}): {Error}");
                    return;
                }
                SteamNetworkingUtils.InitRelayNetworkAccess();
                invite = Callback<GameLobbyJoinRequested_t>.Create(e => RequestJoin(e.m_steamIDLobby));
                friendJoin = Callback<GameRichPresenceJoinRequested_t>.Create(e =>
                {
                    if (TryParseLobby(e.m_rgchConnect.Split(' ', StringSplitOptions.RemoveEmptyEntries), out var lobby)) RequestJoin(lobby);
                });
                var args = Environment.GetCommandLineArgs();
                if (TryParseLobby(args, out var pending)) PendingLobby = pending;
            }
            catch (Exception error) { Error = error.Message; Ready = false; Debug.LogException(error); }
        }
        private static void SetDevelopmentAppId()
        {
            Environment.SetEnvironmentVariable("SteamAppId", Settings.appId.ToString());
            Environment.SetEnvironmentVariable("SteamGameId", Settings.appId.ToString());
        }
        private void Start()
        {
            if (PendingLobby.IsValid()) RequestJoin(PendingLobby);
        }
        private void Update() { if (Ready) SteamAPI.RunCallbacks(); }
        private static void RequestJoin(CSteamID lobby)
        {
            PendingLobby = lobby;
            if (JoinRequested != null) JoinRequested.Invoke(lobby);
            else if (SceneManager.GetActiveScene().name != "Main") _ = DoHwa.UI.UiSceneTransition.LoadAsync("Main");
        }
        public static void ClearPending() => PendingLobby = default;
        public static void SetPending(CSteamID lobby) => PendingLobby = lobby;
        public static bool TryParseLobby(string[] args, out CSteamID lobby)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "+connect_lobby" && ulong.TryParse(args[i + 1], out var id) && id != 0)
                { lobby = new CSteamID(id); return lobby.IsValid(); }
            lobby = default; return false;
        }
        private void OnDestroy()
        {
            invite?.Dispose();
            friendJoin?.Dispose();
            if (Ready) SteamAPI.Shutdown();
            Ready = false;
        }
    }
}
