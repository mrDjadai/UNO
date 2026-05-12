using Mirror;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class MasterServerClient : MonoBehaviour
{
    public static MasterServerClient Instance;

    [SerializeField]
    private string masterServerUrl =
        "http://localhost:5000";

    [SerializeField]
    private float heartbeatInterval = 5f;

    private string serverId;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterServer(
        string serverName)
    {
        StartCoroutine(
            RegisterServerCoroutine(serverName));
    }

    private IEnumerator RegisterServerCoroutine(
        string serverName)
    {
        RegisterServerData data =
            new RegisterServerData
            {
                serverName = serverName,

                address = GetLocalIPAddress(),
                port = 7777,

                players = NetworkServer.connections.Count,
                maxPlayers = 4,

                gameStarted = false,

                alwaysAllowWildCards =
                    ServerDataContainer.Instance
                        .AlwaysAllowWildCards,

                summarizeGetCards =
                    ServerDataContainer.Instance
                        .SummarizeGetCards,

                takeOnlyOneCard =
                    ServerDataContainer.Instance
                        .TakeOnlyOneCard,

                onlyOneWinner =
                    ServerDataContainer.Instance
                        .OnlyOneWinner
            };

        string json =
            JsonUtility.ToJson(data);

        byte[] body =
            Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(
                masterServerUrl + "/register",
                "POST");

        request.uploadHandler =
            new UploadHandlerRaw(body);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        RegisterResponse response =
            JsonUtility.FromJson<RegisterResponse>(
                request.downloadHandler.text);

        serverId = response.serverId;

        InvokeRepeating(
            nameof(SendHeartbeat),
            heartbeatInterval,
            heartbeatInterval);
    }

    private void SendHeartbeat()
    {
        StartCoroutine(SendHeartbeatCoroutine());
    }

    private IEnumerator SendHeartbeatCoroutine()
    {
        if (string.IsNullOrEmpty(serverId))
            yield break;

        HeartbeatData data =
            new HeartbeatData
            {
                serverId = serverId,

                players =
                    NetworkServer.connections.Count,

                gameStarted =
                    TurnManager.Instance.IsStarted
            };

        string json =
            JsonUtility.ToJson(data);

        byte[] body =
            Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(
                masterServerUrl + "/heartbeat",
                "POST");

        request.uploadHandler =
            new UploadHandlerRaw(body);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
    }

    public void UnregisterServer()
    {
        StartCoroutine(UnregisterCoroutine());
    }

    private IEnumerator UnregisterCoroutine()
    {
        if (string.IsNullOrEmpty(serverId))
            yield break;

        UnityWebRequest request =
            UnityWebRequest.PostWwwForm(
                masterServerUrl +
                "/remove/" +
                serverId,
                "");

        yield return request.SendWebRequest();
    }

    private string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }

        return localIP;
    }
}