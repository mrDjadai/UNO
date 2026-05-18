using System;
using System.Collections.Generic;
using System.Collections;
using Mirror;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(AudioSource))]
public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }
    public bool IsStarted => currentState == GameState.Started;
    public bool Direction => direction > 0;
    public bool IsGameFull => players.Count > 6;

    [SyncVar] private GameState currentState = GameState.WaitForStart;
    public event Action<Dictionary<uint, PlayerInfo>> OnPlayersListUpdated;
    public event Action<uint> OnTurnChanged;
    public event Action<bool> OnDirectionChange;

    private readonly Dictionary<uint, PlayerInfo> players = new();
    private readonly List<uint> turnOrder = new();
    private List<uint> completedPlayers = new List<uint>();
    [SyncVar] private uint lastPlayer;

    [SerializeField] private AudioClip changeTurnClip;
    [SerializeField] private AudioClip gameEndClip;
    [SerializeField] private float unoSafeTime = 2f;

    [SyncVar] private int currentTurnIndex = 0;
    [SyncVar(hook = nameof(NotifyChangeDirection))] private int direction = -1;

    [SerializeField] private GameObject[] activatables;
    [SerializeField] private Transform playerIndicator;

    [Header("Table Settings")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float radius = 5f;
    [SerializeField] private WinManager winManager;
    [SerializeField] private StartPanel startPanel;

    private AudioSource audioSource;
    private Coroutine unoTimer;

    private void Awake()
    {
        Instance = this;
        foreach (var item in activatables)
        {
            item.SetActive(true);
        }
        audioSource = GetComponent<AudioSource>();
    }

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        uint netId = player.netId;

        if (players.ContainsKey(netId)) return;

        PlayerInfo info = new PlayerInfo
        {
            netId = netId,
            playerIndex = turnOrder.Count
        };

        players.Add(netId, info);
        turnOrder.Add(netId);

        RecalculatePositions();
        UpdateClients();
    }

    [Server]
    public void StartUnoTimer(uint player)
    {
        if (unoTimer != null)
        {
            StopCoroutine(unoTimer);
        }
        var idenity = NetworkServer.spawned[player];
        var pl = idenity.GetComponent<PlayerController>();
        pl.saidUnoSafe = true;

        if (idenity != null)
            unoTimer = StartCoroutine(SafeUno(pl));
    }

    private IEnumerator SafeUno(PlayerController player)
    {
        yield return new WaitForSeconds(unoSafeTime);
        player.saidUnoSafe = false;
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
        if (turnOrder.Count == 0) 
            return;

        currentState = GameState.Started;
        StartGameRPC();
        CardManager.Instance.OnStartGame();

        currentTurnIndex = 0;
        lastPlayer = turnOrder[0];

        NotifyTurnChanged();
    }

    [ClientRpc]
    private void StartGameRPC()
    {
        InputManager.InputActions.Player.Enable();
        CardManager.Instance.DestroyUpperCard();
    }

    [Server]
    public void NextTurn()
    {
        if (turnOrder.Count == 0)
            return;

        lastPlayer = turnOrder[currentTurnIndex];

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

            if (!NetworkClient.spawned.TryGetValue(p.netId, out var identity))
                continue;
            identity.GetComponent<PlayerController>().SetPlayerVisual(p.playerName, p.skinID);
        }

        OnPlayersListUpdated?.Invoke(players);
    }

    [ClientRpc]
    private void RpcTurnChanged(uint playerId)
    {
        OnTurnChanged?.Invoke(playerId);
        playerIndicator.DORotate(Vector3.down * players[playerId].tableAngle * Mathf.Rad2Deg, 0.5f);
        audioSource.PlayOneShot(changeTurnClip);
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

    [Server]
    public void SetPlayerData(uint id, string nick, int skin)
    {
        players[id].playerName = nick;
        players[id].skinID = skin;
        UpdateClients();
    }

    public void Exit()
    {
        ServerDataContainer.Instance.SetDisconnectType(ServerDataContainer.DisconnectType.ButtonPressed);
        if (NetworkServer.active && NetworkClient.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }
    }

    [Server]
    public void OnPlayerHandEmpty(uint netId)
    {
        completedPlayers.Add(netId);

        if (ServerDataContainer.Instance.OnlyOneWinner)
        {
            EndGame();
        }
        else
        {
            turnOrder.Remove(netId);

            if (completedPlayers.Count == 3)
            {
                EndGame();
            }
            else if (turnOrder.Count == 1)
            {
                foreach (var item in players.Keys)
                {
                    if (completedPlayers.Contains(item) == false)
                    {
                        completedPlayers.Add(item);
                        break;
                    }
                }
                EndGame();
            }
        }
    }

    private void EndGame()
    {
        currentState = GameState.Finished;
        EndGameRPC(completedPlayers);
    }

    [ClientRpc]
    private void EndGameRPC(List<uint> completedPlayers)
    {
        InputManager.InputActions.Player.Disable();
        winManager.ShowWindow(players, completedPlayers, isServer);
        audioSource.PlayOneShot(gameEndClip);
    }

    [Server]
    public void Restart()
    {
        turnOrder.Clear();
        completedPlayers.Clear();

        foreach (var item in players.Keys)
        {
            turnOrder.Add(item);

            if (!NetworkClient.spawned.TryGetValue(item, out var identity))
                continue;
            identity.GetComponent<PlayerController>().ClearHand();
        }
        startPanel.Show();
        RestartRPC();
        direction = -1;
    }

    [ClientRpc]
    private void RestartRPC()
    {
        winManager.Hide();
    }

    public PlayerController GetLastPlayer()
    {
        var indentity = NetworkClient.spawned[lastPlayer];

        if (indentity == null)
        {
            return null;
        }
        return indentity.GetComponent<PlayerController>();
    }

    private void OnApplicationQuit()
    {
        NetworkClient.Disconnect();
        NetworkServer.Shutdown();
    }
}

public enum GameState
{
    WaitForStart,
    Started,
    Finished
}