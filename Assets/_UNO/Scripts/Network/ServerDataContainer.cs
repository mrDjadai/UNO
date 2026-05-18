using UnityEngine;
using Mirror;

public class ServerDataContainer : NetworkBehaviour
{
    public static ServerDataContainer Instance;
    public static DisconnectType Disconnect { get; private set; }
    public bool AlwaysAllowWildCards => alwaysAllowWildCards;

    public bool SummarizeGetCards => summarizeGetCards;

    public bool TakeOnlyOneCard => takeOnlyOneCard;

    public bool OnlyOneWinner => onlyOneWinner;

    [SyncVar] private bool alwaysAllowWildCards;
    [SyncVar] private bool summarizeGetCards;
    [SyncVar] private bool takeOnlyOneCard;
    [SyncVar] private bool onlyOneWinner;
    [SerializeField] private MessageValue messageValue;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        alwaysAllowWildCards = PlayerPrefs.GetInt(nameof(alwaysAllowWildCards)) == 1;
        summarizeGetCards = PlayerPrefs.GetInt(nameof(summarizeGetCards)) == 1;
        takeOnlyOneCard = PlayerPrefs.GetInt(nameof(takeOnlyOneCard)) == 1;
        onlyOneWinner = PlayerPrefs.GetInt(nameof(onlyOneWinner)) == 1;
    }

    public enum DisconnectType
    {
        ButtonPressed,
        NetworkError,
        FullGame,
        GameStarted
    }

    public void SetDisconnectType(DisconnectType type)
    {
        Disconnect = type;
    }
}
