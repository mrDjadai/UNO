using System.Collections.Generic;
using UnityEngine;

public class PlayerListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private PlayerIndicator playerItemPrefab;

    private TurnManager turnManager => TurnManager.Instance;
    private Dictionary<uint, PlayerIndicator> playerItems = new();

    private uint currentActivePlayer = 0;

    private void Start()
    {
        if (turnManager == null)
        {
            Debug.LogError("TurnManager not found!");
            return;
        }

        turnManager.OnPlayersListUpdated += UpdatePlayerList;
        turnManager.OnTurnChanged += OnTurnChanged;

        // первичная инициализация
        UpdatePlayerList(turnManager.GetLocalPlayersInfo());
    }

    private void UpdatePlayerList(Dictionary<uint, PlayerInfo> players)
    {
        // Удаляем лишних
        var toRemove = new List<uint>();

        foreach (var item in playerItems)
        {
            if (!players.ContainsKey(item.Key))
                toRemove.Add(item.Key);
        }

        foreach (var id in toRemove)
        {
            Destroy(playerItems[id].gameObject); // ← фикс
            playerItems.Remove(id);
        }

        // Добавляем / обновляем
        foreach (var pair in players)
        {
            if (!playerItems.ContainsKey(pair.Key))
            {
                var newItem = Instantiate(playerItemPrefab, playerListContainer);
                playerItems[pair.Key] = newItem;
            }

            playerItems[pair.Key].UpdateInfo(pair.Value);
        }

        UpdateSortingOrder(players);
        UpdateActiveVisual();
    }

    private void OnTurnChanged(uint playerId, string playerName)
    {
        currentActivePlayer = playerId;
        UpdateActiveVisual();

        Debug.Log($"Turn: {playerName}");
    }

    private void UpdateActiveVisual()
    {
        foreach (var pair in playerItems)
        {
            bool isActive = pair.Key == currentActivePlayer;
            pair.Value.UpdateActive(isActive);
        }
    }

    private void UpdateSortingOrder(Dictionary<uint, PlayerInfo> players)
    {
        var list = new List<KeyValuePair<uint, PlayerIndicator>>(playerItems);

        list.Sort((a, b) =>
        {
            int indexA = players.ContainsKey(a.Key) ? players[a.Key].playerIndex : 999;
            int indexB = players.ContainsKey(b.Key) ? players[b.Key].playerIndex : 999;
            return indexA.CompareTo(indexB);
        });

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Value.transform.SetSiblingIndex(i);
        }
    }

    private void OnDestroy()
    {
        if (turnManager == null) return;

        turnManager.OnPlayersListUpdated -= UpdatePlayerList;
        turnManager.OnTurnChanged -= OnTurnChanged;
    }
}