using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using DG.Tweening;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }
    public bool IsStarted => isStarted;
    public bool Direction => direction > 0;

    [SyncVar] private bool isStarted;
    public event Action<Dictionary<uint, PlayerInfo>> OnPlayersListUpdated;
    public event Action<uint> OnTurnChanged;
    public event Action<bool> OnDirectionChange;

    private readonly Dictionary<uint, PlayerInfo> players = new();
    private readonly List<uint> turnOrder = new();

    [SyncVar] private int currentTurnIndex = 0;
    [SyncVar(hook = nameof(NotifyChangeDirection))] private int direction = -1;

    [SerializeField] private GameObject[] activatables;
    [SerializeField] private Transform playerIndicator;

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

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        uint netId = player.netId;

        if (players.ContainsKey(netId)) return;

        PlayerInfo info = new PlayerInfo
        {
            netId = netId,
            playerName = player.GetPlayerName(),
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


    [Server]
    public void StartGame()
    {
        if (turnOrder.Count == 0) return;
        isStarted = true;
        CardManager.Instance.OnStartGame();
        StartGameRPC();

        currentTurnIndex = 0;
        NotifyTurnChanged();
    }

    [ClientRpc]
    private void StartGameRPC()
    {
        InputManager.InputActions.Player.Enable();
    }

    [Server]
    public void NextTurn()
    {
        if (turnOrder.Count == 0)
            return;

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
    private uint GetCurrentPlayer()
    {
        if (!IsStarted)
        {
            return 0;
        }
        if (turnOrder.Count == 0) 
        { 
            return 0; 
        }
        return turnOrder[currentTurnIndex];
    }

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
            players[id].tableAngle = angle;

            Vector3 pos = centerPoint.position + new Vector3(
                Mathf.Cos(angle),
                0,
                Mathf.Sin(angle)
            ) * radius;

            identity.transform.position = pos;
            identity.transform.LookAt(centerPoint);
        }
    }

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
    private void RpcTurnChanged(uint playerId)
    {
        OnTurnChanged?.Invoke(playerId);
        playerIndicator.DORotate(Vector3.down * players[playerId].tableAngle * Mathf.Rad2Deg, 0.5f);
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

        if (!players.ContainsKey(id)) 
            return;

        RpcTurnChanged(id);
    }

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
        if (isServer)
        {
            if (!NetworkServer.spawned.TryGetValue(playerId, out var identity))
                throw new Exception("Can't find player on server");

            return identity.transform.position;
        }
        else
        {
            if (!NetworkClient.spawned.TryGetValue(playerId, out var identity))
                throw new Exception("Can't find player on client");

            return identity.transform.position;
        }
    }

    private void NotifyChangeDirection(int oldValue, int newValue)
    {
        OnDirectionChange?.Invoke(newValue > 0);
    }
}