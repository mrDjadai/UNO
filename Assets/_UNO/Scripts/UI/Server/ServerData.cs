using System;

[Serializable]
public class RegisterServerData
{
    public string serverName;

    public string address;
    public int port;

    public int players;
    public int maxPlayers;

    public bool gameStarted;

    public bool alwaysAllowWildCards;
    public bool summarizeGetCards;
    public bool takeOnlyOneCard;
    public bool onlyOneWinner;
}

[Serializable]
public class RegisterResponse
{
    public bool success;
    public string serverId;
}

[Serializable]
public class HeartbeatData
{
    public string serverId;

    public int players;

    public bool gameStarted;
}