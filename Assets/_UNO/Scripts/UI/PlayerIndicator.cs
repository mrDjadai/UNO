using UnityEngine;
using TMPro;

public class PlayerIndicator : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private GameObject activeHighlight;

    public void UpdateInfo(PlayerInfo info)
    {
        nameText.text = info.playerName;
        indexText.text = $"#{info.playerIndex + 1}";
    }

    public void UpdateActive(bool isActive)
    {
        if (activeHighlight != null)
            activeHighlight.SetActive(isActive);
    }
}