using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject cameraOrigin;
    public readonly SyncList<CardData> Hand = new();

    public uint PlayerId => netId;

    private void Start()
    {
        cameraOrigin.SetActive(isLocalPlayer);
    }

    public override void OnStartServer()
    {
        TurnManager.Instance.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterPlayer(this);
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("I am local player");
    }

    public void EndTurn()
    {
        if (!isLocalPlayer) return;

        CmdEndTurn();
    }

    [Command]
    private void CmdEndTurn()
    {
        // сервер проверяет, что сейчас ход этого игрока
        if (TurnManager.Instance.GetCurrentPlayer() != netId)
            return;

        TurnManager.Instance.NextTurn();
    }
}