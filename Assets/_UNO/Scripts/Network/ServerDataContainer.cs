using UnityEngine;
using Mirror;

public class ServerDataContainer : NetworkBehaviour
{
    public static ServerDataContainer Instance;

    public bool AlwaysAllowWildCards => alwaysAllowWildCards;

    public bool SummarizeGetCards => summarizeGetCards;

    public bool TakeOnlyOneCard => takeOnlyOneCard;

    public bool OnlyOneWinner => onlyOneWinner;

    private bool alwaysAllowWildCards;
    private bool summarizeGetCards;
    private bool takeOnlyOneCard;
    private bool onlyOneWinner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        alwaysAllowWildCards = PlayerPrefs.GetInt(nameof(alwaysAllowWildCards)) == 1;
        summarizeGetCards = PlayerPrefs.GetInt(nameof(summarizeGetCards)) == 1;
        takeOnlyOneCard = PlayerPrefs.GetInt(nameof(takeOnlyOneCard)) == 1;
        onlyOneWinner = PlayerPrefs.GetInt(nameof(onlyOneWinner)) == 1;
    }
}
