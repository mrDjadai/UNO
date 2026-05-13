using UnityEngine;
using TMPro;
using Mirror;
using System.Net;
using System.Net.Sockets;

public class ServerInfoDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;

    private void Start()
    {
        if (NetworkServer.active)
        {
            int port = PlayerPrefs.GetInt("ServerPort");
            string ip = GetLocalIP();
            infoText.text = $"{ip}:{port}";
        }
    }

    private string GetLocalIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}