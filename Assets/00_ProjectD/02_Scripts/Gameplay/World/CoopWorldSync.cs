using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] internal sealed class CoopPacket
    {
        public string identity;
        public Vector3 position;
        public WorldCommand command;
        public bool success;
        public string reason;
        public string snapshot;
        public string[] activePlayers;
    }

    // NGO carries ordered commands and authoritative snapshots; the existing local rig stays intact.
    public sealed class CoopWorldSync : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [SerializeField] private WorldRuntime world;
        [SerializeField] private HeroMotor hero;
        [SerializeField] private GameObject remoteAvatarPrefab;
        private readonly Dictionary<ulong, string> identities = new();
        private readonly Dictionary<ulong, float> lastMove = new();
        private readonly Dictionary<ulong, float> lastAction = new();
        private readonly Dictionary<string, CoopAvatar> avatars = new();
        private float nextPose;
        private bool registered;
        private bool awaitingInitialSnapshot;
        private WorldState offlineState;

        private void Awake()
        {
            network.OnClientConnectedCallback += Connected;
            network.OnClientDisconnectCallback += Disconnected;
            network.ConnectionApprovalCallback += Approve;
            network.NetworkConfig.ConnectionApproval = true;
            string identity = PlayerIdentity.Get();
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(identity);
            world.RemoteCommand += Submit;
            world.Changed += Broadcast;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string identity = Encoding.UTF8.GetString(request.Payload);
            bool validIdentity;
            if (network.NetworkConfig.NetworkTransport is SteamSocketTransport)
            {
                ulong steamId = request.ClientNetworkId == NetworkManager.ServerClientId
                    ? Steamworks.SteamUser.GetSteamID().m_SteamID
                    : network.GetTransportIdFromClientId(request.ClientNetworkId);
                validIdentity = steamId != ulong.MaxValue && identity == "steam:" + steamId;
            }
            else validIdentity = Guid.TryParseExact(identity, "N", out _);
            response.Approved = network.ConnectedClientsIds.Count < 4 && validIdentity
                && !identities.ContainsValue(identity);
            response.CreatePlayerObject = false;
            response.Reason = "방이 가득 찼거나 이미 접속한 플레이어입니다";
            if (response.Approved) identities[request.ClientNetworkId] = identity;
        }

        private void Register()
        {
            if (registered) return;
            registered = true;
            network.CustomMessagingManager.RegisterNamedMessageHandler("dohwa.command", Command);
            network.CustomMessagingManager.RegisterNamedMessageHandler("dohwa.pose", Pose);
            network.CustomMessagingManager.RegisterNamedMessageHandler("dohwa.state", Snapshot);
            network.CustomMessagingManager.RegisterNamedMessageHandler("dohwa.result", Result);
        }

        private void Connected(ulong client)
        {
            Register();
            world.IsAuthority = network.IsServer;
            if (client == network.LocalClientId)
            {
                awaitingInitialSnapshot = !network.IsServer;
                offlineState = JsonUtility.FromJson<WorldState>(JsonUtility.ToJson(world.State));
                world.LocalPlayerId = Encoding.UTF8.GetString(network.NetworkConfig.ConnectionData);
                if (network.IsServer)
                {
                    identities[client] = world.LocalPlayerId;
                    var local = world.State.Player("local");
                    if (world.State.Player(world.LocalPlayerId) == null && local != null) local.id = world.LocalPlayerId;
                }
            }
            if (!network.IsServer) return;
            world.State.GetOrCreatePlayer(identities[client], world.Catalog);
            world.ActivePlayers.Clear();
            foreach (string identity in identities.Values) world.ActivePlayers.Add(identity);
            world.Commit();
        }

        private void Disconnected(ulong client)
        {
            if (identities.TryGetValue(client, out var id)) world.ActivePlayers.Remove(id);
            identities.Remove(client); lastMove.Remove(client); lastAction.Remove(client);
            if (client != network.LocalClientId) { Broadcast(); return; }
            foreach (var avatar in avatars.Values) Destroy(avatar.gameObject);
            avatars.Clear(); registered = false;
            bool wasGuest = !world.IsAuthority;
            world.IsAuthority = true;
            if (wasGuest && offlineState != null)
            {
                world.ApplySnapshot(JsonUtility.ToJson(offlineState));
                hero.TryGetComponent<CharacterController>(out var body);
                body.enabled = false;
                hero.transform.position = world.LocalPlayer.position;
                body.enabled = true;
            }
            world.LocalPlayerId = PlayerIdentity.Get();
            world.ActivePlayers.Clear(); world.ActivePlayers.Add(world.LocalPlayerId);
        }

        private void Update()
        {
            if (!network.IsListening || !network.IsConnectedClient || Time.unscaledTime < nextPose) return;
            nextPose = Time.unscaledTime + .1f;
            if (network.IsServer)
            {
                world.LocalPlayer.position = hero.transform.position;
                Broadcast();
            }
            else if (!awaitingInitialSnapshot) Send("dohwa.pose", NetworkManager.ServerClientId, new CoopPacket { position = hero.transform.position });
        }

        private void Pose(ulong sender, FastBufferReader reader)
        {
            if (!network.IsServer || !identities.TryGetValue(sender, out string id)) return;
            var packet = Read(reader);
            if (!float.IsFinite(packet.position.x) || !float.IsFinite(packet.position.y) || !float.IsFinite(packet.position.z)) return;
            float elapsed = lastMove.TryGetValue(sender, out float time) ? Mathf.Clamp(Time.unscaledTime - time, 0, .5f) : .1f;
            var player = world.State.Player(id);
            if (Vector3.Distance(player.position, packet.position) <= elapsed * 14f + .5f) player.position = packet.position;
            lastMove[sender] = Time.unscaledTime;
        }

        private void Submit(WorldCommand command) => Send("dohwa.command", NetworkManager.ServerClientId, new CoopPacket { command = command });

        private void Command(ulong sender, FastBufferReader reader)
        {
            if (!network.IsServer || !identities.TryGetValue(sender, out string id)) return;
            if (lastAction.TryGetValue(sender, out float at) && Time.unscaledTime - at < .08f) return;
            lastAction[sender] = Time.unscaledTime;
            var packet = Read(reader);
            packet.success = world.Execute(id, packet.command, out packet.reason);
            Send("dohwa.result", sender, packet);
        }

        private void Snapshot(ulong sender, FastBufferReader reader)
        {
            if (network.IsServer || sender != NetworkManager.ServerClientId) return;
            var packet = Read(reader);
            world.ActivePlayers.Clear();
            foreach (string id in packet.activePlayers) world.ActivePlayers.Add(id);
            world.ApplySnapshot(packet.snapshot);
            if (awaitingInitialSnapshot)
            {
                hero.TryGetComponent<CharacterController>(out var body);
                body.enabled = false;
                hero.transform.position = world.LocalPlayer.position;
                body.enabled = true;
                awaitingInitialSnapshot = false;
            }
            UpdateAvatars();
        }

        private void Result(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            var packet = Read(reader);
            world.ReceiveResult(packet.command, packet.success, packet.reason);
        }

        private void Broadcast()
        {
            if (!network.IsListening || !network.IsServer || !registered) return;
            var active = new string[world.ActivePlayers.Count]; world.ActivePlayers.CopyTo(active);
            var packet = new CoopPacket { snapshot = JsonUtility.ToJson(world.State), activePlayers = active };
            foreach (ulong client in network.ConnectedClientsIds)
                if (client != network.LocalClientId) Send("dohwa.state", client, packet);
            UpdateAvatars();
        }

        private void UpdateAvatars()
        {
            foreach (var player in world.State.players)
            {
                if (player.id == world.LocalPlayerId || player.id == "local") continue;
                if (!world.ActivePlayers.Contains(player.id))
                { if (avatars.TryGetValue(player.id, out var stale)) stale.gameObject.SetActive(false); continue; }
                if (!avatars.TryGetValue(player.id, out var avatar))
                {
                    var instance = Instantiate(remoteAvatarPrefab, player.position, Quaternion.identity);
                    instance.TryGetComponent<CoopAvatar>(out avatar);
                    avatars.Add(player.id, avatar);
                }
                avatar.gameObject.SetActive(true);
                avatar.SetTarget(player.position);
            }
        }

        private static CoopPacket Read(FastBufferReader reader)
        {
            reader.ReadValueSafe(out string json);
            return JsonUtility.FromJson<CoopPacket>(json);
        }

        private void Send(string name, ulong client, CoopPacket packet)
        {
            string json = JsonUtility.ToJson(packet);
            using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(json), Allocator.Temp);
            writer.WriteValueSafe(json);
            network.CustomMessagingManager.SendNamedMessage(name, client, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void OnDestroy()
        {
            network.OnClientConnectedCallback -= Connected;
            network.OnClientDisconnectCallback -= Disconnected;
            network.ConnectionApprovalCallback -= Approve;
            world.RemoteCommand -= Submit;
            world.Changed -= Broadcast;
        }
    }
}
