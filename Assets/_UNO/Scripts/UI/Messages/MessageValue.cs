using UnityEngine;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(menuName = "UNO/Messages", fileName = "Messages")]
public class MessageValue : ScriptableObject
{
    [field: SerializeField] public SerializedDictionary<string, string> Message = new SerializedDictionary<string, string>();
}
