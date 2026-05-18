using UnityEngine;
using static ServerDataContainer;
using TMPro;

public class MenuMessageShower : Window
{
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private MessageValue messageValue;

    private void Start()
    {
        ShowMessage(ServerDataContainer.Disconnect);        
    }

    public void ShowMessage(DisconnectType type)
    {
        if (type == DisconnectType.ButtonPressed)
        {
            return;
        }

        typeText.text = messageValue.Message[type.ToString()];
        WindowOpener.instance.CloseAll();
        WindowOpener.instance.OpenWindow(this);
    }
}
