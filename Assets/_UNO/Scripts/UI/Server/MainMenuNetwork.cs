using Mirror;
using TMPro;
using UnityEngine;
using System.Text;
using UnityEngine.UI;

public class MainMenuNetwork : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private NetworkManager networkManager;

    [Header("UI")]
    [SerializeField] private TMP_InputField serverNameInput;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private Toggle isPublic;

    public bool alwaysAllowWildCards { get; set; }
    public bool summarizeGetCards { get; set; }
    public bool takeOnlyOneCard { get; set; }
    public bool onlyOneWinner { get; set; }

    [Header("Scene")]
    [SerializeField] private string gameScene = "Game";

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
        if (isPublic.isOn)
        {
            CreateGlobalServer();
        }
        else
        {
            CreateLocalServer();
        }
    }    

    public void CreateLocalServer()
    {
        ApplySettings();

        networkManager.StartHost();

//        networkManager.ServerChangeScene(gameScene);
    }

    public void CreateGlobalServer()
    {
        ApplySettings();

        networkManager.StartHost();

        MasterServerClient.Instance.RegisterServer(
            serverNameInput.text);

//        networkManager.ServerChangeScene(gameScene);
    }

    public void ConnectLocal()
    {
        networkManager.networkAddress = ipInput.text;
        TelepathyTransport transport = Transport.active as TelepathyTransport;

        transport.port = ushort.Parse(portInput.text);

        networkManager.StartClient();
    }

    public void ConnectGlobal(string address, ushort port)
    {
        networkManager.networkAddress = address;

        TelepathyTransport transport = Transport.active as TelepathyTransport;

        if (transport != null)
        {
            transport.port = port;
        }

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