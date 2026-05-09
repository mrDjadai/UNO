using UnityEngine;
using Mirror;

public class ServerDataContainer : NetworkBehaviour
{
    public static ServerDataContainer Instance;

    public bool AlwaysAllowWildCards => alwaysAllowWildCards;

    public bool SummarizeGetCards => summarizeGetCards;

    public bool TakeOnlyOneCard => takeOnlyOneCard;

    public bool OnlyOneWinner => onlyOneWinner;

    [SerializeField] private bool alwaysAllowWildCards;
    [SerializeField] private bool summarizeGetCards;
    [SerializeField] private bool takeOnlyOneCard;
    [SerializeField] private bool onlyOneWinner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
}
