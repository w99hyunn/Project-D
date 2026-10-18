using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace DoHwa.World
{
    public sealed class SteamSocketTransport : NetworkTransport
    {
        public ulong HostSteamId { get; set; }
        public Func<ulong, bool> AcceptPeer { get; set; }
        public override ulong ServerClientId => 0;
        public override bool IsSupported => SteamRuntime.Ready;
        private readonly Dictionary<ulong, HSteamNetConnection> peers = new();
        private readonly Queue<(NetworkEvent type, ulong peer, byte[] data)> events = new();
        private readonly IntPtr[] received = new IntPtr[16];
        private Callback<SteamNetConnectionStatusChangedCallback_t> callback;
        private HSteamListenSocket listener;
        private HSteamNetConnection outbound;
        private bool server;
        public override void Initialize(NetworkManager networkManager = null) { }
        private void Register() => callback ??= Callback<SteamNetConnectionStatusChangedCallback_t>.Create(ConnectionChanged);
        public override bool StartServer()
        {
            if (!IsSupported) return false;
            Register(); server = true;
            listener = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, Array.Empty<SteamNetworkingConfigValue_t>());
            return listener.m_HSteamListenSocket != 0;
        }
        public override bool StartClient()
        {
            if (!IsSupported || HostSteamId == 0) return false;
            Register(); server = false;
            var identity = new SteamNetworkingIdentity(); identity.SetSteamID64(HostSteamId);
            outbound = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, Array.Empty<SteamNetworkingConfigValue_t>());
            peers[0] = outbound;
            return outbound.m_HSteamNetConnection != 0;
        }
        private void ConnectionChanged(SteamNetConnectionStatusChangedCallback_t change)
        {
            ulong steamId = change.m_info.m_identityRemote.GetSteamID64();
            if (server && change.m_info.m_hListenSocket != listener) return;
            if (!server && change.m_hConn != outbound) return;
            ulong peer = server ? steamId : 0;
            switch (change.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    if (!server) return;
                    if (peers.Count >= 3 || peers.ContainsKey(peer) || AcceptPeer == null || !AcceptPeer(steamId))
                    { SteamNetworkingSockets.CloseConnection(change.m_hConn, 0, "Lobby membership required", false); return; }
                    if (SteamNetworkingSockets.AcceptConnection(change.m_hConn) == EResult.k_EResultOK) peers[peer] = change.m_hConn;
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    peers[peer] = change.m_hConn;
                    events.Enqueue((NetworkEvent.Connect, peer, null));
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    SteamNetworkingSockets.CloseConnection(change.m_hConn, 0, "Disconnected", false);
                    if (peers.Remove(peer)) events.Enqueue((NetworkEvent.Disconnect, peer, null));
                    break;
            }
        }
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (events.Count == 0 && IsSupported)
                foreach (var peer in peers)
                {
                    int count = SteamNetworkingSockets.ReceiveMessagesOnConnection(peer.Value, received, received.Length);
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var message = Marshal.PtrToStructure<SteamNetworkingMessage_t>(received[i]);
                            var bytes = new byte[message.m_cbSize];
                            Marshal.Copy(message.m_pData, bytes, 0, bytes.Length);
                            events.Enqueue((NetworkEvent.Data, peer.Key, bytes));
                        }
                        finally { SteamNetworkingMessage_t.Release(received[i]); }
                    }
                }
            receiveTime = Time.realtimeSinceStartup;
            if (events.TryDequeue(out var next))
            { clientId = next.peer; payload = next.data == null ? default : new ArraySegment<byte>(next.data); return next.type; }
            clientId = 0; payload = default; return NetworkEvent.Nothing;
        }
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            if (!peers.TryGetValue(clientId, out var connection)) return;
            int flags = delivery == NetworkDelivery.Unreliable || delivery == NetworkDelivery.UnreliableSequenced
                ? Constants.k_nSteamNetworkingSend_UnreliableNoNagle : Constants.k_nSteamNetworkingSend_ReliableNoNagle;
            var pinned = GCHandle.Alloc(payload.Array, GCHandleType.Pinned);
            try
            {
                var result = SteamNetworkingSockets.SendMessageToConnection(connection,
                    IntPtr.Add(pinned.AddrOfPinnedObject(), payload.Offset), (uint)payload.Count, flags, out _);
                if (result != EResult.k_EResultOK) Debug.LogWarning("Steam send: " + result);
            }
            finally { pinned.Free(); }
        }
        public override ulong GetCurrentRtt(ulong clientId)
        {
            if (!peers.TryGetValue(clientId, out var connection)) return 0;
            var lane = new SteamNetConnectionRealTimeLaneStatus_t();
            var status = new SteamNetConnectionRealTimeStatus_t();
            SteamNetworkingSockets.GetConnectionRealTimeStatus(connection, ref status, 0, ref lane);
            return (ulong)Mathf.Max(0, status.m_nPing);
        }
        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (peers.Remove(clientId, out var connection)) SteamNetworkingSockets.CloseConnection(connection, 0, "Disconnected", false);
        }
        public override void DisconnectLocalClient() => DisconnectRemoteClient(0);
        public override void Shutdown()
        {
            if (IsSupported)
            {
                foreach (var connection in peers.Values) SteamNetworkingSockets.CloseConnection(connection, 0, "Session ended", false);
                if (listener.m_HSteamListenSocket != 0) SteamNetworkingSockets.CloseListenSocket(listener);
            }
            peers.Clear(); events.Clear(); listener = default; outbound = default; server = false;
            callback?.Dispose(); callback = null;
        }
        private void OnDestroy() => Shutdown();
    }
}
