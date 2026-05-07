using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<Dictionary<uint, PlayerInfo>> OnPlayersListUpdated;
    public event Action<uint, string> OnTurnChanged;

    private readonly Dictionary<uint, PlayerInfo> players = new();
    private readonly List<uint> turnOrder = new();

    [SyncVar] private int currentTurnIndex = 0;
    [SyncVar] private int direction = 1; // 1 = вперед, -1 = назад

    [SerializeField] private GameObject[] activatables;
    [Header("Table Settings")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float radius = 5f;

    private void Awake()
    {
        Instance = this;
        foreach (var item in activatables)
        {
            item.SetActive(true);
        }
    }

    #region Registration (вызывается из PlayerController)

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        uint netId = player.netId;

        if (players.ContainsKey(netId)) return;

        PlayerInfo info = new PlayerInfo
        {
            netId = netId,
            playerName = $"Player {netId}",
            playerIndex = turnOrder.Count
        };

        players.Add(netId, info);
        turnOrder.Add(netId);

        RecalculatePositions();
        UpdateClients();
    }

    [Server]
    public void UnregisterPlayer(PlayerController player)
    {
        uint netId = player.netId;

        if (!players.ContainsKey(netId)) return;

        int index = turnOrder.IndexOf(netId);

        players.Remove(netId);
        turnOrder.Remove(netId);

        if (index <= currentTurnIndex && currentTurnIndex > 0)
            currentTurnIndex--;

        UpdatePlayerIndexes();
        RecalculatePositions();
        UpdateClients();
    }

    #endregion

    #region Turn Logic

    [Server]
    public void StartGame()
    {
        if (turnOrder.Count == 0) return;

        currentTurnIndex = 0;
        NotifyTurnChanged();
    }

    [Server]
    public void NextTurn()
    {
        if (turnOrder.Count == 0) return;

        currentTurnIndex += direction;

        if (currentTurnIndex >= turnOrder.Count)
            currentTurnIndex = 0;
        else if (currentTurnIndex < 0)
            currentTurnIndex = turnOrder.Count - 1;

        NotifyTurnChanged();
    }

    [Server]
    public void ReverseDirection()
    {
        direction *= -1;
    }

    [Server]
    public uint GetCurrentPlayer()
    {
        if (turnOrder.Count == 0) return 0;
        return turnOrder[currentTurnIndex];
    }

    #endregion

    #region Positions (круг)

    [Server]
    private void RecalculatePositions()
    {
        int count = turnOrder.Count;
        if (count == 0 || centerPoint == null) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            uint id = turnOrder[i];

            if (!NetworkServer.spawned.TryGetValue(id, out var identity))
                continue;

            float angle = i * angleStep * Mathf.Deg2Rad;

            Vector3 pos = centerPoint.position + new Vector3(
                Mathf.Cos(angle),
                0,
                Mathf.Sin(angle)
            ) * radius;

            identity.transform.position = pos;
            identity.transform.LookAt(centerPoint);
        }

        CardManager.Instance.RotateDeckToPlayer();
    }

    #endregion

    #region Sync (без Dictionary в RPC!)

    [ClientRpc]
    private void RpcUpdatePlayers(List<PlayerInfo> playersList)
    {
        players.Clear();

        foreach (var p in playersList)
        {
            players[p.netId] = p;
        }

        OnPlayersListUpdated?.Invoke(players);
    }

    [ClientRpc]
    private void RpcTurnChanged(uint playerId, string playerName)
    {
        OnTurnChanged?.Invoke(playerId, playerName);
    }

    [Server]
    private void UpdateClients()
    {
        List<PlayerInfo> list = new List<PlayerInfo>(players.Values);
        RpcUpdatePlayers(list);
    }

    [Server]
    private void NotifyTurnChanged()
    {
        uint id = GetCurrentPlayer();

        if (!players.ContainsKey(id)) return;

        string name = players[id].playerName;
        RpcTurnChanged(id, name);
    }

    #endregion

    #region Helpers

    private void UpdatePlayerIndexes()
    {
        for (int i = 0; i < turnOrder.Count; i++)
        {
            uint id = turnOrder[i];
            players[id].playerIndex = i;
        }
    }

    public Dictionary<uint, PlayerInfo> GetLocalPlayersInfo()
    {
        return players;
    }

    public Vector3 GetPlayerPosition(uint playerId)
    {
        if (!NetworkServer.spawned.TryGetValue(playerId, out var identity))
            throw new Exception("Can't find player");

        return identity.transform.position;
    }

    #endregion
}