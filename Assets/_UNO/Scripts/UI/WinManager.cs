using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class WinManager : MonoBehaviour
{
    [SerializeField] private Transform window;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private GameObject restartButton;

    public void ShowWindow(Dictionary<uint, PlayerInfo> players, List<uint> completedPlayers, bool isServer)
    {
        InputManager.Instance.SetCursorLocked(false);
        window.DOScale(Vector3.one, 0.5f);
        restartButton.SetActive(isServer);

        if (completedPlayers.Count == 1)
        {
            winnerText.text = "Победитель\n" + players[completedPlayers[0]].playerName;
        }
        else
        {
            string txt = "Победители";
            for (int i = 0; i < completedPlayers.Count; i++)
            {
                txt += $"\n{i + 1}. {players[completedPlayers[i]].playerName}";
            }
            winnerText.text = txt;
        }
    }

    public void Hide()
    {
        window.DOScale(Vector3.zero, 0.5f);
    }
}
