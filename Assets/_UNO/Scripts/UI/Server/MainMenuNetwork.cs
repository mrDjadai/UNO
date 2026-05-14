using Mirror;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class MainMenuNetwork : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private NetworkManager networkManager;

    [Header("UI")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    public bool alwaysAllowWildCards { get; set; }
    public bool summarizeGetCards { get; set; }
    public bool takeOnlyOneCard { get; set; }
    public bool onlyOneWinner { get; set; }

    [Header("Port Validation")]
    [SerializeField] private ushort defaultPort = 7777;

    private void Awake()
    {
        portInput.onValueChanged.AddListener(ValidatePortInput);

        if (string.IsNullOrWhiteSpace(portInput.text))
        {
            portInput.text = defaultPort.ToString();
        }
    }

    private void OnDestroy()
    {
        portInput.onValueChanged.RemoveListener(ValidatePortInput);
    }

    private void ValidatePortInput(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        StringBuilder filtered = new StringBuilder();

        foreach (char c in value)
        {
            if (char.IsDigit(c))
            {
                filtered.Append(c);
            }
        }

        string filteredText = filtered.ToString();

        if (filteredText.Length > 5)
        {
            filteredText = filteredText[..5];
        }

        if (ushort.TryParse(filteredText, out ushort port))
        {
            if (port == 0)
            {
                filteredText = defaultPort.ToString();
            }
        }
        else if (!string.IsNullOrEmpty(filteredText))
        {
            filteredText = defaultPort.ToString();
        }

        if (portInput.text != filteredText)
        {
            portInput.text = filteredText;
            portInput.caretPosition = portInput.text.Length;
        }
    }

    public void CreateServer()
    {
        CreateLocalServer();
    }

    public void CreateLocalServer()
    {
        ApplySettings();

        ushort freePort = GetFreePort();

        TelepathyTransport transport =
            Transport.active as TelepathyTransport;

        transport.port = freePort;

        portInput.text = freePort.ToString();

        PlayerPrefs.SetInt("ServerPort", freePort);

        networkManager.StartHost();
    }

    private ushort GetFreePort()
    {
        TcpListener listener =
            new TcpListener(IPAddress.Any, 0);

        listener.Start();

        ushort port =
            (ushort)((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();

        return port;
    }

    public void ConnectLocal()
    {
        networkManager.networkAddress = ipInput.text;
        TelepathyTransport transport = Transport.active as TelepathyTransport;

        transport.port = ushort.Parse(portInput.text);

        networkManager.StartClient();
    }

    private void ApplySettings()
    {
        PlayerPrefs.SetInt(nameof(alwaysAllowWildCards), alwaysAllowWildCards ? 1 : 0);
        PlayerPrefs.SetInt(nameof(summarizeGetCards), summarizeGetCards ? 1 : 0);
        PlayerPrefs.SetInt(nameof(takeOnlyOneCard), takeOnlyOneCard ? 1 : 0);
        PlayerPrefs.SetInt(nameof(onlyOneWinner), onlyOneWinner ? 1 : 0);
    }
}