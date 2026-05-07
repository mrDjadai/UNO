using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        new InputManager();
    }
}
