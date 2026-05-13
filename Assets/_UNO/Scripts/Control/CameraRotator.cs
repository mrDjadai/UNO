using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotator : MonoBehaviour
{
    [SerializeField] private Transform origin;
    [SerializeField] private Transform cam;
    [SerializeField] private Transform head;

    [SerializeField] private float sensability = 1f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private float minVerticalAngle = -80f; 
    [SerializeField] private float maxHorizontalAngle = 80f;
    [SerializeField] private float minHorizontalAngle = -80f;

    private Vector2 mouseDelta;
    private float currentVerticalAngle;
    private float currentHorizontalAngle;

    private void Update()
    {
        mouseDelta = InputManager.InputActions.Player.Rotate.ReadValue<Vector2>() *
            PlayerPrefs.GetFloat("Sensability");
        
        currentHorizontalAngle += mouseDelta.x * sensability;
        currentHorizontalAngle = Mathf.Clamp(currentHorizontalAngle, minHorizontalAngle, maxHorizontalAngle);
        origin.localRotation = Quaternion.Euler(0f, currentHorizontalAngle, 0f);

        currentVerticalAngle -= mouseDelta.y * sensability;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
        cam.localRotation = Quaternion.Euler(currentVerticalAngle, 0f, 0f);

        head.LookAt(cam.position + cam.forward);
    }
}