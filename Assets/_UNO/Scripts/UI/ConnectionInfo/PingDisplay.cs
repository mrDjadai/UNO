using UnityEngine;
using TMPro;
using Mirror;

public class PingDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text pingText;

    private void Update()
    {
        if (NetworkClient.active && NetworkClient.connection != null)
        {
            double rtt = NetworkTime.rtt;
            int ping = (int)(rtt * 1000);
            pingText.text = $"ѕинг: {ping} мс";
        }
    }
}